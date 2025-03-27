using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class NeoclipCharacterController : MonoBehaviour
{
    [SerializeField] private RagdollHelper ragdollHelper;
    [SerializeField] private DragCamera dragCamera;
    [SerializeField] private NeoclipCameraController cameraController;
    [SerializeField] private ActiveRagdoll activeRagdoll;
    [SerializeField] private ExitDirectionFinder exitDirectionFinder;
    [SerializeField] private PanicEstimator panicEstimator;
    [SerializeField] private Animator animator;
        
    [Space]
    [SerializeField] private bool applyGravity = true;
    [SerializeField] private float maxMoveSpeed = 5.0f;
    [SerializeField] private float moveAcceleration = 1.0f;
    [SerializeField] private LayerNumber defaultLayer;
    [SerializeField] private LayerNumber noclipLayer;
    [SerializeField] private LayerMask noclipValidationCheckLayers;
    [SerializeField] private LayerMask shapecastValidationCheckLayers;

    [Space]
    [SerializeField] private InputActionReference mouseMoveAction;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference noclipAction;
    [SerializeField] private InputActionReference bounceAction;
    private Vector2 moveInput;
    private bool noclipInput;
    private void SetMoveInput(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    private void SetNoclipInput(InputAction.CallbackContext context) => noclipInput = context.ReadValueAsButton();
    private void SetBounceInput(InputAction.CallbackContext context)
    {
        bool enabled = context.ReadValueAsButton();
        
        if (enabled)
        {
            for (int i = 0; i < ragdollHelper.NumBones; i++)
            {
                Collider collider = ragdollHelper.GetCollider(i);
                collider.material.bounciness = 1.0f;
                collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
            }
            
            for (int i = 0; i < ragdollHelper.NumJoints; i++)
            {
                ConfigurableJoint joint = ragdollHelper.GetJoint(i);
                joint.angularXDrive = new JointDrive
                {
                    positionSpring = joint.angularXDrive.positionSpring * 2000.0f,
                    positionDamper = joint.angularXDrive.positionDamper * 5000.0f,
                    maximumForce = joint.angularXDrive.maximumForce,
                    useAcceleration = joint.angularXDrive.useAcceleration
                };
                joint.angularYZDrive = new JointDrive
                {
                    positionSpring = joint.angularYZDrive.positionSpring * 2000.0f,
                    positionDamper = joint.angularYZDrive.positionDamper * 5000.0f,
                    maximumForce = joint.angularYZDrive.maximumForce,
                    useAcceleration = joint.angularYZDrive.useAcceleration
                };
            }
        }
    }

    private float[] boneSurfaceAreas;
    private bool[] boneClipStates;
    private bool wasAnyClippingLastFrame = false;
    
    private void Awake()
    {
        boneSurfaceAreas = new float[ragdollHelper.NumBones];
        boneClipStates = new bool[ragdollHelper.NumBones];
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void OnEnable()
    {
        mouseMoveAction.action.performed += cameraController.ApplyMouseInput;
        //mouseMoveAction.action.canceled += cameraController.ApplyMouseInput;
        moveAction.action.performed += SetMoveInput;
        moveAction.action.canceled += SetMoveInput;
        noclipAction.action.performed += SetNoclipInput;
        noclipAction.action.canceled += SetNoclipInput;
        bounceAction.action.performed += SetBounceInput;
        bounceAction.action.canceled += SetBounceInput;
    }

    private void OnDisable()
    {
        mouseMoveAction.action.performed -= cameraController.ApplyMouseInput;
        //mouseMoveAction.action.canceled -= cameraController.ApplyMouseInput;
        moveAction.action.performed -= SetMoveInput;
        moveAction.action.canceled -= SetMoveInput;
        noclipAction.action.performed -= SetNoclipInput;
        noclipAction.action.canceled -= SetNoclipInput;
        bounceAction.action.performed -= SetBounceInput;
        bounceAction.action.canceled -= SetBounceInput;
    }
    
    private void FixedUpdate()
    {
        Vector3 movement = cameraController.GetCameraRelativeMoveVector(moveInput) * moveAcceleration;

        panicEstimator.EstimateTimeToHit();
        
        bool shouldApplyDrag = dragCamera.TryUpdateSurfaceAreas(boneSurfaceAreas);
        
        bool anyBoneClipping = false;
        if (noclipInput || wasAnyClippingLastFrame)
        {
            for (int i = 0; i < ragdollHelper.NumBones; i++)
            {
                boneClipStates[i] = ClippingUtils.CheckOrCastCollider(
                    ragdollHelper.GetCollider(i),
                    noclipValidationCheckLayers.value,
                    shapecastValidationCheckLayers.value);
                anyBoneClipping = anyBoneClipping || boneClipStates[i];
            }
        }
        
        if (!exitDirectionFinder.MainJob.IsCompleted)
        {
            Debug.LogWarning("NeoclipCharacterController.FixedUpdate(): Had to wait for exitDirectionJob to complete!");
        }
        exitDirectionFinder.MainJob.Complete();
        Vector3 exitDirection = exitDirectionFinder.ExitDirection;
        if (anyBoneClipping)
        {
            exitDirectionFinder.transform.position = ragdollHelper.AveragePosition;
            exitDirectionFinder.ScheduleJobs();
        }
        
        for (int i = 0; i < ragdollHelper.NumBones; i++)
        {
            Rigidbody rigidbody = ragdollHelper.GetRigidbody(i);
            
            if (noclipInput && !wasAnyClippingLastFrame)
            {
                rigidbody.gameObject.layer = noclipLayer.value;
            }
            else if (!noclipInput && !anyBoneClipping)
            {
                rigidbody.gameObject.layer = defaultLayer.value;
            }
            
            Vector3 force = Vector3.zero;
            Vector3 acceleration = Vector3.zero;
            
            if (!boneClipStates[i]) // This bone is in open space
            {
                acceleration += applyGravity ? Physics.gravity : Vector3.zero;
            }
            else if (!noclipInput) // This bone is clipping (but the button isn't held down)
            {
                acceleration -= Physics.gravity * 2.0f;
                
                acceleration += exitDirection * 20.0f;
                
                /*
                if (anyBoneClipping && Mathf.Abs(rigidbody.linearVelocity.y) < 1.0f)
                {
                    acceleration += Vector3.up * 10.0f;
                }
                */
            }
            
            if (shouldApplyDrag)
            {
                float density = boneClipStates[i]
                    ? Constants.Density.CLIPSPACE
                    : Constants.Density.AIR;
                
                force += 0.5f * 
                         density *
                         rigidbody.linearVelocity.sqrMagnitude * 
                         0.7f *
                         boneSurfaceAreas[i] * 
                         dragCamera.transform.forward;
            }

            acceleration += movement;
            
            rigidbody.AddForce(force, ForceMode.Force);
            rigidbody.AddForce(acceleration, ForceMode.Acceleration);
        }
        
        wasAnyClippingLastFrame = anyBoneClipping;
    }

    private void Update()
    {
#if UNITY_EDITOR
        // Make the spheres look better in motion
        if (wasAnyClippingLastFrame)
        {
            exitDirectionFinder.transform.position = ragdollHelper.AveragePosition;
        }
#endif
    }
}

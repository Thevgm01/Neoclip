using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class NeoclipCharacterController : MonoBehaviour
{
    [SerializeField] private RagdollHelper ragdollHelper;
    [SerializeField] private DragCamera dragCamera;
    [SerializeField] private NeoclipCameraController cameraController;
    [SerializeField] private ActiveRagdoll activeRagdoll;
    [SerializeField] private ExitDirectionFinder exitDirectionFinder;
    [SerializeField] private ImpactTimeEstimator impactTimeEstimator;
    [SerializeField] private Animator animator;
        
    [Space]
    [SerializeField] private bool applyGravity = true;
    [SerializeField] private float secondsToStayClipping = 0.2f;
    [SerializeField] private float angularSlowdown = 0.1f;
    [SerializeField] private float antiSkateVelocityThreshold = 0.2f;
    [SerializeField] private float maxMoveSpeed = 5.0f;
    [SerializeField] private float moveAcceleration = 1.0f;
    [SerializeField] private LayerNumber defaultLayer;
    [SerializeField] private LayerNumber noclipLayer;
    [SerializeField] private AnimationCurve panicAtImpactTime;
    [SerializeField] private AnimationCurve panicMultAtSpeed;
    [SerializeField] private AnimationCurve panicAtAngularVelocity;
    
    [Space]
    [SerializeField] private InputActionReference mouseLookAction;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference noclipAction;
    [SerializeField] private InputActionReference bounceAction;
    [SerializeField] private InputActionReference leftclickAction;
    [SerializeField] private InputActionReference escapeAction;
    private Vector2 moveInput;
    private bool desiredNoclipState;
    private bool mouseIsInside = false;
    
    private float[] boneSurfaceAreas;
    private ClippingUtils.ClipState[] boneClipStates;
    private int noclipBufferFrames;
    private int animPanicID;
    
    public event Action OnNoclipStarted;
    public event Action OnNoclipStopped;
    public bool IsClipping { get; private set; }
        
    private void OnMoveInput(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    private void OnNoclipInput(InputAction.CallbackContext context)
    {
        desiredNoclipState = context.ReadValueAsButton();
    }
    private void OnBounceInput(InputAction.CallbackContext context)
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
    
    private void OnLeftclickInput(InputAction.CallbackContext context)
    {
        if (!mouseIsInside)
        {
            moveAction.action.performed += OnMoveInput; moveAction.action.canceled += OnMoveInput;
            noclipAction.action.performed += OnNoclipInput; noclipAction.action.canceled += OnNoclipInput;
            bounceAction.action.performed += OnBounceInput; bounceAction.action.canceled += OnBounceInput;
            cameraController.BindMouseLook(mouseLookAction, true);
            mouseIsInside = true;
        }
    }
    
    private void OnEscapeInput(InputAction.CallbackContext context)
    {
        if (mouseIsInside)
        {
            moveAction.action.performed -= OnMoveInput; moveAction.action.canceled -= OnMoveInput;
            noclipAction.action.performed -= OnNoclipInput; noclipAction.action.canceled -= OnNoclipInput;
            bounceAction.action.performed -= OnBounceInput; bounceAction.action.canceled -= OnBounceInput;
            cameraController.BindMouseLook(mouseLookAction, false);
            mouseIsInside = false;
        }
#if UNITY_EDITOR
        else
        {
            EditorApplication.isPaused = true;
        }
#endif
    }
    
    private void SetNoclipLayers() => ragdollHelper.SetLayers(noclipLayer.value);
    private void SetDefaultLayers() => ragdollHelper.SetLayers(defaultLayer.value);
    
    private void Awake()
    {
        boneSurfaceAreas = new float[ragdollHelper.NumBones];
        boneClipStates = new ClippingUtils.ClipState[ragdollHelper.NumBones];
        animPanicID = Animator.StringToHash("Panic");

        OnNoclipStarted += SetNoclipLayers;
        OnNoclipStopped += SetDefaultLayers;
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void OnEnable()
    {
        leftclickAction.action.performed += OnLeftclickInput;
        escapeAction.action.performed += OnEscapeInput;
    }
    
    private void OnDisable()
    {
        leftclickAction.action.performed -= OnLeftclickInput;
        escapeAction.action.performed -= OnEscapeInput;
    }
    
    private void FixedUpdate()
    {
        Vector3 movement = cameraController.GetCameraRelativeMoveVector(moveInput) * moveAcceleration;

        float velocityMagnitude = ragdollHelper.AverageLinearVelocity.magnitude;
        Vector3 velocityNormalized = ragdollHelper.AverageLinearVelocity / velocityMagnitude;
        
        // Set the animator's panic value based on the time to impact
        animator.SetFloat(animPanicID, Mathf.Max(
            panicAtImpactTime.Evaluate(impactTimeEstimator.Estimate()) * panicMultAtSpeed.Evaluate(velocityMagnitude),
            panicAtAngularVelocity.Evaluate(ragdollHelper.AverageAngularVelocity.magnitude)));
        
        // Grab the last frame's drag data
        bool shouldApplyDrag = dragCamera.TryUpdateSurfaceAreas(boneSurfaceAreas);
        
        // Grab last frame's exit direction data
        Vector3 exitDirection;
        if (!exitDirectionFinder.MainJob.IsCompleted)
        {
            double startTime = Time.realtimeSinceStartupAsDouble;
            exitDirectionFinder.MainJob.Complete();
            float timeDiffTruncated = (int)((Time.realtimeSinceStartupAsDouble - startTime) * 100) / 100.0f;
            if (timeDiffTruncated > 0.0f)
            {
                Debug.LogWarning($"NeoclipCharacterController.FixedUpdate(): Had to wait {timeDiffTruncated}ms for exitDirectionJob to complete!");
            }
        }
        else
        {
            exitDirectionFinder.MainJob.Complete();
        }
        exitDirection = exitDirectionFinder.GetExitDirection();
        
        // Test all bones to see if they're actively clipping
        bool anyBoneClipping = false;
        bool anyBoneRayHit = false;
        bool allBonesClipping = true;
        if (desiredNoclipState || noclipBufferFrames > 0)
        {
            for (int i = 0; i < ragdollHelper.NumBones; i++)
            {
                boneClipStates[i] = ClippingUtils.CheckColliderOrCastRaysDetailed(ragdollHelper.GetCollider(i));
                anyBoneClipping = anyBoneClipping || (boneClipStates[i] & ClippingUtils.ClipState.IsClipping) > 0;
                anyBoneRayHit = anyBoneRayHit || (boneClipStates[i] & ClippingUtils.ClipState.RayBackfaceMask) > 0;
                allBonesClipping = allBonesClipping && (boneClipStates[i] & ClippingUtils.ClipState.IsClipping) > 0;
            }
        }
        
        //Debug.Log(boneClipStates[0]);
        
        // Determine if we're "skating" along the ground, which we don't want
        float antiSkateVelocityDot = Vector3.Dot(ragdollHelper.AverageLinearVelocity, -Physics.gravity.normalized);
        bool antiSkate = !desiredNoclipState && anyBoneClipping && !anyBoneRayHit && !allBonesClipping && antiSkateVelocityDot > 0.0f && antiSkateVelocityDot < antiSkateVelocityThreshold;
        if (antiSkate)
        {
            Debug.Log("NeoclipCharacterController.FixedUpdate: Anti-skating mechanism triggered.");
            anyBoneClipping = false;
            anyBoneRayHit = false;
            allBonesClipping = false;
            for (int i = 0; i < ragdollHelper.NumBones; i++)
            {
                boneClipStates[i] = ClippingUtils.ClipState.None;
            }
            noclipBufferFrames = 0;
        }
        
        // Fire events
        if (noclipBufferFrames == 0)
        {
            if (desiredNoclipState) // If we want to noclip and we weren't already noclipping
            {
                OnNoclipStarted?.Invoke();
                IsClipping = true;
            }
            else // If we want to stop noclipping and the timer has run out
            {
                OnNoclipStopped?.Invoke();
                IsClipping = false;
            }
        }
        
        // If we're inside something, calculate the exit direction for next frame
        if (anyBoneClipping)
        {
            exitDirectionFinder.transform.position = ragdollHelper.AveragePosition;
            exitDirectionFinder.ScheduleJobs();
        }
        else
        {
            exitDirectionFinder.ResetExitDirection();
        }
        
        // Iterate through all bones
        for (int i = 0; i < ragdollHelper.NumBones; i++)
        {
            Rigidbody rigidbody = ragdollHelper.GetRigidbody(i);
            
            Vector3 force = Vector3.zero;
            Vector3 acceleration = Vector3.zero;
            
            if ((boneClipStates[i] & ClippingUtils.ClipState.IsClipping) == 0) // This bone is in open space
            {
                acceleration += applyGravity ? Physics.gravity : Vector3.zero;
            }
            else if (!desiredNoclipState) // This bone is clipping (but the button isn't held down)
            {
                acceleration -= Physics.gravity * 2.0f;
                
                acceleration += exitDirection * 20.0f;
            }
            
            if (shouldApplyDrag)
            {
                float density = (boneClipStates[i] & ClippingUtils.ClipState.IsClipping) > 0
                    ? Constants.Density.CLIPSPACE
                    : Constants.Density.AIR;

                Vector3 projectedVelocity = Vector3.Project(rigidbody.linearVelocity, velocityNormalized);
                
                force += 0.5f * 
                         density *
                         projectedVelocity.sqrMagnitude * 
                         0.7f *
                         boneSurfaceAreas[i] * 
                         dragCamera.transform.forward;
            }

            acceleration += movement;
            
            rigidbody.AddForce(force, ForceMode.Force);
            rigidbody.AddForce(acceleration, ForceMode.Acceleration);
            
            if (angularSlowdown > 0.0f)
            {
                rigidbody.angularVelocity = Vector3.RotateTowards(rigidbody.angularVelocity, Vector3.zero, 0.0f, angularSlowdown);
            }
        }
        
        // If we're trying to noclip, or already were, then reset the timer
        if (desiredNoclipState || anyBoneClipping)
        {
            noclipBufferFrames = Mathf.CeilToInt(secondsToStayClipping / Time.fixedDeltaTime);
        }
        else if (noclipBufferFrames > 0)
        {
            noclipBufferFrames--;
        }
    }
}

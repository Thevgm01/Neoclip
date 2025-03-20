using UnityEngine;
using UnityEngine.InputSystem;

public class NeoclipCharacterController : MonoBehaviour
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private DragCamera dragCamera;
    [SerializeField] private NeoclipCameraController cameraController;
    [SerializeField] private ConcaveClipHelper concaveClipHelper;
    
    [Space]
    [SerializeField] private bool applyGravity = true;
    [SerializeField] private float maxMoveSpeed = 5.0f;
    [SerializeField] private float moveAcceleration = 1.0f;
    [Tooltip("Don't collide with these layers while noclipping")]
    [SerializeField] private LayerMask noclipIgnoreLayers;

    [Space]
    [SerializeField] private InputActionReference moveAction;
    private Vector2 moveInput;
    private void SetMoveInput(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    [SerializeField] private InputActionReference noclipAction;
    private bool noclipInput;
    private void SetNoclipInput(InputAction.CallbackContext context) => noclipInput = context.ReadValueAsButton();

    private int defaultIgnoreLayers;

    private float[] boneSurfaceAreas;
    private bool[] boneClipStates;
    private bool wasAnyClippingLastFrame = false;
    
    private void Awake()
    {
        ragdollAverages.Init();
        dragCamera.Init();
        cameraController.Init();

        defaultIgnoreLayers = ragdollAverages.GetCollider(0).excludeLayers.value;
        boneSurfaceAreas = new float[ragdollAverages.NumBones];
        boneClipStates = new bool[ragdollAverages.NumBones];
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void OnEnable()
    {
        moveAction.action.performed += SetMoveInput;
        moveAction.action.canceled += SetMoveInput;
        noclipAction.action.performed += SetNoclipInput;
        noclipAction.action.canceled += SetNoclipInput;
    }

    private void OnDisable()
    {
        moveAction.action.performed -= SetMoveInput;
        moveAction.action.canceled -= SetMoveInput;
        noclipAction.action.performed -= SetNoclipInput;
        noclipAction.action.canceled -= SetNoclipInput;
    }
    
    private void FixedUpdate()
    {
        Vector3 movement = cameraController.CameraRelativeMoveVector(moveInput) * moveAcceleration;
        
        bool shouldApplyDrag = dragCamera.CalculateSurfaceAreas(boneSurfaceAreas);
        bool anyBoneClipping = 
            (noclipInput || wasAnyClippingLastFrame) &&
            concaveClipHelper.CheckAllBones(boneClipStates);
        
        for (int i = 0; i < ragdollAverages.NumBones; i++)
        {
            Rigidbody rigidbody = ragdollAverages.GetRigidbody(i);
            Collider collider = ragdollAverages.GetCollider(i);
            
            if (noclipInput && !wasAnyClippingLastFrame)
            {
                collider.excludeLayers = defaultIgnoreLayers ^ noclipIgnoreLayers.value;
            }
            else if (!noclipInput && !anyBoneClipping)
            {
                collider.excludeLayers = defaultIgnoreLayers;
            }
            
            Vector3 force = Vector3.zero;
            Vector3 acceleration = Vector3.zero;

            if (!boneClipStates[i]) // This bone is in open space
            {
                acceleration += applyGravity ? Physics.gravity : Vector3.zero;
            }
            else if (!noclipInput) // This bone is clipping (but the button isn't held down)
            {
                acceleration -= Physics.gravity;

                if (anyBoneClipping && Mathf.Abs(rigidbody.linearVelocity.y) < 1.0f)
                {
                    acceleration += Vector3.up * 10.0f;
                }
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
    
    private void LateUpdate()
    {
        cameraController.Tick();
    }
}

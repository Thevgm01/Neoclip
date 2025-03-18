using UnityEngine;
using UnityEngine.InputSystem;

public class NeoclipCharacterController : MonoBehaviour
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private DragCamera dragCamera;
    [SerializeField] private NeoclipCameraController cameraController;
    
    [Space]
    [SerializeField] private float minSpeedForDrag = 1.0f;
    [SerializeField] private float maxMoveSpeed = 5.0f;
    [SerializeField] private float moveAcceleration = 1.0f;
    [SerializeField] private LayerMask layersToExcludeWhileNoclipping;

    [Space]
    [SerializeField] private InputActionReference moveAction;
    private Vector2 moveInput;
    private void SetMoveInput(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    [SerializeField] private InputActionReference noclipAction;
    private bool noclipInput;
    private void SetNoclipInput(InputAction.CallbackContext context) => noclipInput = context.ReadValueAsButton();

    private int defaultExcludeLayers;
    
    private void Awake()
    {
        ragdollAverages.Init();
        dragCamera.Init();
        cameraController.Init();

        defaultExcludeLayers = ragdollAverages.GetCollider(0).excludeLayers.value;
        
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
        bool applyDrag = ragdollAverages.AverageVelocity.sqrMagnitude >= minSpeedForDrag * minSpeedForDrag;

        Vector3 movement = cameraController.CameraRelativeMoveVector(moveInput) * moveAcceleration;
        
        for (int i = 0; i < ragdollAverages.NumBones; i++)
        {
            Rigidbody rigidbody = ragdollAverages.GetRigidbody(i);
            NoclipDetector noclipDetector = ragdollAverages.GetTriggerScript(i);
            
            if (noclipInput && !noclipDetector.enabled)
            {
                noclipDetector.enabled = true;
                ragdollAverages.GetCollider(i).excludeLayers = defaultExcludeLayers ^ layersToExcludeWhileNoclipping.value;
            }
            else if (!noclipInput && noclipDetector.IsOutsideEverything)
            {
                noclipDetector.enabled = false;
                ragdollAverages.GetCollider(i).excludeLayers = defaultExcludeLayers;
            }
            
            Vector3 force = Vector3.zero;
            Vector3 acceleration = Vector3.zero;

            if (noclipDetector.IsOutsideEverything)
            {
                acceleration += Physics.gravity;
            }
            else if (!noclipInput)
            {
                acceleration -= Physics.gravity;
            }
            
            if (applyDrag)
            {
                float density = noclipDetector.IsInsideAnything
                    ? Constants.Density.CLIPSPACE
                    : Constants.Density.AIR;
                
                force += 0.5f * 
                         density *
                         rigidbody.linearVelocity.sqrMagnitude * 
                         0.7f *
                         dragCamera.RigidbodySurfaceAreas[i] * 
                         dragCamera.transform.forward;
            }

            acceleration += movement;
            
            rigidbody.AddForce(force, ForceMode.Force);
            rigidbody.AddForce(acceleration, ForceMode.Acceleration);
        }
    }
    
    private void LateUpdate()
    {
        cameraController.Tick();
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class NeoclipCharacterController : MonoBehaviour
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private DragCamera dragCamera;
    [SerializeField] private NeoclipCameraController cameraController;

    [Space] [SerializeField] private InputActionReference moveAction;
    
    [Space]
    [SerializeField] private float minSpeedForDrag = 1.0f;
    [SerializeField] private float maxMoveSpeed = 5.0f;
    [SerializeField] private float moveAcceleration = 1.0f;

    private Vector2 moveInput;
    private void SetMoveInput(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    
    private void Awake()
    {
        ragdollAverages.Init();
        dragCamera.Init();
        cameraController.Init();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void OnEnable()
    {
        moveAction.action.performed += SetMoveInput;
        moveAction.action.canceled += SetMoveInput;
    }

    private void OnDisable()
    {
        moveAction.action.performed -= SetMoveInput;
        moveAction.action.canceled -= SetMoveInput;
    }
    
    private void FixedUpdate()
    {
        bool applyDrag = ragdollAverages.AverageVelocity.sqrMagnitude >= minSpeedForDrag * minSpeedForDrag;

        Vector3 movement = cameraController.CameraRelativeMoveVector(moveInput) * moveAcceleration;
        
        for (int i = 0; i < ragdollAverages.NumBones; i++)
        {
            Rigidbody rigidbody = ragdollAverages.GetRigidbody(i);

            Vector3 force = Vector3.zero;
            Vector3 acceleration = Vector3.zero;

            if (applyDrag)
            {
                force += 0.5f * 
                         Constants.Density.AIR * 
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

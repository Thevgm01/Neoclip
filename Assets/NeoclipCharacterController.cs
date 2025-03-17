using UnityEngine;

public class NeoclipCharacterController : MonoBehaviour
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private DragCamera dragCamera;
    [SerializeField] private NeoclipCameraController cameraController;

    [Space]
    [SerializeField] private float minSpeedForDrag = 1.0f;
    
    private void Awake()
    {
        ragdollAverages.Init();
        dragCamera.Init();
        cameraController.Init();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        bool applyDrag = ragdollAverages.AverageVelocity.sqrMagnitude >= minSpeedForDrag * minSpeedForDrag;
        
        for (int i = 0; i < ragdollAverages.NumRigidbodies; i++)
        {
            Rigidbody rigidbody = ragdollAverages.Rigidbodies[i];

            Vector3 force = Vector3.zero;
            Vector3 acceleration = Vector3.zero;

            if (applyDrag)
            {
                force += 0.5f * 
                         Utils.Density.AIR * 
                         rigidbody.linearVelocity.sqrMagnitude * 
                         0.7f *
                         dragCamera.RigidbodySurfaceAreas[i] * 
                         dragCamera.transform.forward;
            }
            
            rigidbody.AddForce(force, ForceMode.Force);
            rigidbody.AddForce(acceleration, ForceMode.Acceleration);
        }
    }
    
    private void LateUpdate()
    {
        cameraController.Tick();
    }
}

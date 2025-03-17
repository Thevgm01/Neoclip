using UnityEngine;

public class NeoclipCharacterController : MonoBehaviour
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private DragCamera dragCamera;
    [SerializeField] private NeoclipCameraController cameraController;
    
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
        dragCamera.Tick();
    }
    
    private void LateUpdate()
    {
        cameraController.Tick();
    }
}

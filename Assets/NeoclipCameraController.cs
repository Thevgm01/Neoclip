using UnityEngine;
using UnityEngine.InputSystem;

public class NeoclipCameraController : NeoclipCharacterComponent
{
    private enum LookMode { UP, FREE }
    
    [SerializeField] private RagdollAverages ragdollAverages = null;
    
    [Space]
    [SerializeField] private float followSpeed = 5.0f;
    [SerializeField] private float followDistance = 3.0f;
    [Space]
    [SerializeField] private LookMode lookMode = LookMode.UP;
    [SerializeField] private Vector2 mouseSensitivity = Vector2.one;
    [SerializeField] private float rotationSpeed = 30.0f;
    [SerializeField] [Range(0, 1)] private float skewStrength = 0.5f;

    private Vector3 manualCameraAngles = Vector3.zero;
    
    private Vector3 currentPosition = Vector3.zero;
    private Vector3 desiredPosition = Vector3.zero;
    private Quaternion currentRotation = Quaternion.identity;
    private Quaternion desiredRotation = Quaternion.identity;

    public Vector3 CameraRelativeMoveVector(Vector2 moveInput)
    {
        switch (lookMode)
        {
            case LookMode.UP:
                Vector2 rotated = moveInput.Rotate(Mathf.Deg2Rad * -manualCameraAngles.y);
                return new Vector3(rotated.x, 0.0f, rotated.y);
            case LookMode.FREE:
                return desiredRotation * new Vector3(moveInput.x, 0, moveInput.y);
            default:
                return Vector3.zero;
        }
    }
    
    public override void Init()
    {
        manualCameraAngles = transform.rotation.eulerAngles;
        
        currentPosition = transform.position;
        desiredPosition = currentPosition;
        currentRotation = transform.rotation;
        desiredRotation = currentRotation;
    }

    public override void Tick()
    {
        Vector2 movement = mouseSensitivity * Mouse.current.delta.ReadValue() * Time.smoothDeltaTime;

        switch (lookMode)
        {
            case LookMode.UP:
                manualCameraAngles = new Vector3(
                    Mathf.Clamp(manualCameraAngles.x - movement.y, -89, 89), 
                    Mathf.Repeat(manualCameraAngles.y + movement.x, 360), 
                    Mathf.Repeat(manualCameraAngles.z, 360));
                desiredRotation = Quaternion.Euler(manualCameraAngles);
                break;
            case LookMode.FREE:
                desiredRotation *= Quaternion.Euler(-movement.y, movement.x, 0);
                break;
        }
        
        desiredPosition = ragdollAverages.AveragePosition;
        currentPosition = Vector3.Lerp(currentPosition, desiredPosition, GenericUtils.ExpT(followSpeed));

        currentRotation = Quaternion.Slerp(currentRotation, desiredRotation, GenericUtils.ExpT(rotationSpeed));

        Vector3 offsetPosition = currentPosition + currentRotation * new Vector3(0, 0, -followDistance);
        //Vector3 dirToTarget = (desiredPosition - offsetPosition).normalized;
        //Quaternion skew = Quaternion.LookRotation(
        //    dirToTarget, 
        //    Vector3.Cross(dirToTarget, currentRotation * Vector3.right));
        
        // TODO probably don't use skew?
        // Just change the desiredRotation as if the player was trying to look towards the ragdoll as it falls
        //Quaternion skewedRotation = Quaternion.Slerp(currentRotation, skew, skewStrength);

        transform.SetPositionAndRotation(offsetPosition, currentRotation);
    }
}

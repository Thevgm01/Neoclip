using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public enum LookMode { UP, FREE }
    
    [SerializeField] private Transform followTarget = null;
    [SerializeField] private float followSpeed = 5.0f;
    [SerializeField] private float followDistance = 3.0f;
    [Space]
    [SerializeField] private LookMode lookMode = LookMode.UP;
    [SerializeField] private Vector2 mouseSensitivity = Vector2.one;
    [SerializeField] private float rotationSpeed = 30.0f;
    [SerializeField] [Range(0, 1)] private float skewStrength = 0.5f;

    private Vector3 currentPosition = Vector3.zero;
    private Vector3 desiredPosition = Vector3.zero;
    private Quaternion currentRotation = Quaternion.identity;
    private Quaternion desiredRotation = Quaternion.identity;
    
    private void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        currentPosition = transform.position;
        desiredPosition = currentPosition;
        currentRotation = transform.rotation;
        desiredRotation = currentRotation;
    }
    
    private void LateUpdate()
    {
        Vector2 movement = mouseSensitivity * Mouse.current.delta.ReadValue() * Time.smoothDeltaTime;

        switch (lookMode)
        {
            case LookMode.UP:
                break;
            case LookMode.FREE:
                desiredRotation *= Quaternion.Euler(-movement.y, movement.x, 0);
                break;
        }

        desiredPosition = followTarget.position;
        currentPosition = Vector3.Lerp(currentPosition, desiredPosition, Utils.ExpT(followSpeed));

        currentRotation = Quaternion.Slerp(currentRotation, desiredRotation, Utils.ExpT(rotationSpeed));

        Vector3 offsetPosition = currentPosition + currentRotation * new Vector3(0, 0, -followDistance);
        Vector3 dirToTarget = (desiredPosition - offsetPosition).normalized;
        Quaternion skew = Quaternion.LookRotation(
            dirToTarget, 
            Vector3.Cross(dirToTarget, currentRotation * Vector3.right));
        
        Quaternion skewedRotation = Quaternion.Slerp(currentRotation, skew, skewStrength);

        transform.SetPositionAndRotation(offsetPosition, skewedRotation);
    }
}

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public enum LookMode { UP, FREE }
    
    [SerializeField] private RagdollAverages ragdollAverages = null;
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
    
    private void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        manualCameraAngles = transform.rotation.eulerAngles;
        
        currentPosition = transform.position;
        desiredPosition = currentPosition;
        currentRotation = transform.rotation;
        desiredRotation = currentRotation;
    }

    private void Update()
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
    }

    private void LateUpdate()
    {
        desiredPosition = ragdollAverages.AveragePositionInterpolated;
        currentPosition = Vector3.Lerp(currentPosition, desiredPosition, Utils.ExpT(followSpeed));

        currentRotation = Quaternion.Slerp(currentRotation, desiredRotation, Utils.ExpT(rotationSpeed));

        Vector3 offsetPosition = currentPosition + currentRotation * new Vector3(0, 0, -followDistance);
        Vector3 dirToTarget = (desiredPosition - offsetPosition).normalized;
        Quaternion skew = Quaternion.LookRotation(
            dirToTarget, 
            Vector3.Cross(dirToTarget, currentRotation * Vector3.right));
        
        // TODO probably don't use skew?
        // Just change the desiredRotation as if the player was trying to look towards the ragdoll as it falls
        Quaternion skewedRotation = Quaternion.Slerp(currentRotation, skew, skewStrength);

        transform.SetPositionAndRotation(offsetPosition, skewedRotation);
    }
}

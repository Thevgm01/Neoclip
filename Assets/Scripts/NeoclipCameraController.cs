using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class NeoclipCameraController : MonoBehaviour
{
    private enum LookMode { UP, FREE }
    
    [SerializeField] private RagdollHelper ragdollHelper = null;

    [Space]
    [SerializeField] private LayerNumber checkLayer;
    [SerializeField] private LayerNumber castLayer;
    [SerializeField] private float followSpeed = 5.0f;
    [SerializeField] private float followDistance = 3.0f;
    [Space]
    [SerializeField] private LookMode lookMode = LookMode.UP;
    [SerializeField] private Vector2 mouseSensitivity = Vector2.one;
    [SerializeField] private float rotationSpeed = 30.0f;
    [SerializeField] [Range(0, 1)] private float skewStrength = 0.5f;

    private Camera camera;
    private bool wasClipping = false;
    private bool mouseLooking = false;
    
    private Vector3 manualCameraAngles = Vector3.zero;
    
    private Vector3 currentPosition = Vector3.zero;
    private Vector3 desiredPosition = Vector3.zero;
    private Quaternion currentRotation = Quaternion.identity;
    private Quaternion desiredRotation = Quaternion.identity;

    public void BindMouseLook(InputActionReference mouseLookAction, bool value)
    {
        if (value && !mouseLooking) mouseLookAction.action.performed += ApplyMouseInput;
        else if (!value && mouseLooking) mouseLookAction.action.performed -= ApplyMouseInput;
        mouseLooking = value;
    }
    
    public Vector3 GetCameraRelativeMoveVector(Vector2 moveInput)
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

    private void ApplyMouseInput(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>() * mouseSensitivity;
        
        switch (lookMode)
        {
            case LookMode.UP:
                manualCameraAngles = new Vector3(
                    Mathf.Clamp(manualCameraAngles.x - delta.y, -89, 89), 
                    Mathf.Repeat(manualCameraAngles.y + delta.x, 360), 
                    Mathf.Repeat(manualCameraAngles.z, 360));
                desiredRotation = Quaternion.Euler(manualCameraAngles);
                break;
            case LookMode.FREE:
                desiredRotation *= Quaternion.Euler(-delta.y, delta.x, 0);
                break;
        }
    }
    
    // The enum in UnityEngine.ShaderGraph is marked as internal, so I'm replicating it here
    private enum ZTest { Less, Greater, LEqual, GEqual, Equal, NotEqual, Always }
    
    private void SetClipParameters(bool isClipping)
    {
        if (isClipping)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            
            Shader.SetGlobalInteger("_NeoclipCullMode", (int)UnityEngine.Rendering.CullMode.Off);
            
            Shader.SetGlobalInteger("_NeoclipBlendTarget", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            Shader.SetGlobalInteger("_NeoclipBlendSourceFactor", (int)UnityEngine.Rendering.BlendMode.One);
            Shader.SetGlobalInteger("_NeoclipBlendDestinationFactor", (int)UnityEngine.Rendering.BlendMode.One);
            Shader.SetGlobalInteger("_NeoclipBlendDestinationAlpha", (int)UnityEngine.Rendering.BlendMode.One);
            
            Shader.SetGlobalInteger("_NeoclipZTest", (int)ZTest.Always);
            
            Shader.SetGlobalInteger("_NeoclipZWrite", 0);
            
            Shader.SetGlobalInteger("_NeoclipAlphaToMask", 0);
        }
        
        else
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            
            Shader.SetGlobalInteger("_NeoclipCullMode", (int)UnityEngine.Rendering.CullMode.Back);
            
            Shader.SetGlobalInteger("_NeoclipBlendTarget", (int)UnityEngine.Rendering.BlendMode.One);
            Shader.SetGlobalInteger("_NeoclipBlendSourceFactor", (int)UnityEngine.Rendering.BlendMode.Zero);
            Shader.SetGlobalInteger("_NeoclipBlendDestinationFactor", (int)UnityEngine.Rendering.BlendMode.Zero);
            Shader.SetGlobalInteger("_NeoclipBlendDestinationAlpha", (int)UnityEngine.Rendering.BlendMode.Zero);
            
            Shader.SetGlobalInteger("_NeoclipZTest", (int)ZTest.LEqual);
            
            Shader.SetGlobalInteger("_NeoclipZWrite", 1);
            
            Shader.SetGlobalInteger("_NeoclipAlphaToMask", 1);
        }
    }
    
    private void Awake()
    {
        camera = GetComponent<Camera>();
        SetClipParameters(wasClipping);
        manualCameraAngles = transform.rotation.eulerAngles;
        
        currentPosition = transform.position;
        desiredPosition = currentPosition;
        currentRotation = transform.rotation;
        desiredRotation = currentRotation;
    }
    
    private void LateUpdate()
    {
        desiredPosition = ragdollHelper.AveragePosition;
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

        bool isClipping = ClippingUtils.CheckOrCastRay(offsetPosition, 0.0f, checkLayer.value, castLayer.value);
        if (isClipping != wasClipping)
        {
            SetClipParameters(isClipping);
            wasClipping = isClipping;
        }
    }
    
    private void OnDestroy()
    {
        SetClipParameters(false);
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DragCamera : MonoBehaviour
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private float minSpeed = 1.0f;
    private Camera dragCamera;
    private RenderPipeline.StandardRequest request;
    
    private void Awake()
    {
        dragCamera = GetComponent<Camera>();
        dragCamera.enabled = false;
        request = new RenderPipeline.StandardRequest();
    }
    
    private void FixedUpdate()
    {
        if (ragdollAverages.AverageVelocity.sqrMagnitude >= minSpeed * minSpeed)
        {
            Quaternion rotation = Quaternion.LookRotation(-ragdollAverages.AverageVelocity.normalized);
            
            transform.SetPositionAndRotation(
                ragdollAverages.AveragePosition + rotation * new Vector3(0, 0, -dragCamera.orthographicSize), 
                rotation);

            RenderPipeline.SubmitRenderRequest(dragCamera, request);
        }
    }
}

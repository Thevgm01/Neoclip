using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DragCamera : MonoBehaviour
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private float minSpeed = 1.0f;
    private Camera camera;
    
    private void Awake()
    {
        camera = GetComponent<Camera>();
        camera.enabled = false;
    }
    
    private void FixedUpdate()
    {
        if (ragdollAverages.AverageVelocity.sqrMagnitude >= minSpeed * minSpeed)
        {
            Quaternion rotation = Quaternion.LookRotation(-ragdollAverages.AverageVelocity.normalized);
            
            transform.SetPositionAndRotation(
                ragdollAverages.AveragePosition + rotation * new Vector3(0, 0, -camera.orthographicSize), 
                rotation);

            camera.Render(); 
            //var request = new UniversalRenderPipeline.SingleCameraRequest();
            //request.destination = rt;
            //RenderPipeline.SubmitRenderRequest(camera, a);
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DragCamera : MonoBehaviour
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private float minSpeed = 1.0f;
    [SerializeField] ComputeShader computeShader;
    
    private Camera dragCamera;
    private RenderTexture renderTexture;
    private RenderPipeline.StandardRequest request;

    private int MaxColors = 20;
    private int[] hitsPerColor;
    private ComputeBuffer hitBuffer;
    
    private void Awake()
    {
        dragCamera = GetComponent<Camera>();
        dragCamera.enabled = false;
        renderTexture = dragCamera.targetTexture;
        request = new RenderPipeline.StandardRequest();
        hitsPerColor = new int[20];
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
            CountColors();
            Debug.Log(ragdollAverages.AverageVelocity.magnitude * 60 * 60 / 1000);
        }
    }

    private void CountColors()
    {
        int width = renderTexture.width;
        int height = renderTexture.height;

        // Create a buffer for color counts
        hitsPerColor = new int[20];
        hitBuffer = new ComputeBuffer(MaxColors, sizeof(int));
        hitBuffer.SetData(hitsPerColor);

        // Set shader parameters
        int kernel = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernel, "InputTexture", renderTexture);
        computeShader.SetBuffer(kernel, "ColorCounts", hitBuffer);

        // Dispatch the compute shader
        int threadGroupsX = Mathf.CeilToInt(width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(height / 8.0f);
        computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        // Retrieve results
        hitBuffer.GetData(hitsPerColor); // Use async method?

        float totalSurfaceArea = dragCamera.orthographicSize * dragCamera.orthographicSize * 4.0f;
        float areaPerPixel = totalSurfaceArea / (width * height);
        
        for(int i = 0; i < ragdollAverages.Rigidbodies.Length; i++)
        {
            Rigidbody rigidbody = ragdollAverages.Rigidbodies[i];
            float drag_magnitude = 0.5f * Utils.Density.AIR * rigidbody.linearVelocity.sqrMagnitude * 0.7f * hitsPerColor[i] * areaPerPixel;
            ragdollAverages.Rigidbodies[i].AddForce(drag_magnitude * dragCamera.transform.forward, ForceMode.Force);
        }

        // Clean up
        hitBuffer.Release();
    }
}

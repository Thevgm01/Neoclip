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
        float time = Time.time;
        hitBuffer.GetData(hitsPerColor);
        Debug.Log(Time.time - time);
        
        Debug.Log(string.Join(", ", hitsPerColor));

        // Clean up
        hitBuffer.Release();
    }
}

using UnityEngine;
using UnityEngine.Rendering;

public class DragCamera : NeoclipCharacterComponent
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] ComputeShader computeShader;
    
    private Camera dragCamera;
    
    private Vector2Int renderTextureDimensions;
    
    private float areaPerPixel;

    private RenderPipeline.StandardRequest renderRequest;
    
    private int computeKernel;
    private ComputeBuffer hitBuffer;

    private FixedTimeUpdatedProperty<float[]> rigidbodySurfaceAreas;
    public float[] RigidbodySurfaceAreas => rigidbodySurfaceAreas.GetValue();
    
    public override void Init()
    {
        dragCamera = GetComponent<Camera>();
        dragCamera.enabled = false;
        
        renderTextureDimensions = new(dragCamera.targetTexture.width, dragCamera.targetTexture.height);
        
        float dragCameraSurfaceArea = dragCamera.orthographicSize * dragCamera.orthographicSize * 4.0f;
        float numRenderTexturePixels = renderTextureDimensions.x * renderTextureDimensions.y;
        areaPerPixel = dragCameraSurfaceArea / numRenderTexturePixels;

        renderRequest = new RenderPipeline.StandardRequest();
        
        // Set shader parameters
        computeKernel = computeShader.FindKernel("CSMain");
        hitBuffer = new ComputeBuffer(ragdollAverages.NumRigidbodies, sizeof(int));
        computeShader.SetTexture(computeKernel, Shader.PropertyToID("InputTexture"), dragCamera.targetTexture);
        computeShader.SetBuffer(computeKernel, Shader.PropertyToID("ColorCounts"), hitBuffer);
        
        rigidbodySurfaceAreas = new FixedTimeUpdatedProperty<float[]>(CalculateSurfaceAreas);
    }

    private void MoveCamera()
    {
        Quaternion rotation = Quaternion.LookRotation(-ragdollAverages.AverageVelocity.normalized);
            
        transform.SetPositionAndRotation(
            ragdollAverages.AveragePosition + rotation * new Vector3(0, 0, -dragCamera.orthographicSize), 
            rotation);
    }

    private int[] CalculateHitsPerColor()
    {
        // Create a buffer for color counts
        int[] hitsPerColor = new int[ragdollAverages.NumRigidbodies];
        hitBuffer.SetData(hitsPerColor);

        // Dispatch the compute shader
        int threadGroupsX = Mathf.CeilToInt(renderTextureDimensions.x / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(renderTextureDimensions.y / 8.0f);
        computeShader.Dispatch(computeKernel, threadGroupsX, threadGroupsY, 1);

        // Retrieve results
        //float time = Time.realtimeSinceStartup;
        hitBuffer.GetData(hitsPerColor); // Use async method?
        //Debug.Log(Time.realtimeSinceStartup - time);
        
        return hitsPerColor;
    }
    
    private float[] CalculateSurfaceAreas()
    {
        MoveCamera();
        
        RenderPipeline.SubmitRenderRequest(dragCamera, renderRequest);
        
        int[] hits = CalculateHitsPerColor();

        float[] result = new float[ragdollAverages.NumRigidbodies];

        for (int i = 0; i < ragdollAverages.NumRigidbodies; i++)
        {
            result[i] = hits[i] * areaPerPixel;
        }

        return result;
    }

    private void OnDestroy()
    {
        // Clean up
        hitBuffer.Release();
    }
}

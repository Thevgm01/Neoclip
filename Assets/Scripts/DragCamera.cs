using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class DragCamera : MonoBehaviour
{
    [SerializeField] private RagdollHelper ragdollHelper;
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private float minSpeedForDrag = 1.0f;
    
    private Camera dragCamera;

    private RenderPipeline.StandardRequest renderRequest;
    
    private int computeKernel;
    private ComputeBuffer hitBuffer;
    
    private Vector2Int renderTextureDimensions;
    private float areaPerPixel;
    private int[] pixelHits;
    private bool hasData;
    
    public void Awake()
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
        hitBuffer = new ComputeBuffer(ragdollHelper.NumBones, sizeof(int));
        computeShader.SetTexture(computeKernel, Shader.PropertyToID("InputTexture"), dragCamera.targetTexture);
        computeShader.SetBuffer(computeKernel, Shader.PropertyToID("ColorCounts"), hitBuffer);

        pixelHits = new int[ragdollHelper.NumBones];
    }

    private void MoveCamera()
    {
        Quaternion rotation = Quaternion.LookRotation(-ragdollHelper.AverageLinearVelocity.Normalized);
            
        transform.SetPositionAndRotation(
            ragdollHelper.AveragePosition + rotation * new Vector3(0, 0, -dragCamera.orthographicSize), 
            rotation);
    }

    private void DispatchComputeShader()
    {
        // Create a buffer for color counts
        hitBuffer.SetData(new int[ragdollHelper.NumBones]);

        // Dispatch the compute shader
        int threadGroupsX = Mathf.CeilToInt(renderTextureDimensions.x / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(renderTextureDimensions.y / 8.0f);
        computeShader.Dispatch(computeKernel, threadGroupsX, threadGroupsY, 1);

        // Grab the data when it's done
        AsyncGPUReadback.Request(hitBuffer, OnCompleteReadback);
    }

    private void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        request.GetData<int>().CopyTo(pixelHits);
        hasData = true;
    }
    
    public bool TryUpdateSurfaceAreas(float[] surfaceAreas)
    {
        if (ragdollHelper.AverageLinearVelocity.SqrMagnitude >= minSpeedForDrag * minSpeedForDrag)
        {
            MoveCamera();
            RenderPipeline.SubmitRenderRequest(dragCamera, renderRequest);
            DispatchComputeShader();
        }
        
        if (hasData)
        {
            for (int i = 0; i < ragdollHelper.NumBones; i++)
            {
                surfaceAreas[i] = pixelHits[i] * areaPerPixel;
            }
            
            hasData = false;
            return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        // Clean up
        hitBuffer.Release();
    }
}

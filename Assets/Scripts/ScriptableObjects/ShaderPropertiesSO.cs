using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ClipspaceShaderProperties", menuName = "Scriptable Objects/ClipspaceShaderProperties")]
public class ShaderPropertiesSO : ScriptableObject
{
    // The enum in UnityEngine.ShaderGraph is marked as internal, so I'm replicating it here
    public enum ZTest { Less, Greater, LEqual, GEqual, Equal, NotEqual, Always }

    [System.Serializable]
    public struct BlendMode
    {
        public UnityEngine.Rendering.BlendMode sourceFactor;
        public UnityEngine.Rendering.BlendMode destinationFactor;
    }
    
    [Header("Camera Settings")]
    public CameraClearFlags clearFlags;
    public Color backgroundColor;
    public OpaqueSortMode opaqueSortMode;
    public TransparencySortMode transparencySortMode;
    
    [Header("Shader Globals")]
    public UnityEngine.Rendering.CullMode cullMode;
    public BlendMode blendMode;
    public UnityEngine.Rendering.BlendOp blendOp;
    public ZTest zTest;
    public bool zWrite;
    public bool alphaToMask;
    public bool isClipping;
}

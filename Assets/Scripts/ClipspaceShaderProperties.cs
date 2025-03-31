using UnityEngine;

[CreateAssetMenu(fileName = "ClipspaceShaderProperties", menuName = "Scriptable Objects/ClipspaceShaderProperties")]
public class ClipspaceShaderProperties : ScriptableObject
{
    // The enum in UnityEngine.ShaderGraph is marked as internal, so I'm replicating it here
    public enum ZTest { Less, Greater, LEqual, GEqual, Equal, NotEqual, Always }
    
    public UnityEngine.Rendering.CullMode cullMode;
    public UnityEngine.Rendering.BlendMode blendTarget;
    public UnityEngine.Rendering.BlendMode blendSourceFactor;
    public UnityEngine.Rendering.BlendMode blendDestinationFactor;
    public UnityEngine.Rendering.BlendMode blendDestinationAlpha;
    public ZTest zTest;
    public bool zWrite;
    public bool alphaToMask;
    public bool isClipping;
}

using System;
using UnityEngine;

public class ClipspaceShaderPropertySetter : MonoBehaviour
{
    // The enum in UnityEngine.ShaderGraph is marked as internal, so I'm replicating it here
    private enum ZTest { Less, Greater, LEqual, GEqual, Equal, NotEqual, Always }
    
    [SerializeField] private NeoclipCharacterController characterController;
    [SerializeField] private NeoclipCameraController cameraController;
    [SerializeField] private Camera camera;
    [SerializeField] private bool waitForRagdollToExit;

    private bool areClippingParametersSet = false;
    private bool characterClipping = false;
    private bool cameraWasClipping = false;
    
    private void SetShaderParameters(bool isClipping)
    {
        if (isClipping && !areClippingParametersSet)
        {
            Debug.Log("ClipspaceShaderPropertySetter.SetShaderParameters: Setting clipspace parameters.");
            
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
            Shader.SetGlobalInteger("_NeoclipIsClipping", 1);

            areClippingParametersSet = true;
        }
        else if (!isClipping && areClippingParametersSet)
        {
            Debug.Log("ClipspaceShaderPropertySetter.SetShaderParameters: Setting standard parameters.");
            
            camera.clearFlags = CameraClearFlags.Skybox;
            
            Shader.SetGlobalInteger("_NeoclipCullMode", (int)UnityEngine.Rendering.CullMode.Back);
            Shader.SetGlobalInteger("_NeoclipBlendTarget", (int)UnityEngine.Rendering.BlendMode.One);
            Shader.SetGlobalInteger("_NeoclipBlendSourceFactor", (int)UnityEngine.Rendering.BlendMode.Zero);
            Shader.SetGlobalInteger("_NeoclipBlendDestinationFactor", (int)UnityEngine.Rendering.BlendMode.Zero);
            Shader.SetGlobalInteger("_NeoclipBlendDestinationAlpha", (int)UnityEngine.Rendering.BlendMode.Zero);
            Shader.SetGlobalInteger("_NeoclipZTest", (int)ZTest.LEqual);
            Shader.SetGlobalInteger("_NeoclipZWrite", 1);
            Shader.SetGlobalInteger("_NeoclipAlphaToMask", 1);
            Shader.SetGlobalInteger("_NeoclipIsClipping", 0);

            areClippingParametersSet = false;
        }
    }
    
    private void CharacterStartedNoclipping() => characterClipping = true;

    private void CharacterStoppedNoclipping()
    {
        characterClipping = false;

        if (!cameraWasClipping && waitForRagdollToExit)
        {
            SetShaderParameters(false);
        }
    }
    
    private void CameraMoved()
    {
        bool isClipping = ClippingUtils.CheckOrCastRay(cameraController.transform.position, 0.0f);

        if (isClipping && !cameraWasClipping)
        {
            SetShaderParameters(true);
        }
        else if (!isClipping && cameraWasClipping || characterClipping && waitForRagdollToExit)
        {
            SetShaderParameters(false);
        }

        cameraWasClipping = isClipping;
    }
    
    private void OnEnable()
    {
        characterController.OnNoclipStarted += CharacterStartedNoclipping;
        characterController.OnNoclipStopped += CharacterStoppedNoclipping;
        cameraController.OnMove += CameraMoved;
        
        SetShaderParameters(false);
    }

    private void OnDisable()
    {
        characterController.OnNoclipStarted -= CharacterStartedNoclipping;
        characterController.OnNoclipStopped -= CharacterStoppedNoclipping;
        cameraController.OnMove -= CameraMoved;
        
        SetShaderParameters(false);
    }

    private void OnDestroy()
    {
        SetShaderParameters(false);
    }
}

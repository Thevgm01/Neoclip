using System;
using UnityEngine;

public class ClipspaceShaderPropertySetter : MonoBehaviour
{
    [SerializeField] private NeoclipCharacterController characterController;
    [SerializeField] private NeoclipCameraController cameraController;
    [SerializeField] private Camera camera;
    [SerializeField] private bool waitForRagdollToExit;
    [SerializeField] private ClipspaceShaderProperties normalProperties;
    [SerializeField] private ClipspaceShaderProperties clippingProperties;
    
    private bool characterClipping = false;
    private bool cameraWasClipping = false;
    
    private readonly int cullModeID = Shader.PropertyToID("_NeoclipCullMode");
    private readonly int blendTargetID = Shader.PropertyToID("_NeoclipBlendTarget");
    private readonly int blendSourceFactorID = Shader.PropertyToID("_NeoclipBlendSourceFactor");
    private readonly int blendDestinationFactorID = Shader.PropertyToID("_NeoclipBlendDestinationFactor");
    private readonly int blendDestinationAlphaID = Shader.PropertyToID("_NeoclipBlendDestinationAlpha");
    private readonly int zTestID = Shader.PropertyToID("_NeoclipZTest");
    private readonly int zWriteID = Shader.PropertyToID("_NeoclipZWrite");
    private readonly int alphaToMaskID = Shader.PropertyToID("_NeoclipAlphaToMask");
    private readonly int isClippingID = Shader.PropertyToID("_NeoclipIsClipping");
    
    private void SetShaderParameters(ClipspaceShaderProperties properties)
    {
        //Debug.Log("ClipspaceShaderPropertySetter.SetShaderParameters: Setting parameters.");
        camera.clearFlags = properties.clearFlags;
        camera.backgroundColor = properties.backgroundColor;
        Shader.SetGlobalInteger(cullModeID, (int)properties.cullMode);
        Shader.SetGlobalInteger(blendTargetID, (int)properties.blendTarget);
        Shader.SetGlobalInteger(blendSourceFactorID, (int)properties.blendSourceFactor);
        Shader.SetGlobalInteger(blendDestinationFactorID, (int)properties.blendDestinationFactor);
        Shader.SetGlobalInteger(blendDestinationAlphaID, (int)properties.blendDestinationAlpha);
        Shader.SetGlobalInteger(zTestID, (int)properties.zTest);
        Shader.SetGlobalInteger(zWriteID, properties.zWrite ? 1 : 0);
        Shader.SetGlobalInteger(alphaToMaskID, properties.alphaToMask ? 1 : 0);
        Shader.SetGlobalInteger(isClippingID, properties.isClipping ? 1 : 0);
    }
    
    private void CharacterStartedNoclipping() => characterClipping = true;

    private void CharacterStoppedNoclipping()
    {
        characterClipping = false;

        if (!cameraWasClipping && waitForRagdollToExit)
        {
            SetShaderParameters(normalProperties);
        }
    }
    
    private void CameraMoved()
    {
        bool isClipping = ClippingUtils.CheckOrCastRay(cameraController.transform.position, 0.0f);

        if (isClipping && !cameraWasClipping)
        {
            SetShaderParameters(clippingProperties);
        }
        else if (!isClipping && cameraWasClipping || characterClipping && waitForRagdollToExit)
        {
            SetShaderParameters(normalProperties);
        }

        cameraWasClipping = isClipping;
    }
    
    private void OnEnable()
    {
        characterController.OnNoclipStarted += CharacterStartedNoclipping;
        characterController.OnNoclipStopped += CharacterStoppedNoclipping;
        cameraController.OnMove += CameraMoved;
        
        SetShaderParameters(normalProperties);
    }

    private void OnDisable()
    {
        characterController.OnNoclipStarted -= CharacterStartedNoclipping;
        characterController.OnNoclipStopped -= CharacterStoppedNoclipping;
        cameraController.OnMove -= CameraMoved;
        
        SetShaderParameters(normalProperties);
    }
}

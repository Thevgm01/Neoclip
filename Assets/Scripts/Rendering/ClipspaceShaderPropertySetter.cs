using System;
using UnityEngine;

public class ClipspaceShaderPropertySetter : MonoBehaviour
{
    [SerializeField] private NeoclipCharacterController characterController;
    [SerializeField] private NeoclipCameraController cameraController;
    [SerializeField] private Camera camera;
    [SerializeField] private bool waitForRagdollToExit;
    [SerializeField] private ShaderPropertiesSO normalProperties;
    [SerializeField] private ShaderPropertiesSO clippingProperties;
    
    private bool characterClipping = false;
    private bool cameraWasClipping = false;
    
    private readonly int cullModeID = Shader.PropertyToID("_NeoclipCullMode");
    private readonly int blendSourceFactorID = Shader.PropertyToID("_NeoclipBlendSourceFactor");
    private readonly int blendDestinationFactorID = Shader.PropertyToID("_NeoclipBlendDestinationFactor");
    private readonly int blendOpID = Shader.PropertyToID("_NeoclipBlendOp");
    private readonly int zTestID = Shader.PropertyToID("_NeoclipZTest");
    private readonly int zWriteID = Shader.PropertyToID("_NeoclipZWrite");
    private readonly int alphaToMaskID = Shader.PropertyToID("_NeoclipAlphaToMask");
    private readonly int isClippingID = Shader.PropertyToID("_NeoclipIsClipping");
    
    private void SetShaderParameters(ShaderPropertiesSO properties, bool shouldPrint = true)
    {
        if (shouldPrint)
        {
            Debug.Log($"{nameof(ClipspaceShaderPropertySetter)}.{nameof(SetShaderParameters)}: Setting parameters to {properties.name}");
        }

        camera.clearFlags = properties.clearFlags;
        camera.backgroundColor = properties.backgroundColor;
        camera.opaqueSortMode = properties.opaqueSortMode;
        camera.transparencySortMode = properties.transparencySortMode;
        
        Shader.SetGlobalInteger(cullModeID, (int)properties.cullMode);
        Shader.SetGlobalInteger(blendSourceFactorID, (int)properties.blendMode.sourceFactor);
        Shader.SetGlobalInteger(blendDestinationFactorID, (int)properties.blendMode.destinationFactor);
        Shader.SetGlobalInteger(blendOpID, (int)properties.blendOp);
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
        bool isClipping = ClippingUtils.CheckOrCastRays(cameraController.transform.position, 0.0f);

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

#if UNITY_EDITOR
    [SerializeField] private bool updateEveryFrame = false;
    private void Update()
    {
        if (updateEveryFrame)
        {
            SetShaderParameters(
                cameraWasClipping || characterClipping && waitForRagdollToExit
                    ? clippingProperties 
                    : normalProperties, false);
        }
    }
#endif
}

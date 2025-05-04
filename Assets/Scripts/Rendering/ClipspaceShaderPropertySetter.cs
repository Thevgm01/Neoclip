using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ClipspaceShaderPropertySetter : MonoBehaviour
{
    [SerializeField] private NeoclipCharacterController characterController;
    [SerializeField] private NeoclipCameraController cameraController;
    [SerializeField] private Camera camera;
    [Tooltip("Whether to wait for the ragdoll's noclip buffer timer to expire before setting the properties back to normal.")]
    [SerializeField] private bool waitForRagdollToExit;
    [SerializeField] private ShaderPropertiesSO normalProperties;
    [SerializeField] private ShaderPropertiesSO clippingProperties;
    [SerializeField] private GameObject city;
    [Tooltip("The renderer with the EdgDetectionFeature that can be turned off by the shader properties")]
    [SerializeField] private ScriptableRendererData rendererData;
    [Space]
    [Tooltip("When the player is not noclipping, set their color to this. Independent of whether the camera is inside something.")]
    [SerializeField] private Color defaultPlayerColor;
    [Tooltip("When the player is trying to noclip, set their color to this. Independent of whether the camera is inside something.")]
    [SerializeField] private Color clippingPlayerColor;
    
    private bool characterClipping = false;
    private bool cameraWasClipping = false;

    private Material[] cityMaterials;
    private EdgeDetectionRendererFeature edgeDetectionRendererFeature;
    
    private readonly int cullModeID = Shader.PropertyToID("_NeoclipCullMode");
    private readonly int blendSourceFactorID = Shader.PropertyToID("_NeoclipBlendSourceFactor");
    private readonly int blendDestinationFactorID = Shader.PropertyToID("_NeoclipBlendDestinationFactor");
    private readonly int blendOpID = Shader.PropertyToID("_NeoclipBlendOp");
    private readonly int zTestID = Shader.PropertyToID("_NeoclipZTest");
    private readonly int zWriteID = Shader.PropertyToID("_NeoclipZWrite");
    private readonly int alphaToMaskID = Shader.PropertyToID("_NeoclipAlphaToMask");
    private readonly int isClippingID = Shader.PropertyToID("_NeoclipIsClipping");
    private readonly int playerColorID = Shader.PropertyToID("_NeoclipPlayerColor");
    
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

        // TODO: Figure out something better than this! It lags if the materials switch too frequently
        for (int i = 0; i < cityMaterials.Length; i++)
        {
            cityMaterials[i].shader = properties.cityShader;
        }
        
        edgeDetectionRendererFeature.SetActive(properties.enableEdgeDetectionRendererFeature);
    }

    private void SetCharacterColor(Color color)
    {
        Shader.SetGlobalColor(playerColorID, color);
    }

    private void CharacterStartedNoclipping()
    {
        characterClipping = true;
        SetCharacterColor(clippingPlayerColor);
    }

    private void CharacterStoppedNoclipping()
    {
        characterClipping = false;
        SetCharacterColor(defaultPlayerColor);
        
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
        SetCharacterColor(characterClipping ? clippingPlayerColor : defaultPlayerColor);
    }

    private void OnDisable()
    {
        characterController.OnNoclipStarted -= CharacterStartedNoclipping;
        characterController.OnNoclipStopped -= CharacterStoppedNoclipping;
        cameraController.OnMove -= CameraMoved;
        
        SetShaderParameters(normalProperties);
        SetCharacterColor(defaultPlayerColor);
    }

    private void Awake()
    {
        Renderer[] cityRenderers = city.GetComponentsInChildren<Renderer>();
        HashSet<Material> uniqueCityMaterials = new HashSet<Material>();
        foreach (Renderer renderer in cityRenderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                uniqueCityMaterials.Add(material);
            }
        }
        cityMaterials = uniqueCityMaterials.ToArray();
        Debug.Log($"{nameof(ClipspaceShaderPropertySetter)}.{nameof(Awake)}: Found {cityMaterials.Length} unique city materials");

        rendererData.TryGetRendererFeature(out edgeDetectionRendererFeature);
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

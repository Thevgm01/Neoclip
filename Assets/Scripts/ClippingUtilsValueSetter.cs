using UnityEngine;

public class ClippingUtilsValueSetter : MonoBehaviour
{
    [SerializeField] private Collider voidCollider;
    [SerializeField] private LayerMask checkLayers;
    [SerializeField] private LayerMask castLayers;
    
    void OnEnable()
    {
        ClippingUtils.SetVoidCollider(voidCollider);
        ClippingUtils.SetLayers(checkLayers.value, castLayers.value);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ClippingUtils.SetVoidCollider(voidCollider);
        ClippingUtils.SetLayers(checkLayers.value, castLayers.value);
    }
#endif
}

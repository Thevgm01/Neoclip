using UnityEngine;

public class VoidCollider : MonoBehaviour
{
    void OnEnable()
    {
        ClippingUtils.SetVoidCollider(GetComponent<Collider>());
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ClippingUtils.SetVoidCollider(GetComponent<Collider>());
    }
#endif
}

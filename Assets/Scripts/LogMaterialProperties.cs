using System;
using UnityEngine;

public class LogMaterialProperties : MonoBehaviour
{
    public Material material;

    private void Awake()
    {
        Debug.Log("Floats: " + string.Join(", ", material.GetPropertyNames(MaterialPropertyType.Float)));
        Debug.Log("Ints: " + string.Join(", ", material.GetPropertyNames(MaterialPropertyType.Int)));
    }
}

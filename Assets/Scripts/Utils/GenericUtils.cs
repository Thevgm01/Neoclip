using System;
using UnityEngine;

public static class GenericUtils
{
    public static int FixedUpdateCount => Mathf.CeilToInt(Time.fixedTime / Time.fixedDeltaTime);
    
    public static float ExpT(float speed) => 1.0f - Mathf.Exp(-speed * Time.deltaTime);

    public static int ToLayerNumber(this LayerMask mask) => Mathf.RoundToInt(Mathf.Log(mask.value, 2.0f));
    
    public static Vector2 Rotate(this Vector2 v, float radians)
    {
	    float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
	    return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}

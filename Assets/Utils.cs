using UnityEngine;

public class Utils
{
    public static float ExpT(float speed) => 1.0f - Mathf.Exp(-speed * Time.deltaTime);
}

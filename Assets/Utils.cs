using UnityEngine;

public class Utils
{
    public class Density
    {
        public const float WATER = 1000.0f;
    }
    
    public static float ExpT(float speed) => 1.0f - Mathf.Exp(-speed * Time.deltaTime);

    public static float CalculateVolume(Collider collider)
    {
        if (collider is BoxCollider)
        {
            BoxCollider boxCollider = collider as BoxCollider;
            return boxCollider.size.x * boxCollider.size.y * boxCollider.size.z;
        }
        
        if (collider is CapsuleCollider)
        {
            CapsuleCollider capsuleCollider = collider as CapsuleCollider;
            return Mathf.PI * capsuleCollider.radius * capsuleCollider.radius *
                   (capsuleCollider.radius * 4.0f / 3.0f + capsuleCollider.height);
        }

        if (collider is SphereCollider)
        {
            SphereCollider sphereCollider = collider as SphereCollider;
            return Mathf.PI * sphereCollider.radius * sphereCollider.radius * sphereCollider.radius * 4.0f / 3.0f;
        }

        Debug.LogWarning($"Utils.CalculateVolume: Unknown collider {collider}");
        return 0.0f;
    }
}

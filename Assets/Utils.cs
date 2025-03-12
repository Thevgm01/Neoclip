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
        switch (collider)
        {
            case BoxCollider boxCollider:
                return boxCollider.size.x * boxCollider.size.y * boxCollider.size.z;
            case CapsuleCollider capsuleCollider:
                return Mathf.PI * capsuleCollider.radius * capsuleCollider.radius * (capsuleCollider.radius * 4.0f / 3.0f + capsuleCollider.height);
            case SphereCollider sphereCollider:
                return Mathf.PI * sphereCollider.radius * sphereCollider.radius * sphereCollider.radius * 4.0f / 3.0f;
        }

        Debug.LogWarning($"Utils.CalculateVolume: Unknown collider {collider}");
        return 0.0f;
    }
}

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

    public static GameObject ColliderToMesh(Collider collider)
    {
        switch (collider)
        {
            case BoxCollider boxCollider:
                GameObject boxGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                MeshFilter meshFilter = boxGO.GetComponent<MeshFilter>();
                Mesh oldMesh = meshFilter.sharedMesh;
                Mesh newMesh = new Mesh();
                
                Vector3[] vertices = new Vector3[oldMesh.vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = new Vector3(
                        oldMesh.vertices[i].x * boxCollider.size.x + boxCollider.center.x,
                        oldMesh.vertices[i].y * boxCollider.size.y + boxCollider.center.y,
                        oldMesh.vertices[i].z * boxCollider.size.z + boxCollider.center.z);
                }
                
                newMesh.vertices = vertices;
                newMesh.triangles = oldMesh.triangles;
                newMesh.RecalculateBounds();
                newMesh.RecalculateNormals();
                meshFilter.sharedMesh = newMesh;
                return boxGO;
            case CapsuleCollider capsuleCollider:
                //GameObject capsuleGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                break;
            case SphereCollider sphereCollider:
                //GameObject sphereGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                break;
        }

        Debug.LogWarning($"Utils.ColliderToMesh: Unknown collider {collider}");
        return null;
    }
}

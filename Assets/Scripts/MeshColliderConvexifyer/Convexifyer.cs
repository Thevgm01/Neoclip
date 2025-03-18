using Unity.VisualScripting;
using UnityEngine;

public class Convexifyer : MonoBehaviour
{
    public bool make = false;

    private void OnValidate()
    {
        if (!make) return;

        make = false;
        
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<MeshCollider>();

        SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
        Mesh sphereMesh = sphereCollider.ToMeshWithVertexColor(Color.white);
        DestroyImmediate(sphereCollider);
        
        collider.convex = true;
        collider.sharedMesh = sphereMesh;
    }
}

using System;
using UnityEngine;

public class Utils
{
    public class Density
    {
        public const float WATER = 1000.0f;
    }
 
    //https://discussions.unity.com/t/getting-a-primitive-mesh-without-creating-a-new-gameobject/78809/6
    private static Mesh _unityCapsuleMesh = null;
    private static Mesh _unityCubeMesh = null;
    private static Mesh _unityCylinderMesh = null;
    private static Mesh _unityPlaneMesh = null;
    private static Mesh _unitySphereMesh = null;
    private static Mesh _unityQuadMesh = null;
    
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

    //https://discussions.unity.com/t/getting-a-primitive-mesh-without-creating-a-new-gameobject/78809/6
    private static string GetPrimitiveMeshPath(PrimitiveType primitiveType)
    {
	    switch (primitiveType)
	    {
		    case PrimitiveType.Sphere:
			    return "New-Sphere.fbx";
		    case PrimitiveType.Capsule:
			    return "New-Capsule.fbx";
		    case PrimitiveType.Cylinder:
			    return "New-Cylinder.fbx";
		    case PrimitiveType.Cube:
			    return "Cube.fbx";
		    case PrimitiveType.Plane:
			    return "New-Plane.fbx";
		    case PrimitiveType.Quad:
			    return "Quad.fbx";
		    default:
			    throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, null);
	    }
    }
    
    //https://discussions.unity.com/t/getting-a-primitive-mesh-without-creating-a-new-gameobject/78809/6
	private static Mesh GetCachedPrimitiveMesh(ref Mesh primMesh, PrimitiveType primitiveType)
	{
		if (primMesh == null)
		{
			Debug.Log("Utils.GetCachedPrimitiveMesh: Getting Unity Primitive Mesh: " + primitiveType);
			primMesh = Resources.GetBuiltinResource<Mesh>(GetPrimitiveMeshPath(primitiveType));

			if (primMesh == null)
			{
				Debug.LogError("Utils.GetCachedPrimitiveMesh: Couldn't load Unity Primitive Mesh: " + primitiveType);
			}
		}

		return primMesh;
	}

	//https://discussions.unity.com/t/getting-a-primitive-mesh-without-creating-a-new-gameobject/78809/6
	public static Mesh GetUnityPrimitiveMesh(PrimitiveType primitiveType)
	{
		switch (primitiveType)
		{
			case PrimitiveType.Sphere:
				return GetCachedPrimitiveMesh(ref _unitySphereMesh, primitiveType);
			case PrimitiveType.Capsule:
				return GetCachedPrimitiveMesh(ref _unityCapsuleMesh, primitiveType);
			case PrimitiveType.Cylinder:
				return GetCachedPrimitiveMesh(ref _unityCylinderMesh, primitiveType);
			case PrimitiveType.Cube:
				return GetCachedPrimitiveMesh(ref _unityCubeMesh, primitiveType);
			case PrimitiveType.Plane:
				return GetCachedPrimitiveMesh(ref _unityPlaneMesh, primitiveType);
			case PrimitiveType.Quad:
				return GetCachedPrimitiveMesh(ref _unityQuadMesh, primitiveType);
			default:
				throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, null);
		}
	}

	public static PrimitiveType ColliderToPrimitiveType(Collider collider)
	{
		switch (collider)
		{
			case SphereCollider:
				return PrimitiveType.Sphere;
			case CapsuleCollider:
				return PrimitiveType.Capsule;
			case BoxCollider:
				return PrimitiveType.Cube;
			default:
				throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
		}
	}

    public static Mesh ColliderToMesh(Collider collider)
    {
        Mesh oldMesh = GetUnityPrimitiveMesh(ColliderToPrimitiveType(collider));
        Mesh newMesh = new Mesh();
        Vector3[] vertices = new Vector3[oldMesh.vertices.Length];

        switch (collider)
        {
            case BoxCollider boxCollider:
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = new Vector3(
                        oldMesh.vertices[i].x * boxCollider.size.x + boxCollider.center.x,
                        oldMesh.vertices[i].y * boxCollider.size.y + boxCollider.center.y,
                        oldMesh.vertices[i].z * boxCollider.size.z + boxCollider.center.z);
                }
                break;
            case CapsuleCollider capsuleCollider:
                //GameObject capsuleGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                break;
            case SphereCollider sphereCollider:
                //GameObject sphereGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                break;
        }

        newMesh.name = oldMesh.name;
        newMesh.vertices = vertices;
        newMesh.triangles = oldMesh.triangles;
        newMesh.RecalculateBounds();
        newMesh.RecalculateNormals();
                
        return newMesh;
    }
}

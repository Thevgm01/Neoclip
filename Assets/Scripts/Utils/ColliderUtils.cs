using System;
using UnityEngine;

public enum ColliderType { Box, Capsule, Sphere }

public static class ColliderUtils
{
	/// <param name="capsuleCollider"></param>
	/// <returns>Vector3.right for X, Vector3.up for Y, Vector3.forward for Z</returns>
	public static Vector3 GetAxis(this CapsuleCollider capsuleCollider)
	{
		switch (capsuleCollider.direction)
		{
			case 0: return Vector3.right;
			case 1: return Vector3.up;
			case 2: return Vector3.forward;
			default: return Vector3.zero;
		}
	}

	public static Vector3 GetCenter(this Collider collider)
	{
		switch (collider)
		{
			case BoxCollider boxCollider: return boxCollider.center;
			case CapsuleCollider capsuleCollider: return capsuleCollider.center;
			case SphereCollider sphereCollider: return sphereCollider.center;
			default: throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
		}
	}
	
	public static float CalculateVolume(this Collider collider)
	{
		switch (collider)
		{
			case BoxCollider boxCollider:
				return boxCollider.size.x * boxCollider.size.y * boxCollider.size.z;
			case CapsuleCollider capsuleCollider:
				return Mathf.PI * capsuleCollider.radius * capsuleCollider.radius * (capsuleCollider.radius * 4.0f / 3.0f + capsuleCollider.height);
			case SphereCollider sphereCollider:
				return Mathf.PI * sphereCollider.radius * sphereCollider.radius * sphereCollider.radius * 4.0f / 3.0f;
			default:
				throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
		}
	}

	public static ColliderType ToColliderType(this Collider collider)
	{
		switch (collider)
		{
			case BoxCollider: return ColliderType.Box;
			case CapsuleCollider: return ColliderType.Capsule;
			case SphereCollider: return ColliderType.Sphere;
			default: throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
		}
	}
	
	public static PrimitiveType ToPrimitiveType(this Collider collider)
	{
		switch (collider)
		{
			case SphereCollider: return PrimitiveType.Sphere;
			case CapsuleCollider: return PrimitiveType.Capsule;
			case BoxCollider: return PrimitiveType.Cube;
			default: throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
		}
	}
	
	// The next few variables and functions are for converting a Collider into a PrimitiveMesh
	// https://discussions.unity.com/t/getting-a-primitive-mesh-without-creating-a-new-gameobject/78809/6

	// Cache the default meshes as we need them
    private static Mesh _unityCapsuleMesh = null;
    private static Mesh _unityCubeMesh = null;
    private static Mesh _unityCylinderMesh = null;
    private static Mesh _unityPlaneMesh = null;
    private static Mesh _unitySphereMesh = null;
    private static Mesh _unityQuadMesh = null;

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
    
	private static Mesh GetCachedPrimitiveMesh(ref Mesh primMesh, PrimitiveType primitiveType)
	{
		if (primMesh == null)
		{
			Debug.Log($"{nameof(ColliderUtils)}.{nameof(GetCachedPrimitiveMesh)}: Getting Unity Primitive Mesh: {primitiveType}");
			primMesh = Resources.GetBuiltinResource<Mesh>(GetPrimitiveMeshPath(primitiveType));

			if (primMesh == null)
			{
				Debug.LogError($"{nameof(ColliderUtils)}.{nameof(GetCachedPrimitiveMesh)}: Couldn't load Unity Primitive Mesh: {primitiveType}");
			}
		}

		return primMesh;
	}

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

	// The pièce de résistance...
    public static Mesh ToMeshWithVertexColor(this Collider collider, Color32 vertexColor)
    {
        Mesh oldMesh = GetUnityPrimitiveMesh(collider.ToPrimitiveType());
        Mesh newMesh = new Mesh();
        Vector3[] vertices = new Vector3[oldMesh.vertices.Length];
        //Color32[] colors32 = new Color32[oldMesh.colors32.Length]; oldMesh.colors32.Length is 0!!!
        Color32[] colors32 = new Color32[vertices.Length];
        
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
	            for (int i = 0; i < vertices.Length; i++)
	            {
		            Vector3 vertex =
			            capsuleCollider.radius * 2.0f * (oldMesh.vertices[i] + Mathf.Sign(oldMesh.vertices[i].y) * Vector3.down) +
			            Mathf.Sign(oldMesh.vertices[i].y) * capsuleCollider.height * 0.5f * Vector3.up;
					
		            // Shuffle the coordinates around based on the capsule's direction
		            switch (capsuleCollider.direction)
		            {
			            case 0:
				            vertices[i] = new Vector3(vertex.y, vertex.z, vertex.x) + capsuleCollider.center;
				            break;
			            case 1:
				            vertices[i] = new Vector3(vertex.x, vertex.y, vertex.z) + capsuleCollider.center;
				            break;
			            case 2:
				            vertices[i] = new Vector3(vertex.z, vertex.x, vertex.y) + capsuleCollider.center;
				            break;
		            }
	            }
	            break;
            case SphereCollider sphereCollider:
	            for (int i = 0; i < vertices.Length; i++)
	            {
		            vertices[i] = sphereCollider.radius * 2.0f * oldMesh.vertices[i] + sphereCollider.center;
	            }
	            break;
            default:
	            throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
        }
        
        for (int i = 0; i < vertices.Length; i++)
        {
	        colors32[i] = vertexColor;
        }
		
        newMesh.name = "Custom" + oldMesh.name;
        newMesh.vertices = vertices;
        newMesh.colors32 = colors32;
        newMesh.triangles = oldMesh.triangles;
        newMesh.RecalculateBounds();
        //newMesh.RecalculateNormals(); // Normals should stay the same I think
                
        return newMesh;
    }

    public static Collider CopyTo(this Collider collider, GameObject gameObject)
    {
	    Collider newCollider;
	    
	    switch (collider)
	    {
		    case BoxCollider boxCollider:
			    BoxCollider newBoxCollider = gameObject.AddComponent<BoxCollider>();
			    newBoxCollider.center = boxCollider.center;
			    newBoxCollider.size = boxCollider.size;
			    newCollider = newBoxCollider;
			    break;
		    case CapsuleCollider capsuleCollider:
			    CapsuleCollider newCapsuleCollider = gameObject.AddComponent<CapsuleCollider>();
			    newCapsuleCollider.center = capsuleCollider.center;
			    newCapsuleCollider.direction = capsuleCollider.direction;
			    newCapsuleCollider.height = capsuleCollider.height;
			    newCapsuleCollider.radius = capsuleCollider.radius;
			    newCollider = newCapsuleCollider;
			    break;
		    case SphereCollider sphereCollider:
			    SphereCollider newSphereCollider = gameObject.AddComponent<SphereCollider>();
			    newSphereCollider.center = sphereCollider.center;
			    newSphereCollider.radius = sphereCollider.radius;
			    newCollider = newSphereCollider;
			    break;
		    default:
			    throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
	    }

	    newCollider.enabled = collider.enabled;
	    newCollider.isTrigger = collider.isTrigger;
	    newCollider.providesContacts = collider.providesContacts;
	    newCollider.sharedMaterial = collider.sharedMaterial;
	    newCollider.layerOverridePriority = collider.layerOverridePriority;
	    newCollider.includeLayers = collider.includeLayers;
	    newCollider.excludeLayers = collider.excludeLayers;
		
	    return newCollider;
    }

    public static BoxcastCommand ToCastCommand(this BoxCollider boxCollider, Vector3 direction, QueryParameters parameters, float distance = float.MaxValue)
    {
	    return new BoxcastCommand(
		    boxCollider.transform.TransformPoint(boxCollider.center),
		    boxCollider.size,
		    boxCollider.transform.rotation,
		    direction,
		    parameters,
		    distance
	    );
    }

    public static CapsulecastCommand ToCastCommand(this CapsuleCollider capsuleCollider, Vector3 direction, QueryParameters parameters, float distance = float.MaxValue)
    {
	    Vector3 axis = capsuleCollider.transform.TransformDirection(capsuleCollider.GetAxis()) * capsuleCollider.height / 2.0f;
	    return new CapsulecastCommand(
		    capsuleCollider.transform.TransformPoint(capsuleCollider.center) + axis,
		    capsuleCollider.transform.TransformPoint(capsuleCollider.center) - axis,
		    capsuleCollider.radius,
		    direction,
		    parameters,
		    distance
	    );
    }
    
    public static SpherecastCommand ToCastCommand(this SphereCollider sphereCollider, Vector3 direction, QueryParameters parameters, float distance = float.MaxValue)
    {
	    return new SpherecastCommand(
		    sphereCollider.transform.TransformPoint(sphereCollider.center),
		    sphereCollider.radius,
		    direction,
		    parameters,
		    distance
	    );
    }

    public static int HashCollider(Collider collider)
    {
	    Hash128 hash = new Hash128();
	    
	    switch (collider)
	    {
            case BoxCollider boxCollider:
	            hash.Append(boxCollider.center.x);
	            hash.Append(boxCollider.center.y);
	            hash.Append(boxCollider.center.z);
	            hash.Append(boxCollider.size.x);
	            hash.Append(boxCollider.size.y);
	            hash.Append(boxCollider.size.z);
	            break;
            case CapsuleCollider capsuleCollider:
	            hash.Append(capsuleCollider.center.x);
	            hash.Append(capsuleCollider.center.y);
	            hash.Append(capsuleCollider.center.z);
	            hash.Append(capsuleCollider.direction);
	            hash.Append(capsuleCollider.radius);
	            hash.Append(capsuleCollider.height);
	            break;
            case SphereCollider sphereCollider:
	            hash.Append(sphereCollider.center.x);
	            hash.Append(sphereCollider.center.y);
	            hash.Append(sphereCollider.center.z);
	            hash.Append(sphereCollider.radius);
	            break;
            default:
	            throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
	    }

	    return hash.GetHashCode();
    }
}

using System;
using UnityEditor;
using UnityEngine;

public static class ClippingUtils
{
    public const float MAX_DISTANCE = 100.0f;
    public const float DOT_THRESHOLD = 0.0f;
    public const int MINIMUM_BACKFACES_TO_BE_INSIDE = 3;

    public const int NUM_CASTS = 6;
    public static readonly Vector3[] CastDirections =
    {
        Vector3.up, Vector3.down,
        Vector3.forward, Vector3.right, Vector3.back, Vector3.left
    };

    public static int VoidColliderInstanceID { get; private set; }
    public static int ShapeCheckLayerMask { get; private set; }
    public static int ShapeCastLayerMask { get; private set; }

    public static void SetVoidCollider(Collider collider)
    {
        VoidColliderInstanceID = collider.GetInstanceID();
    }

    public static void SetLayers(int shapeCheckLayerMask, int shapeCastLayerMask)
    {
        ShapeCheckLayerMask = shapeCheckLayerMask;
        ShapeCastLayerMask = shapeCastLayerMask;
    }
    
    // Should automatically account for the ray not hitting anything, because the normal will be (0, 0, 0) and the dot product will thus be 0
    public static bool HitFrontface(this RaycastHit hit, Vector3 direction) => Vector3.Dot(hit.normal, direction) < DOT_THRESHOLD;
    // Should automatically account for the ray not hitting anything, because the normal will be (0, 0, 0) and the dot product will thus be 0
    public static bool HitBackface(this RaycastHit hit, Vector3 direction) => Vector3.Dot(hit.normal, direction) > DOT_THRESHOLD;
    public static bool HitVoid(this RaycastHit hit) => hit.colliderInstanceID == VoidColliderInstanceID;

    public static bool CheckOrCastRay(Vector3 origin, float radius)
    {
        if (Physics.CheckSphere(origin, Mathf.Max(radius, 0.00001f), ShapeCheckLayerMask))
        {
            return true;
        }
        
        int backfaceHits = 0;
        for (int i = 0; i < CastDirections.Length; i++)
        {
            if (Physics.Raycast(origin, CastDirections[i], out RaycastHit hit, MAX_DISTANCE, ShapeCastLayerMask) &&
                hit.HitBackface(CastDirections[i]) && ++backfaceHits >= MINIMUM_BACKFACES_TO_BE_INSIDE || hit.HitVoid())
            {
                return true;
            }
        }
        
        return false;
    }
    
    public static bool CheckOrCastBox(BoxCollider boxCollider)
    {
        Transform boxTransform = boxCollider.transform;
        Vector3 origin = boxTransform.TransformPoint(boxCollider.center);
        Vector3 halfExtents = boxCollider.size * 0.5f;
        Quaternion orientation = boxTransform.rotation;

        if (Physics.CheckBox(origin, halfExtents, orientation, ShapeCheckLayerMask))
        {
            return true;
        }

        int backfaceHits = 0;
        for (int i = 0; i < CastDirections.Length; i++)
        {
            if (Physics.BoxCast(origin, halfExtents, CastDirections[i], out RaycastHit hit, orientation, MAX_DISTANCE, ShapeCastLayerMask) &&
                hit.HitBackface(CastDirections[i]) && ++backfaceHits >= MINIMUM_BACKFACES_TO_BE_INSIDE || hit.HitVoid())
            {
                return true;
            }
        }
        
        return false;
    }

    public static bool CheckOrCastCapsule(CapsuleCollider capsuleCollider)
    {
        Transform capsuleTransform = capsuleCollider.transform;
            
        Vector3 origin = capsuleTransform.TransformPoint(capsuleCollider.center);
        Vector3 axis = capsuleTransform.TransformDirection(capsuleCollider.height * 0.5f * capsuleCollider.GetAxis());
        Vector3 point1 = origin + axis;
        Vector3 point2 = origin - axis;
        
        if (Physics.CheckCapsule(point1, point2, capsuleCollider.radius, ShapeCheckLayerMask))
        {
            return true;
        }

        int backfaceHits = 0;
        for (int i = 0; i < CastDirections.Length; i++)
        {
            if (Physics.CapsuleCast(point1, point2, capsuleCollider.radius, CastDirections[i], out RaycastHit hit, MAX_DISTANCE, ShapeCastLayerMask) &&
                hit.HitBackface(CastDirections[i]) && ++backfaceHits >= MINIMUM_BACKFACES_TO_BE_INSIDE || hit.HitVoid())
            {
                return true;
            }
        }
        
        return false;
    }
    
    public static bool CheckOrCastSphere(SphereCollider sphereCollider)
    {
        Vector3 origin = sphereCollider.transform.TransformPoint(sphereCollider.center);

        if (Physics.CheckSphere(origin, sphereCollider.radius, ShapeCheckLayerMask))
        {
            return true;
        }

        int backfaceHits = 0;
        for (int i = 0; i < CastDirections.Length; i++)
        {
            if (Physics.SphereCast(origin, sphereCollider.radius, CastDirections[i], out RaycastHit hit, MAX_DISTANCE, ShapeCastLayerMask) &&
                hit.HitBackface(CastDirections[i]) && ++backfaceHits >= MINIMUM_BACKFACES_TO_BE_INSIDE || hit.HitVoid())
            {
                return true;
            }
        }
        
        return false;
    }

    public static bool CheckOrCastCollider(Collider collider)
    {
        switch (collider)
        {
            case BoxCollider boxCollider:
                return CheckOrCastBox(boxCollider);
            case CapsuleCollider capsuleCollider:
                return CheckOrCastCapsule(capsuleCollider);
            case SphereCollider sphereCollider:
                return CheckOrCastSphere(sphereCollider);
            default:
                throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
        }
    }
}

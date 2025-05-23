using System;
using UnityEditor;
using UnityEngine;

public static class ClippingUtils
{
    [Flags]
    public enum ClipState
    {
        None = 0,
        OneHit = 1, // All these "nHit" numbers are mostly there to make it print pretty
        TwoHit = 2,
        ThreeHit = 3,
        FourHit = 4,
        FiveHit = 5,
        SixHit = 6,
        RayBackfaceMask = 7, // First 3 bits used for the number of backface hits
        DidOverlap = 8,
        IsClipping = 16,
        RayHitVoid = 32
    }
    
    public const float MAX_DISTANCE = 100.0f;
    public const float DOT_THRESHOLD = 0.0f;
    public const int MINIMUM_BACKFACES_TO_BE_INSIDE = 2;

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
    public static int GetNumberOfBackfaceFits(this ClipState clipState) => (int)(clipState & ClipState.RayBackfaceMask);
    
    public static ClipState CastRaysDetailed(Vector3 origin)
    {
        ClipState result = new ClipState();
        for (int i = 0; i < CastDirections.Length; i++)
        {
            if (Physics.Raycast(origin, CastDirections[i], out RaycastHit hit, MAX_DISTANCE, ShapeCastLayerMask))
            {
                if (hit.HitBackface(CastDirections[i]))
                {
                    result++; // Add 1 to the total number of ray backface hits
                    
                    if ((int)(result & ClipState.RayBackfaceMask) >= MINIMUM_BACKFACES_TO_BE_INSIDE)
                    {
                        result |= ClipState.IsClipping;
                    }
                }
                
                if (hit.HitVoid())
                {
                    result |= ClipState.IsClipping | ClipState.RayHitVoid;
                }
            }
        }
        return result;
    }
    
    public static bool CastRays(Vector3 origin)
    {
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

    public static ClipState CheckOrCastRaysDetailed(Vector3 origin, float radius)
    {
        return (Physics.CheckSphere(origin, Mathf.Max(radius, 0.00001f), ShapeCheckLayerMask)
            ? ClipState.DidOverlap | ClipState.IsClipping
            : ClipState.None) | CastRaysDetailed(origin);
    }
    
    public static bool CheckOrCastRays(Vector3 origin, float radius)
    {
        if (Physics.CheckSphere(origin, Mathf.Max(radius, 0.00001f), ShapeCheckLayerMask))
        {
            return true;
        }
        
        return CastRays(origin);
    }
    
    public static bool CheckOrCastBoxes(BoxCollider boxCollider)
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

    public static bool CheckOrCastCapsules(CapsuleCollider capsuleCollider)
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
    
    public static bool CheckOrCastSpheres(SphereCollider sphereCollider)
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

    public static ClipState CheckColliderOrCastRaysDetailed(Collider collider)
    {
        switch (collider)
        {
            case BoxCollider boxCollider:
                Transform boxTransform = boxCollider.transform;
                Vector3 boxOrigin = boxTransform.TransformPoint(boxCollider.center);
                return (Physics.CheckBox(boxOrigin, boxCollider.size * 0.5f, boxTransform.rotation, ShapeCheckLayerMask)
                    ? ClipState.DidOverlap | ClipState.IsClipping
                    : ClipState.None) | CastRaysDetailed(boxOrigin);
            
            case CapsuleCollider capsuleCollider:
                Transform capsuleTransform = capsuleCollider.transform;
                Vector3 capsuleOrigin = capsuleTransform.TransformPoint(capsuleCollider.center);
                Vector3 axis = capsuleTransform.TransformDirection(capsuleCollider.height * 0.5f * capsuleCollider.GetAxis());
                return (Physics.CheckCapsule(capsuleOrigin + axis, capsuleOrigin - axis, capsuleCollider.radius, ShapeCheckLayerMask)
                    ? ClipState.DidOverlap | ClipState.IsClipping
                    : ClipState.None) | CastRaysDetailed(capsuleOrigin);
            
            case SphereCollider sphereCollider:
                Transform sphereTransform = sphereCollider.transform;
                Vector3 sphereOrigin = sphereTransform.TransformPoint(sphereCollider.center);
                return (Physics.CheckSphere(sphereOrigin, sphereCollider.radius, ShapeCheckLayerMask)
                    ? ClipState.DidOverlap | ClipState.IsClipping
                    : ClipState.None) | CastRaysDetailed(sphereOrigin);
            
            default:
                throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
        }
    }
    
    public static bool CheckColliderOrCastRays(Collider collider)
    {
        switch (collider)
        {
            case BoxCollider boxCollider:
                Transform boxTransform = boxCollider.transform;
                Vector3 boxOrigin = boxTransform.TransformPoint(boxCollider.center);
                return Physics.CheckBox(boxOrigin, boxCollider.size * 0.5f, boxTransform.rotation, ShapeCheckLayerMask) || CastRays(boxOrigin);
            
            case CapsuleCollider capsuleCollider:
                Transform capsuleTransform = capsuleCollider.transform;
                Vector3 capsuleOrigin = capsuleTransform.TransformPoint(capsuleCollider.center);
                Vector3 axis = capsuleTransform.TransformDirection(capsuleCollider.height * 0.5f * capsuleCollider.GetAxis());
                return Physics.CheckCapsule(capsuleOrigin + axis, capsuleOrigin - axis, capsuleCollider.radius, ShapeCheckLayerMask) || CastRays(capsuleOrigin);
            
            case SphereCollider sphereCollider:
                Transform sphereTransform = sphereCollider.transform;
                Vector3 sphereOrigin = sphereTransform.TransformPoint(sphereCollider.center);
                return Physics.CheckSphere(sphereOrigin, sphereCollider.radius, ShapeCheckLayerMask) || CastRays(sphereOrigin);
            
            default:
                throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
        }
    }
    
    public static bool CheckOrCastColliders(Collider collider)
    {
        switch (collider)
        {
            case BoxCollider boxCollider:
                return CheckOrCastBoxes(boxCollider);
            case CapsuleCollider capsuleCollider:
                return CheckOrCastCapsules(capsuleCollider);
            case SphereCollider sphereCollider:
                return CheckOrCastSpheres(sphereCollider);
            default:
                throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
        }
    }
}

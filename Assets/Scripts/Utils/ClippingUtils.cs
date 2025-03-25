using System;
using UnityEngine;

public static class ClippingUtils
{
    public const float MAX_DISTANCE = 100.0f;
    public const float DOT_THRESHOLD = 0.0f;

    public const int NUM_CASTS = 6;
    public static readonly Vector3[] CastDirections =
    {
        Vector3.up,
        Vector3.forward, Vector3.right, Vector3.back, Vector3.left,
        Vector3.down
    };
    
    // Should automatically account for the ray not hitting anything, because the normal will be (0, 0, 0) and the dot product will thus be 0
    public static bool IsFrontface(this RaycastHit hit, Vector3 direction) => Vector3.Dot(hit.normal, direction) < DOT_THRESHOLD;
    public static bool IsBackface(this RaycastHit hit, Vector3 direction) => Vector3.Dot(hit.normal, direction) > DOT_THRESHOLD;
        
    public static bool CheckOrCastBox(BoxCollider boxCollider, int shapeCheckLayerMask, int shapeCastLayerMask)
    {
        Transform boxTransform = boxCollider.transform;
        Vector3 origin = boxTransform.TransformPoint(boxCollider.center);
        Vector3 halfExtents = boxCollider.size * 0.5f;
        Quaternion orientation = boxTransform.rotation;

        if (Physics.CheckBox(origin, halfExtents, orientation, shapeCheckLayerMask))
        {
            return true;
        }

        for (int i = 0; i < CastDirections.Length; i++)
        {
            if (Physics.BoxCast(origin, halfExtents, CastDirections[i], out RaycastHit hit, orientation, MAX_DISTANCE, shapeCastLayerMask) &&
                hit.IsBackface(CastDirections[i]))
            {
                //Debug.DrawLine(origin, hit.point);
                return true;
            }
        }
        
        return false;
    }

    public static bool CheckOrCastCapsule(CapsuleCollider capsuleCollider, int shapeCheckLayerMask, int shapeCastLayerMask)
    {
        Transform capsuleTransform = capsuleCollider.transform;
            
        Vector3 origin = capsuleTransform.TransformPoint(capsuleCollider.center);
        Vector3 axis = capsuleTransform.TransformDirection(capsuleCollider.GetAxis() * capsuleCollider.height * 0.5f);
        Vector3 point1 = origin + axis;
        Vector3 point2 = origin - axis;
        
        if (Physics.CheckCapsule(point1, point2, capsuleCollider.radius, shapeCheckLayerMask))
        {
            return true;
        }

        for (int i = 0; i < CastDirections.Length; i++)
        {
            if (Physics.CapsuleCast(point1, point2, capsuleCollider.radius, CastDirections[i], out RaycastHit hit, MAX_DISTANCE, shapeCastLayerMask) &&
                hit.IsBackface(CastDirections[i]))
            {
                //Debug.DrawLine(origin, hit.point);
                return true;
            }
        }
        
        return false;
    }
    
    public static bool CheckOrCastSphere(SphereCollider sphereCollider, int shapeCheckLayerMask, int shapeCastLayerMask)
    {
        Vector3 origin = sphereCollider.transform.TransformPoint(sphereCollider.center);

        if (Physics.CheckSphere(origin, sphereCollider.radius, shapeCheckLayerMask))
        {
            return true;
        }

        for (int i = 0; i < CastDirections.Length; i++)
        {
            if (Physics.SphereCast(origin, sphereCollider.radius, CastDirections[i], out RaycastHit hit, MAX_DISTANCE, shapeCastLayerMask) &&
                hit.IsBackface(CastDirections[i]))
            {
                //Debug.DrawLine(origin, hit.point);
                return true;
            }
        }
        
        return false;
    }

    public static bool CheckOrCastCollider(Collider collider, int shapeCheckLayerMask, int shapeCastLayerMask)
    {
        switch (collider)
        {
            case BoxCollider boxCollider:
                return CheckOrCastBox(boxCollider, shapeCheckLayerMask, shapeCastLayerMask);
            case CapsuleCollider capsuleCollider:
                return CheckOrCastCapsule(capsuleCollider, shapeCheckLayerMask, shapeCastLayerMask);
            case SphereCollider sphereCollider:
                return CheckOrCastSphere(sphereCollider, shapeCheckLayerMask, shapeCastLayerMask);
            default:
                throw new ArgumentOutOfRangeException(nameof(collider), collider, null);
        }
    }
}

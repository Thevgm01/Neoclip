using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Serialization;

public class ConcaveClipHelper : MonoBehaviour
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private LayerMask shapeCheckLayerMask;
    [SerializeField] private LayerMask shapeCastLayerMask;

    private const float MAX_DISTANCE = 100.0f;
    private const float DOT_THRESHOLD = 0.0f;
    
    private readonly Vector3[] directions =
    {
        Vector3.up,
        Vector3.forward, Vector3.right, Vector3.back, Vector3.left,
        Vector3.down
    };

    private bool CheckOrCastBox(BoxCollider boxCollider)
    {
        Transform boxTransform = boxCollider.transform;
        Vector3 origin = boxTransform.TransformPoint(boxCollider.center);
        Vector3 halfExtents = boxCollider.size / 2.0f;
        Quaternion orientation = boxTransform.rotation;

        if (Physics.CheckBox(origin, halfExtents, orientation, shapeCheckLayerMask.value))
        {
            return true;
        }

        for (int i = 0; i < directions.Length; i++)
        {
            if (Physics.BoxCast(origin, halfExtents, directions[i], out RaycastHit hit, orientation, MAX_DISTANCE, shapeCastLayerMask.value) &&
                Vector3.Dot(hit.normal, directions[i]) > DOT_THRESHOLD)
            {
                //Debug.DrawLine(origin, hit.point);
                return true;
            }
        }
        
        return false;
    }

    private bool CheckOrCastCapsule(CapsuleCollider capsuleCollider)
    {
        Transform capsuleTransform = capsuleCollider.transform;
            
        Vector3 origin = capsuleTransform.TransformPoint(capsuleCollider.center);
        Vector3 axis = capsuleTransform.TransformDirection(capsuleCollider.GetAxis() * capsuleCollider.height / 2.0f);
        Vector3 point1 = origin + axis;
        Vector3 point2 = origin - axis;
        
        if (Physics.CheckCapsule(point1, point2, capsuleCollider.radius, shapeCheckLayerMask.value))
        {
            return true;
        }

        for (int i = 0; i < directions.Length; i++)
        {
            if (Physics.CapsuleCast(point1, point2, capsuleCollider.radius, directions[i], out RaycastHit hit, MAX_DISTANCE, shapeCastLayerMask.value) &&
                Vector3.Dot(hit.normal, directions[i]) > DOT_THRESHOLD)
            {
                //Debug.DrawLine(origin, hit.point);
                return true;
            }
        }
        
        return false;
    }
    
    private bool CheckOrCastSphere(SphereCollider sphereCollider)
    {
        Vector3 origin = sphereCollider.transform.TransformPoint(sphereCollider.center);

        if (Physics.CheckSphere(origin, sphereCollider.radius, shapeCheckLayerMask.value))
        {
            return true;
        }

        for (int i = 0; i < directions.Length; i++)
        {
            if (Physics.SphereCast(origin, sphereCollider.radius, directions[i], out RaycastHit hit, MAX_DISTANCE, shapeCastLayerMask.value) &&
                Vector3.Dot(hit.normal, directions[i]) > DOT_THRESHOLD)
            {
                //Debug.DrawLine(origin, hit.point);
                return true;
            }
        }
        
        return false;
    }
    
    public bool CheckAllBones(bool[] results)
    {
        bool anythingInside = false;

        for (int i = 0; i < ragdollAverages.NumBones; i++)
        {
            switch (ragdollAverages.GetCollider(i))
            {
                case BoxCollider boxCollider:
                    results[i] = CheckOrCastBox(boxCollider);
                    break;
                case CapsuleCollider capsuleCollider:
                    results[i] = CheckOrCastCapsule(capsuleCollider);
                    break;
                case SphereCollider sphereCollider:
                    results[i] = CheckOrCastSphere(sphereCollider);
                    break;
            }
            
            anythingInside = anythingInside || results[i];
        }

        return anythingInside;
    }
}

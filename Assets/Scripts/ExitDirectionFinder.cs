using System;
using UnityEngine;

[ExecuteInEditMode]
public class ExitDirectionFinder : MonoBehaviour
{
    private void OnDrawGizmosSelected()
    {
        Vector3[] transformedPoints = new Vector3[1000];
        transform.TransformPoints(UniformSpherePoints.GetCachedVectors(1000), transformedPoints);
        
        foreach (Vector3 vec in transformedPoints)
        {
            Gizmos.DrawLine(vec * 0.99f, vec);
        }
    }

    private void FixedUpdate()
    {
        
    }
}

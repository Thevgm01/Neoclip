using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

public class ExitDirectionFinder : MonoBehaviour
{
    private const int NUM_RAYS = 1000;

    [SerializeField] private float distance = 100.0f;
    [SerializeField] private LayerMask layerMask;
    
    private readonly Vector3[] rayDirections = new Vector3[NUM_RAYS];

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            RunRaycasts();
        }
#endif
    
    private void RunRaycasts()
    {
        NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(NUM_RAYS, Allocator.TempJob);
        NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(NUM_RAYS, Allocator.TempJob);

        Vector3 origin = transform.position;
        
        transform.TransformDirections(UniformSpherePoints.GetCachedVectors(NUM_RAYS), rayDirections);
        
        // Cast outward
        QueryParameters outParameters = new QueryParameters()
        {
            layerMask = layerMask.value,
            hitBackfaces = false
        };
        
        for (int i = 0; i < NUM_RAYS; i++)
        {
            commands[i] = new RaycastCommand(origin, rayDirections[i], outParameters, distance);
        }
                
        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1, 1);
        handle.Complete();
        
        // Cast inward
        QueryParameters inParameters = new QueryParameters()
        {
            layerMask = layerMask.value,
            hitBackfaces = false
        };
        
        for (int i = 0; i < NUM_RAYS; i++)
        {
            if (results[i].collider == null)
            {
                commands[i] = new RaycastCommand(origin + rayDirections[i] * distance, -rayDirections[i], inParameters, distance);
            }
            else if (results[i].collider)
            {
                commands[i] = new RaycastCommand(results[i].point, -rayDirections[i], inParameters, results[i].distance);
            }
        }
        
        handle = RaycastCommand.ScheduleBatch(commands, results, 1, 1);
        handle.Complete();
        
        // Process final results
        for (int i = 0; i < NUM_RAYS; i++)
        {
            if (results[i].collider != null)
            {
#if UNITY_EDITOR
                Gizmos.DrawLine(origin, results[i].point);
#endif
            }
        }
        
        results.Dispose();
        commands.Dispose();
    }

    private void FixedUpdate()
    {
        RunRaycasts();
    }
}

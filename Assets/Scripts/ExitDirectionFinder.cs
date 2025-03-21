using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ExitDirectionFinder : MonoBehaviour
{
    private const int NUM_RAYS = 1000;
    private const int NUM_HITS = 5;

    [SerializeField] private float distance = 1.0f;
    [SerializeField] private LayerMask layerMask;
    
    private readonly Vector3[] feelerDirections = new Vector3[NUM_RAYS];

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            RunRaycasts();
        }
#endif

    private bool GetIndexOfLastInsideHit(NativeArray<RaycastHit> results, int startIndex, out int backfaceIndex)
    {
        backfaceIndex = -1;

        for (int i = 0; i < NUM_HITS; i++)
        {
            RaycastHit hit = results[startIndex * NUM_HITS + i];

            if (hit.collider == null)
            {
                return backfaceIndex >= 0;
            }

            float dot = Vector3.Dot(hit.normal, feelerDirections[i]);

            if (backfaceIndex == -1 && dot > 0)
            {
                backfaceIndex = i;
            }
            else if (backfaceIndex >= 0 && dot <= 0)
            {
                return true;
            }
        }

        return false;
    }
    
    private void RunRaycasts()
    {
        NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(NUM_RAYS * NUM_HITS, Allocator.TempJob);
        NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(NUM_RAYS, Allocator.TempJob);

        Vector3 origin = transform.position;
        
        transform.TransformDirections(UniformSpherePoints.GetCachedVectors(NUM_RAYS), feelerDirections);
        
        QueryParameters parameters = new QueryParameters()
        {
            layerMask = layerMask.value,
            hitMultipleFaces = true,
            hitBackfaces = true
        };
        
        for (int i = 0; i < NUM_RAYS; i++)
        {
            commands[i] = new RaycastCommand(origin, feelerDirections[i], parameters, distance);
        }
                
        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1, NUM_HITS);
        
        handle.Complete();

        for (int i = 0; i < NUM_RAYS; i++)
        {
            if (GetIndexOfLastInsideHit(results, i, out int backfaceIndex))
            {
#if UNITY_EDITOR
                Gizmos.DrawLine(origin, results[i * NUM_HITS + backfaceIndex].point);
#endif
            }
        }

        results.Dispose();
        commands.Dispose();
    }
}

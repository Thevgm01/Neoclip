using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ExitDirectionFinder : MonoBehaviour
{
    private const int NUM_RAYS = 1000;
    private const int NUM_HITS = 5;

    [SerializeField] private float distance = 1.0f;
    [SerializeField] private LayerMask layerMask;
    
    private readonly Vector3[] rayDirections = new Vector3[NUM_RAYS];

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            RunRaycasts();
        }
#endif

    private class RaycastComparer : IComparer<RaycastHit>
    {
        public int Compare(RaycastHit a, RaycastHit b)
        {
            if (a.collider == null && b.collider == null) return 0;
            if (a.collider == null) return 1;
            if (b.collider == null) return -1;
            return a.distance.CompareTo(b.distance);
        }
    }
        
    private bool GetIndexOfLastInsideHit(NativeSlice<RaycastHit> hits, out int backfaceIndex)
    {
        backfaceIndex = 0;
        return true;

        /*
        backfaceIndex = -1;

        for (int i = 0; i < NUM_HITS; i++)
        {
            RaycastHit hit = hits[i];

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
        */
    }
    
    private void RunRaycasts()
    {
        Physics.SyncTransforms();
        
        NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(NUM_RAYS * NUM_HITS, Allocator.TempJob);
        NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(NUM_RAYS, Allocator.TempJob);

        Vector3 origin = transform.position;
        
        transform.TransformDirections(UniformSpherePoints.GetCachedVectors(NUM_RAYS), rayDirections);
        
        QueryParameters parameters = new QueryParameters()
        {
            layerMask = layerMask.value,
            hitMultipleFaces = true,
            hitBackfaces = true
        };
        
        for (int i = 0; i < NUM_RAYS; i++)
        {
            commands[i] = new RaycastCommand(origin, rayDirections[i], parameters, distance);
        }
                
        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1, NUM_HITS);
        
        handle.Complete();

        var comparer = new RaycastComparer();
        
        for (int i = 0; i < NUM_RAYS; i++)
        {
            NativeSlice<RaycastHit> singleRaycast = results.Slice(i * NUM_HITS, NUM_HITS);
            singleRaycast.Sort(comparer);
            if (GetIndexOfLastInsideHit(singleRaycast, out int backfaceIndex))
            {
#if UNITY_EDITOR
                Gizmos.DrawLine(origin, singleRaycast[0].point);
#endif
            }
        }

        results.Dispose();
        commands.Dispose();
    }
}

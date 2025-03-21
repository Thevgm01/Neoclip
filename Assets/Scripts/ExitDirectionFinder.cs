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
            for (int j = 0; j < NUM_HITS; j++)
            {
                RaycastHit hit = results[i * NUM_HITS + j];

                if (hit.collider == null)
                {
                    break;
                }
                
                #if UNITY_EDITOR
                    Gizmos.color = new Color32(0, (byte)((j + 1) * 32), 0, 255);
                    Gizmos.DrawLine(
                        j == 0 ? origin : results[i * NUM_HITS + j - 1].point, 
                        hit.point);
                #endif
                //Debug.DrawLine(origin, results[i * NUM_HITS].point);
            }
        }

        results.Dispose();
        commands.Dispose();
    }
}

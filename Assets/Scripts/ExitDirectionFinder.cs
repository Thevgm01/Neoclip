using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ExitDirectionFinder : MonoBehaviour
{
    private const int NUM_RAYS = 1000;

    [SerializeField] private float distance = 100.0f;
    [SerializeField] private LayerMask layerMask;
    
    private readonly NativeArray<Vector3> rayDirections = new (NUM_RAYS, Allocator.Persistent);
    private readonly NativeArray<RaycastHit> results = new (NUM_RAYS, Allocator.Persistent);
    private readonly NativeArray<RaycastCommand> commands = new (NUM_RAYS, Allocator.Persistent);
    
    public struct RayCreationParameters
    {
        public Vector3 origin;
        public NativeArray<Vector3> directions;
        
        public QueryParameters query;
        public float distance;
    }
    
    [BurstCompile]
    public struct CreateOutRays : IJob
    {
        private NativeArray<RaycastCommand> commands;
        [ReadOnly] private RayCreationParameters rayParameters;

        public CreateOutRays(NativeArray<RaycastCommand> commands, RayCreationParameters rayParameters)
        {
            this.commands = commands;
            this.rayParameters = rayParameters;
        }
        
        public void Execute()
        {
            for (int i = 0; i < NUM_RAYS; i++)
            {
                commands[i] = new RaycastCommand(
                    rayParameters.origin,
                    rayParameters.directions[i],
                    rayParameters.query,
                    rayParameters.distance);
            }
        }
    }
    
    [BurstCompile]
    private struct CreateInRays : IJob
    {
        [ReadOnly] private RayCreationParameters rayParameters;
        [ReadOnly] private NativeArray<RaycastHit> hits;
        private NativeArray<RaycastCommand> commands;
        
        public CreateInRays(NativeArray<RaycastCommand> commands, NativeArray<RaycastHit> hits, RayCreationParameters rayParameters)
        {
            this.commands = commands;
            this.hits = hits;
            this.rayParameters = rayParameters;
        }
        
        public void Execute()
        {
            for (int i = 0; i < NUM_RAYS; i++)
            {
                //if (hits[i].collider == null) // Can't do this in a job!
                if (hits[i].point == default) // Hopefully this hack works
                {
                    commands[i] = new RaycastCommand(
                        rayParameters.origin + rayParameters.directions[i] * rayParameters.distance,
                        -rayParameters.directions[i],
                        rayParameters.query,
                        rayParameters.distance);
                }
                else
                {
                    commands[i] = new RaycastCommand(
                        hits[i].point,
                        -rayParameters.directions[i],
                        rayParameters.query,
                        hits[i].distance);
                }
            }
        }
    }
    
    public JobHandle ScheduleJobs()
    {
        transform.TransformDirections(UniformSpherePoints.GetCachedVectors(NUM_RAYS), rayDirections);

        RayCreationParameters rayParameters = new RayCreationParameters
        {
            origin = transform.position,
            directions = rayDirections,
            query = new QueryParameters
            {
                layerMask = layerMask.value,
                hitMultipleFaces = false,
                hitTriggers = QueryTriggerInteraction.UseGlobal,
                hitBackfaces = false
            },
            distance = distance
        };
        
        JobHandle createOutRays = new CreateOutRays(commands, rayParameters).Schedule();
        JobHandle castOutRays = RaycastCommand.ScheduleBatch(commands, results, 1, 1, createOutRays);
        JobHandle createInRays = new CreateInRays(commands, results, rayParameters).Schedule(castOutRays);
        JobHandle castInRays = RaycastCommand.ScheduleBatch(commands, results, 1, 1, createInRays);
        
        return castInRays;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float time = Time.realtimeSinceStartup;
        ScheduleJobs().Complete();
        Debug.Log((Time.realtimeSinceStartup - time) / 1000.0f);

        for (int i = 0; i < NUM_RAYS; i++)
        {
            if (results[i].collider != null)
            {
                Gizmos.DrawLine(transform.position, results[i].point);
            }
        }
    }
#endif
}

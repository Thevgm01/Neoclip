using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ExitDirectionFinder : MonoBehaviour
{
    private const int NUM_RAYS = 2048;

    [SerializeField] private float distance = 100.0f;
    [SerializeField] private LayerMask layerMask;
    
    private readonly NativeArray<Vector3> rayDirections = new (NUM_RAYS, Allocator.Persistent);
    private readonly NativeArray<RaycastHit> results = new (NUM_RAYS, Allocator.Persistent);
    private readonly NativeArray<RaycastCommand> commands = new (NUM_RAYS, Allocator.Persistent);
    private readonly NativeArray<Vector3> exitDirection = new (1, Allocator.Persistent);
    
    public struct RayCreationParameters
    {
        public Vector3 origin;
        public NativeArray<Vector3> directions;
        
        public QueryParameters query;
        public float distance;
    }
    
    [BurstCompile]
    private struct CreateOutRays : IJobFor
    {
        private NativeArray<RaycastCommand> commands;
        [ReadOnly] private RayCreationParameters rayParameters;

        public static JobHandle Schedule(
            NativeArray<RaycastCommand> commands, 
            RayCreationParameters rayParameters, 
            JobHandle dependsOn = default) =>
            new CreateOutRays
            {
                commands = commands,
                rayParameters = rayParameters
            }.ScheduleParallel(NUM_RAYS, 32, dependsOn);
        
        public void Execute(int index)
        {
            commands[index] = new RaycastCommand(
                rayParameters.origin,
                rayParameters.directions[index],
                rayParameters.query,
                rayParameters.distance);
        }
    }
    
    [BurstCompile]
    private struct CreateInRays : IJobFor
    {
        private NativeArray<RaycastCommand> commands;
        [ReadOnly] private NativeArray<RaycastHit> hits;
        [ReadOnly] private RayCreationParameters rayParameters;

        public static JobHandle Schedule(
            NativeArray<RaycastCommand> commands,
            NativeArray<RaycastHit> hits,
            RayCreationParameters rayParameters, 
            JobHandle dependsOn = default) =>
            new CreateInRays
            {
                commands = commands,
                hits = hits,
                rayParameters = rayParameters
            }.ScheduleParallel(NUM_RAYS, 32, dependsOn);
        
        public void Execute(int index)
        {
            //if (hits[i].collider == null) // Can't do this in a job!
            if (hits[index].point == default) // Hopefully this hack works
            {
                commands[index] = new RaycastCommand(
                    rayParameters.origin + rayParameters.directions[index] * rayParameters.distance,
                    -rayParameters.directions[index],
                    rayParameters.query,
                    rayParameters.distance);
            }
            else
            {
                commands[index] = new RaycastCommand(
                    hits[index].point,
                    -rayParameters.directions[index],
                    rayParameters.query,
                    hits[index].distance);
            }
        }
    }

    [BurstCompile]
    private struct CalculateAverageDirection : IJob
    {
        [ReadOnly] private NativeArray<RaycastHit> hits;
        private NativeArray<Vector3> result;
        
        public void Execute()
        {
            
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
        
        return RaycastCommand.ScheduleBatch(commands, results, 1, 1, 
            CreateInRays.Schedule(commands, results, rayParameters, 
                RaycastCommand.ScheduleBatch(commands, results, 1, 1, 
                    CreateOutRays.Schedule(commands, rayParameters))));
        
        JobHandle createOutRays = CreateOutRays.Schedule(commands, rayParameters);
        JobHandle castOutRays = RaycastCommand.ScheduleBatch(commands, results, 1, 1, createOutRays);
        JobHandle createInRays = CreateInRays.Schedule(commands, results, rayParameters, castOutRays);
        JobHandle castInRays = RaycastCommand.ScheduleBatch(commands, results, 1, 1, createInRays);
        
        return castInRays;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        ScheduleJobs().Complete();
        
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

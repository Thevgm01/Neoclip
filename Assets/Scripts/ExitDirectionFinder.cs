using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ExitDirectionFinder : MonoBehaviour
{
    private const int NUM_RAYS = 2048;
    private const int HITS_PER_RAY = 8;

    [SerializeField] private float distance = 100.0f;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private int debugRay = 0;
    
    NativeArray<RaycastHit> rayHits;
    private NativeArray<RaycastHit> finalHits = new (NUM_RAYS, Allocator.Persistent);
    private NativeReference<Vector3> exitDirection = new (Vector3.zero, Allocator.Persistent);
    
    public struct RayCreationParameters
    {
        public Vector3 origin;
        public QueryParameters query;
        public float distance;
    }
    
    [BurstCompile]
    private struct CreateRays : IJobFor
    {
        [ReadOnly] private NativeArray<Vector3> directions;
        private NativeArray<RaycastCommand> commands;
        [ReadOnly] private RayCreationParameters rayParameters;

        public static JobHandle Schedule(
            NativeArray<RaycastCommand> commands, 
            NativeArray<Vector3> directions,
            RayCreationParameters rayParameters,
            int minCommandsPerJob,
            JobHandle dependsOn = default) =>
            new CreateRays
            {
                directions = directions,
                commands = commands,
                rayParameters = rayParameters
            }.ScheduleParallel(NUM_RAYS, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            commands[index] = new RaycastCommand(
                rayParameters.origin,
                directions[index],
                rayParameters.query,
                rayParameters.distance);
        }
    }
    
    [BurstCompile]
    private struct RaycastComparer : IComparer<RaycastHit>
    {
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public int Compare(RaycastHit x, RaycastHit y)
        {
            if (x.colliderInstanceID == 0 && y.colliderInstanceID == 0) return 0;
            if (x.colliderInstanceID == 0) return 1;
            if (y.colliderInstanceID == 0) return -1;
            // This is a stupid hack! Without this it'll stop dead when both rays hit the exact same position
            if (x.distance == y.distance) return x.colliderInstanceID.CompareTo(y.colliderInstanceID);
            return x.distance.CompareTo(y.distance);
        }
    }
    
    [BurstCompile]
    private struct RayProcessor : IJobFor
    {
        [ReadOnly] private NativeArray<Vector3> directions;
        [NativeDisableParallelForRestriction] private NativeArray<RaycastHit> hits;
        private NativeArray<RaycastHit> finalHits;
        [ReadOnly] private RaycastComparer comparer;

        public static JobHandle Schedule(
            NativeArray<Vector3> directions,
            NativeArray<RaycastHit> hits,
            NativeArray<RaycastHit> finalHits,
            int minCommandsPerJob,
            JobHandle dependsOn = default) =>
            new RayProcessor
            {
                directions = directions,
                hits = hits,
                finalHits = finalHits,
                comparer = new RaycastComparer()
            }.ScheduleParallel(NUM_RAYS, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            int rayStartIndex = index * HITS_PER_RAY;
            
            NativeSlice<RaycastHit> myHits = hits.Slice(rayStartIndex, HITS_PER_RAY);
            myHits.Sort(comparer);
            
            bool isCurInside = Vector3.Dot(myHits[0].normal, directions[index]) > 0;
            
            for (int i = 0; i < HITS_PER_RAY - 1; i++)
            {
                bool isNextInside = Vector3.Dot(myHits[i + 1].normal, directions[index]) > 0;
                
                // Innie -> Innie: Continue
                // Innie -> Outie: Continue if near, stop if far
                // Outie -> Innie: Continue
                // Outie -> Outie: Continue
                
                if (myHits[i + 1].colliderInstanceID == 0 || // If the next hit went off into the void, the current hit is the last valid one
                    (isCurInside && !isNextInside && myHits[i + 1].distance > myHits[i].distance + 1.0f)) // If the next hit is far away from the current hit, mark the current hit as the boundary
                {
                    finalHits[index] = myHits[i];
                    return;
                }

                isCurInside = isNextInside;
            }
            // If we reached the end, that means the geometry was so dense that we used every ray
            // So just set the result to be the last ray
            finalHits[index] = myHits[HITS_PER_RAY - 1];
        }
    }

    [BurstCompile]
    private struct CalculateAverageDirection : IJob
    {
        [ReadOnly] private NativeArray<RaycastHit> hits;
        private NativeReference<Vector3> result;
        [ReadOnly] private RayCreationParameters rayCreationParameters;
        
        public static JobHandle Schedule(
            NativeArray<RaycastHit> hits,
            NativeReference<Vector3> result,
            RayCreationParameters rayCreationParameters,
            JobHandle dependsOn = default) =>
            new CalculateAverageDirection
            {
                rayCreationParameters = rayCreationParameters,
                hits = hits,
                result = result,
            }.Schedule(dependsOn);
        
        public void Execute()
        {
            for (int i = 0; i < NUM_RAYS; i++)
            {
                if (hits[i].colliderInstanceID != 0)
                {
                    result.Value += hits[i].point - rayCreationParameters.origin;
                }
            }
            
            result.Value /= -NUM_RAYS;
        }
    }
    
    public JobHandle ScheduleJobs()
    {
        NativeArray<Vector3> rayDirections = new (NUM_RAYS, Allocator.TempJob);
        NativeArray<RaycastCommand> rayCommands = new (NUM_RAYS, Allocator.TempJob);
        rayHits = new (NUM_RAYS * HITS_PER_RAY, Allocator.TempJob);
                
        transform.TransformDirections(UniformSpherePoints.GetCachedVectors(NUM_RAYS), rayDirections);

        RayCreationParameters rayParameters = new RayCreationParameters
        {
            origin = transform.position,
            query = new QueryParameters
            {
                layerMask = layerMask.value,
                hitMultipleFaces = true,
                hitTriggers = QueryTriggerInteraction.UseGlobal,
                hitBackfaces = true
            },
            distance = distance
        };

        exitDirection.Value = Vector3.zero;
        
        JobHandle createRays = CreateRays.Schedule(rayCommands, rayDirections, rayParameters, 32);
        JobHandle raycast = RaycastCommand.ScheduleBatch(rayCommands, rayHits, 1, HITS_PER_RAY, createRays);
        JobHandle processRays = RayProcessor.Schedule(rayDirections, rayHits, finalHits, 16, raycast);
        JobHandle calculateAverageDirection = CalculateAverageDirection.Schedule(finalHits, exitDirection, rayParameters, processRays);
        
        rayCommands.Dispose(raycast);
        rayDirections.Dispose(processRays);
        #if !UNITY_EDITOR
        rayHits.Dispose(processRays);
        #endif

        return calculateAverageDirection;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        ScheduleJobs().Complete();

        if (debugRay < 0)
        {
            Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
            for (int i = 0; i < NUM_RAYS; i++)
            {
                if (finalHits[i].colliderInstanceID != 0)
                {
                    Gizmos.DrawLine(transform.position, finalHits[i].point);
                }
            }
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + exitDirection.Value);
            Gizmos.DrawSphere(transform.position + exitDirection.Value, 0.2f);
        }
        else
        {
            Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            Gizmos.DrawLine(transform.position, finalHits[debugRay].point);
            
            for (int i = 0; i < HITS_PER_RAY; i++)
            {
                int rayIndex = debugRay * HITS_PER_RAY + i;
                Gizmos.color = Color.HSVToRGB((float)i / HITS_PER_RAY, 1.0f, 1.0f);
                Gizmos.DrawSphere(rayHits[rayIndex].point, 0.2f);
                Debug.Log($"{rayHits[rayIndex].colliderInstanceID} / {rayHits[rayIndex].distance} / {rayHits[rayIndex].point}");
            }
        }

        rayHits.Dispose();
    }
#endif
}

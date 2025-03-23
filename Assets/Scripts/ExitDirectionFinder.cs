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
    [SerializeField] private bool showBalls = true;
    
    NativeArray<RaycastHit> rayOutHits;
    NativeArray<RaycastHit> rayInHits;
    private NativeArray<RaycastHit> finalHits = new (NUM_RAYS, Allocator.Persistent);
    private NativeReference<Vector3> exitDirection = new (Vector3.zero, Allocator.Persistent);
    
    public struct RayCreationParameters
    {
        public Vector3 origin;
        public QueryParameters query;
        public float distance;
    }
    
    [BurstCompile]
    private struct CreateOutRays : IJobFor
    {
        [ReadOnly] private NativeArray<Vector3> directions;
        [ReadOnly] private RayCreationParameters rayParameters;
        private NativeArray<RaycastCommand> outCommands;
        
        public static JobHandle Schedule(
            NativeArray<Vector3> directions,
            RayCreationParameters rayParameters,
            NativeArray<RaycastCommand> outCommands,
            int minCommandsPerJob,
            JobHandle dependsOn = default) =>
            new CreateOutRays
            {
                directions = directions,
                outCommands = outCommands,
                rayParameters = rayParameters
            }.ScheduleParallel(NUM_RAYS, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            outCommands[index] = new RaycastCommand(
                rayParameters.origin,
                directions[index],
                rayParameters.query,
                rayParameters.distance);
        }
    }
    
    private struct CreateInRays : IJobFor
    {
        [ReadOnly] private NativeArray<RaycastHit> outHits;
        [ReadOnly] private NativeArray<Vector3> directions;
        [ReadOnly] private RayCreationParameters rayParameters;
        private NativeArray<RaycastCommand> inCommands;
        
        public static JobHandle Schedule(
            NativeArray<RaycastHit> outHits,
            NativeArray<Vector3> directions,
            RayCreationParameters rayParameters,
            NativeArray<RaycastCommand> inCommands,
            int minCommandsPerJob,
            JobHandle dependsOn = default) =>
            new CreateInRays
            {
                outHits = outHits,
                directions = directions,
                inCommands = inCommands,
                rayParameters = rayParameters
            }.ScheduleParallel(NUM_RAYS * HITS_PER_RAY, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            int outIndex = index / HITS_PER_RAY;
            inCommands[index] = new RaycastCommand(
                outHits[outIndex].colliderInstanceID == 0
                    ? rayParameters.origin + directions[outIndex] * rayParameters.distance
                    : outHits[outIndex].point,
                -directions[outIndex],
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
        [NativeDisableParallelForRestriction] private NativeArray<RaycastHit> outHits;
        [NativeDisableParallelForRestriction] private NativeArray<RaycastHit> inHits;
        private NativeArray<RaycastHit> finalHits;
        [ReadOnly] private RaycastComparer comparer;

        public static JobHandle Schedule(
            NativeArray<RaycastHit> outHits,
            NativeArray<RaycastHit> inHits,
            NativeArray<Vector3> directions,
            NativeArray<RaycastHit> finalHits,
            int minCommandsPerJob,
            JobHandle dependsOn = default) =>
            new RayProcessor
            {
                directions = directions,
                outHits = outHits,
                inHits = inHits,
                finalHits = finalHits,
                comparer = new RaycastComparer()
            }.ScheduleParallel(NUM_RAYS, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            /*
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
    
            */
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
        NativeArray<RaycastCommand> rayOutCommands = new (NUM_RAYS, Allocator.TempJob);
        NativeArray<RaycastCommand> rayInCommands = new (NUM_RAYS * HITS_PER_RAY, Allocator.TempJob);
        rayOutHits = new (NUM_RAYS * HITS_PER_RAY, Allocator.TempJob);
        rayInHits = new (NUM_RAYS * HITS_PER_RAY, Allocator.TempJob);
                
        transform.TransformDirections(UniformSpherePoints.GetCachedVectors(NUM_RAYS), rayDirections);

        RayCreationParameters rayParameters = new RayCreationParameters
        {
            origin = transform.position,
            query = new QueryParameters
            {
                layerMask = layerMask.value,
                hitMultipleFaces = true,
                hitTriggers = QueryTriggerInteraction.UseGlobal,
                hitBackfaces = false
            },
            distance = distance
        };

        exitDirection.Value = Vector3.zero;
        
        JobHandle createOutRays = CreateOutRays.Schedule(rayDirections, rayParameters, rayOutCommands, 32);
        JobHandle raycastOut = RaycastCommand.ScheduleBatch(rayOutCommands, rayOutHits, 1, HITS_PER_RAY, createOutRays);
        JobHandle createInRays = CreateInRays.Schedule(rayOutHits, rayDirections, rayParameters, rayInCommands, 32, raycastOut);
        JobHandle raycastIn = RaycastCommand.ScheduleBatch(rayInCommands, rayInHits, 1, 1, createInRays);

        JobHandle processRays = RayProcessor.Schedule(rayOutHits, rayInHits, rayDirections, finalHits, 16, raycastIn);
        JobHandle calculateAverageDirection = CalculateAverageDirection.Schedule(finalHits, exitDirection, rayParameters, processRays);
        
        rayOutCommands.Dispose(raycastIn);
        rayInCommands.Dispose(raycastIn);
        rayDirections.Dispose(processRays);
        #if !UNITY_EDITOR
        rayOutHits.Dispose(processRays);
        rayInHits.Dispose(processRays);
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

            //Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
            //Gizmos.DrawLine(transform.position, rayOutHits[(debugRay + 1) * HITS_PER_RAY - 1].point);
            
            for (int i = 0; i < HITS_PER_RAY; i++)
            {
                int rayIndex = debugRay * HITS_PER_RAY + i;

                Vector3 rayNormal = rayOutHits[rayIndex].normal;
                Vector3 colorNormal = (rayNormal + Vector3.one) * 0.5f;
                Gizmos.color = new Color(colorNormal.x, colorNormal.y, colorNormal.z, 1.0f);
                Gizmos.DrawLine(rayOutHits[rayIndex].point, rayOutHits[rayIndex].point + rayNormal);
                
                if (showBalls)
                {
                    Gizmos.color = Color.HSVToRGB((float)i / HITS_PER_RAY, 1.0f, 1.0f);
                    Gizmos.DrawWireSphere(rayOutHits[rayIndex].point, 0.2f);
                    Gizmos.DrawWireCube(rayInHits[rayIndex].point, Vector3.one * 0.4f);
                    Debug.Log(
                        $"{rayOutHits[rayIndex].colliderInstanceID} / {rayOutHits[rayIndex].distance} / {rayOutHits[rayIndex].point}");
                }
            }
        }

        rayOutHits.Dispose();
        rayInHits.Dispose();
    }
#endif
}

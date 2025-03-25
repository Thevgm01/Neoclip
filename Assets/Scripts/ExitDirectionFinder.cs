using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ExitDirectionFinder : MonoBehaviour
{
    private const float CHECK_RADIUS = 20.0f;
    private const float CHECK_SPACING = 2.0f;
    
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private int debugRay = 0;
    [SerializeField] private bool showBalls = true;
    [SerializeField] private bool printJobTime = true;
        
    private NativeReference<Vector3> exitDirection = new (Vector3.zero, Allocator.Persistent);
    
    [BurstCompile]
    private struct CreateRayCommands : IJobFor
    {
        [NativeDisableParallelForRestriction] private NativeArray<RaycastCommand> rayCommands;
        [ReadOnly] private NativeArray<Vector3> points;
        [ReadOnly] private QueryParameters queryParameters;
                        
        public static JobHandle ScheduleBatch(NativeArray<RaycastCommand> rayCommands, NativeArray<Vector3> points,
                QueryParameters queryParameters, int minCommandsPerJob, JobHandle dependsOn = default) =>
            new CreateRayCommands
            {
                rayCommands = rayCommands,
                points = points,
                queryParameters = queryParameters
            }.ScheduleParallel(points.Length, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            for (int i = 0; i < ClippingUtils.NUM_CASTS; i++)
            {
                rayCommands[index * ClippingUtils.NUM_CASTS + i] = new RaycastCommand
                {
                    direction = ClippingUtils.CastDirections[i],
                    distance = ClippingUtils.MAX_DISTANCE,
                    from = points[index],
                    queryParameters = queryParameters
                };
            }
        }
    }
    
    [BurstCompile]
    private struct CountBackfaceHits : IJobFor
    {
        private NativeArray<byte> pointBackfaceHits;
        [ReadOnly] private NativeArray<RaycastHit> rayHits;
              
        public static JobHandle ScheduleBatch(NativeArray<byte> pointBackfaceHits,
                NativeArray<RaycastHit> rayHits, int minCommandsPerJob, JobHandle dependsOn = default) =>
            new CountBackfaceHits
            {
                pointBackfaceHits = pointBackfaceHits,
                rayHits = rayHits
            }.ScheduleParallel(pointBackfaceHits.Length, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            for (int i = 0; i < ClippingUtils.NUM_CASTS; i++)
            {
                pointBackfaceHits[index] += 
                    rayHits[index * ClippingUtils.NUM_CASTS + i].IsBackface(ClippingUtils.CastDirections[i]) ? (byte)1 : (byte)0;
            }
        }
    }
    
    [BurstCompile]
    private struct CalculateAverageDirection : IJob
    {
        private NativeReference<Vector3> result;
        [ReadOnly] private NativeArray<Vector3> points;
        [ReadOnly] private NativeArray<byte> pointBackfaceHits;
        [ReadOnly] private Vector3 origin;
        
        public static JobHandle Schedule(NativeReference<Vector3> result, NativeArray<Vector3> points,
                NativeArray<byte> pointBackfaceHits, Vector3 origin, JobHandle dependsOn = default) =>
            new CalculateAverageDirection
            {
                result = result,
                points = points,
                pointBackfaceHits = pointBackfaceHits,
                origin = origin
            }.Schedule(dependsOn);
        
        public void Execute()
        {
            for (int index = 0; index < points.Length; index++)
            {
                if (pointBackfaceHits[index] <= 2)
                {
                    result.Value += points[index] - origin;
                }
            }

            result.Value *= 2.0f / points.Length;
        }
    }
    
    public JobHandle ScheduleJobs(bool displayDebug = false)
    {
        //Vector3[] rawPoints = CachedSpherePoints.solidSphereInstance.Get((CHECK_RADIUS, CHECK_SPACING));
        Vector3[] rawPoints = new Vector3[2048];
        CachedSpherePoints.goldenSpiralShellInstance.Get(2048).CopyTo(rawPoints, 0);
        for (int i = 0; i < rawPoints.Length; i++)
        {
            rawPoints[i] *= Mathf.Pow(1.5f, ((i * 3141592653) % 2048) / 300.0f);
        }
        
        int numPoints = rawPoints.Length;
        NativeArray<Vector3> points = new(numPoints, Allocator.TempJob);
        NativeArray<RaycastCommand> rayCommands = new (numPoints * ClippingUtils.NUM_CASTS, Allocator.TempJob);
        NativeArray<RaycastHit> rayHits = new (numPoints * ClippingUtils.NUM_CASTS, Allocator.TempJob);
        NativeArray<byte> pointBackfaceHits = new (numPoints, Allocator.TempJob);
        
        Vector3 origin = transform.position;
        transform.TransformPoints(rawPoints, points);

        QueryParameters queryParameters = new QueryParameters
        {
            layerMask = layerMask.value,
            hitMultipleFaces = false,
            hitTriggers = QueryTriggerInteraction.UseGlobal,
            hitBackfaces = true
        };

        exitDirection.Value = Vector3.zero;
        
        JobHandle createRayCommands = CreateRayCommands.ScheduleBatch(rayCommands, points, queryParameters, 1);
        JobHandle raycast = RaycastCommand.ScheduleBatch(rayCommands, rayHits, 1, createRayCommands);
        JobHandle countBackfaceHits = CountBackfaceHits.ScheduleBatch(pointBackfaceHits, rayHits, 1, raycast);
        JobHandle calculateAverageDirection = CalculateAverageDirection.Schedule(exitDirection, points, pointBackfaceHits, origin, countBackfaceHits);
        
#if UNITY_EDITOR
        if (displayDebug)
        {
            double time = Time.realtimeSinceStartupAsDouble;
            calculateAverageDirection.Complete();
            if (printJobTime)
            {
                Debug.Log((Time.realtimeSinceStartupAsDouble - time) * 1000.0);
            }
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position + exitDirection.Value, 0.1f);
            Gizmos.DrawLine(transform.position, transform.position + exitDirection.Value);
            
            if (showBalls)
            {
                for (int i = 0; i < numPoints; i++)
                {
                    Gizmos.color = new Color((float)pointBackfaceHits[i] / ClippingUtils.NUM_CASTS, 0.0f, 0.0f, 1.0f);
                    Gizmos.DrawSphere(points[i], 0.05f);
                }
            }

            if (debugRay >= 0)
            {
                for (int i = 0; i < ClippingUtils.NUM_CASTS; i++)
                {
                    int hitIndex = debugRay * ClippingUtils.NUM_CASTS + i;
                    if (rayHits[hitIndex].colliderInstanceID != 0)
                    {
                        Gizmos.color = rayHits[hitIndex].IsBackface(ClippingUtils.CastDirections[i])
                            ? Color.green
                            : Color.blue;
                        Gizmos.DrawLine(points[debugRay], rayHits[hitIndex].point);
                    }
                }
            }

            debugRay = Mathf.Clamp(debugRay, -1, numPoints - 1);

            rayCommands.Dispose();
            rayHits.Dispose();
            points.Dispose();
            pointBackfaceHits.Dispose();
            return default;
        }
        else
        {
            rayCommands.Dispose(raycast);
            rayHits.Dispose(countBackfaceHits);
            points.Dispose(calculateAverageDirection);
            pointBackfaceHits.Dispose(calculateAverageDirection);
            return calculateAverageDirection;
        }
#else
        rayCommands.Dispose(raycast);
        rayHits.Dispose(countBackfaceHits);
        points.Dispose(calculateAverageDirection);
        pointBackfaceHits.Dispose(calculateAverageDirection);
        return calculateAverageDirection;
#endif
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        ScheduleJobs(true);
    }
#endif
}

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

public class ExitDirectionFinder : MonoBehaviour
{
    private const float CHECK_RADIUS = 20.0f;
    private const float CHECK_SPACING = 2.0f;
    private const int NUM_POINTS = 2048;
    
    private static Vector3[] rawPoints;
    
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private int debugRay = 0;
    [SerializeField] private bool showBalls = true;
    [SerializeField] private bool printJobTime = true;
    
    private NativeReference<Vector3> exitDirection = new (Vector3.zero, Allocator.Persistent);
    public Vector3 ExitDirection => exitDirection.Value;
    
    [BurstCompile]
    private struct CreateCommands : IJobFor
    {
        private NativeArray<OverlapSphereCommand> overlapCommands;
        [NativeDisableParallelForRestriction] private NativeArray<RaycastCommand> rayCommands;
        [ReadOnly] private NativeArray<Vector3> points;
        [ReadOnly] private QueryParameters queryParameters;
        
        public static JobHandle ScheduleBatch(NativeArray<OverlapSphereCommand> overlapCommands, NativeArray<RaycastCommand> rayCommands,
                NativeArray<Vector3> points, QueryParameters queryParameters, int minCommandsPerJob, JobHandle dependsOn = default) =>
            new CreateCommands
            {
                rayCommands = rayCommands,
                overlapCommands = overlapCommands,
                points = points,
                queryParameters = queryParameters
            }.ScheduleParallel(points.Length, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            overlapCommands[index] = new OverlapSphereCommand
            {
                point = points[index],
                queryParameters = queryParameters,
                radius = 0.1f
            };
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
        [ReadOnly] private NativeArray<ColliderHit> overlapHits;
        [ReadOnly] private NativeArray<RaycastHit> rayHits;
              
        public static JobHandle ScheduleBatch(NativeArray<byte> pointBackfaceHits, NativeArray<ColliderHit> overlapHits,
                NativeArray<RaycastHit> rayHits, int minCommandsPerJob, JobHandle dependsOn = default) =>
            new CountBackfaceHits
            {
                pointBackfaceHits = pointBackfaceHits,
                overlapHits = overlapHits,
                rayHits = rayHits
            }.ScheduleParallel(pointBackfaceHits.Length, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            if (overlapHits[index].instanceID != 0)
            {
                pointBackfaceHits[index] = ClippingUtils.NUM_CASTS;
            }
            else
            {
                for (int i = 0; i < ClippingUtils.NUM_CASTS; i++)
                {
                    pointBackfaceHits[index] +=
                        rayHits[index * ClippingUtils.NUM_CASTS + i].IsBackface(ClippingUtils.CastDirections[i])
                            ? (byte)1
                            : (byte)0;
                }
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
    
    public JobHandle ScheduleJobs(bool drawGizmos = false)
    {
        NativeArray<Vector3> points = new(NUM_POINTS, Allocator.TempJob);
        
        NativeArray<OverlapSphereCommand> overlapCommands = new(NUM_POINTS, Allocator.TempJob);
        NativeArray<ColliderHit> overlapHits = new(NUM_POINTS, Allocator.TempJob);
        
        NativeArray<RaycastCommand> rayCommands = new (NUM_POINTS * ClippingUtils.NUM_CASTS, Allocator.TempJob);
        NativeArray<RaycastHit> rayHits = new (NUM_POINTS * ClippingUtils.NUM_CASTS, Allocator.TempJob);
        
        NativeArray<byte> pointBackfaceHits = new (NUM_POINTS, Allocator.TempJob);
        
        transform.TransformPoints(rawPoints, points);

        QueryParameters queryParameters = new QueryParameters
        {
            layerMask = layerMask.value,
            hitMultipleFaces = false,
            hitTriggers = QueryTriggerInteraction.UseGlobal,
            hitBackfaces = true
        };
        
        exitDirection.Value = Vector3.zero;
        
        JobHandle createCommands = CreateCommands.ScheduleBatch(overlapCommands, rayCommands, points, queryParameters, 1);
        
        JobHandle overlapSpheres = OverlapSphereCommand.ScheduleBatch(overlapCommands, overlapHits, 1, 1, createCommands);
        JobHandle raycasts = RaycastCommand.ScheduleBatch(rayCommands, rayHits, 1, 1, createCommands);
        JobHandle physicsChecks = JobHandle.CombineDependencies(overlapSpheres, raycasts);
        
        JobHandle countBackfaceHits = CountBackfaceHits.ScheduleBatch(pointBackfaceHits, overlapHits, rayHits, 1, physicsChecks);
        
        JobHandle calculateAverageDirection = CalculateAverageDirection.Schedule(exitDirection, points, pointBackfaceHits, transform.position, countBackfaceHits);
        
#if UNITY_EDITOR
        if (drawGizmos)
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
                for (int i = 0; i < NUM_POINTS; i++)
                {
                    Gizmos.color = overlapHits[i].instanceID != 0 ? Color.magenta :
                        new Color((float)pointBackfaceHits[i] / ClippingUtils.NUM_CASTS, 0.0f, 0.0f, 1.0f);
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

            debugRay = Mathf.Clamp(debugRay, -1, NUM_POINTS - 1);

            overlapCommands.Dispose();
            overlapHits.Dispose();
            rayCommands.Dispose();
            rayHits.Dispose();
            points.Dispose();
            pointBackfaceHits.Dispose();
            return default;
        }
        else
        {
#endif
        overlapCommands.Dispose(overlapSpheres);
        overlapHits.Dispose(countBackfaceHits);
        rayCommands.Dispose(raycasts);
        rayHits.Dispose(countBackfaceHits);
        points.Dispose(calculateAverageDirection);
        pointBackfaceHits.Dispose(calculateAverageDirection);
        return calculateAverageDirection;
#if UNITY_EDITOR
        }
#endif
    }
    
    [RuntimeInitializeOnLoadMethod]
    private static void SetRawPoints()
    {
        rawPoints = SpherePointGenerator.GenerateGoldenSpiralShell(NUM_POINTS);
        for (int i = 0; i < NUM_POINTS; i++)
        {
            // These numbers are shamelessly hardcoded to facilitate a relatively-evenly-spread arrangement
            // with the points generally clustered closer to the center
            rawPoints[i] *= Mathf.Pow(1.5f, ((i * 3141592653) % NUM_POINTS) / 300.0f);
        }
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Selection.activeGameObject == this.gameObject)
        {
            if (rawPoints == null) SetRawPoints();
            ScheduleJobs(true);
        }
    }
#endif
    
    private void Awake()
    {
        SetRawPoints();
    }
}

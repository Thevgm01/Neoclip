using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

public class ExitDirectionFinder : MonoBehaviour
{
    private const int NUM_POINTS = 2048;
    
    private static Vector3[] rawPoints;
    
    [SerializeField] private bool showBalls = true;
    [SerializeField] private bool printJobTime = true;
    
    public JobHandle MainJob { get; private set; }
    
    private NativeReference<Vector3> exitDirection = new (Vector3.zero, Allocator.Persistent);
    public Vector3 GetExitDirection() => exitDirection.Value;
    public Vector3 ResetExitDirection() => exitDirection.Value = Vector3.zero;
        
    #if UNITY_EDITOR
    private bool shouldDrawGizmos;
    #endif
    
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
        [ReadOnly] private int voidColliderInstanceID;
              
        public static JobHandle ScheduleBatch(NativeArray<byte> pointBackfaceHits, NativeArray<ColliderHit> overlapHits,
                NativeArray<RaycastHit> rayHits, int voidColliderInstanceID, int minCommandsPerJob, JobHandle dependsOn = default) =>
            new CountBackfaceHits
            {
                pointBackfaceHits = pointBackfaceHits,
                overlapHits = overlapHits,
                rayHits = rayHits,
                voidColliderInstanceID = voidColliderInstanceID
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
                    if (rayHits[index * ClippingUtils.NUM_CASTS + i].colliderInstanceID == voidColliderInstanceID)
                    {
                        pointBackfaceHits[index] = ClippingUtils.NUM_CASTS;
                        return;
                    }
                    pointBackfaceHits[index] +=
                        (byte)(rayHits[index * ClippingUtils.NUM_CASTS + i].HitBackface(ClippingUtils.CastDirections[i]) ? 1 : 0);
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
            result.Value = Vector3.zero;
            
            for (int index = 0; index < points.Length; index++)
            {
                if (pointBackfaceHits[index] < ClippingUtils.MINIMUM_BACKFACES_TO_BE_INSIDE)
                {
                    result.Value += points[index] - origin;
                }
            }

            result.Value *= 2.0f / points.Length;
        }
    }
    
    public void ScheduleJobs()
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
            layerMask = ClippingUtils.ShapeCastLayerMask,
            hitMultipleFaces = false,
            hitTriggers = QueryTriggerInteraction.UseGlobal,
            hitBackfaces = true
        };
        
        JobHandle createCommands = CreateCommands.ScheduleBatch(overlapCommands, rayCommands, points, queryParameters, 1);
        
        JobHandle overlapSpheres = OverlapSphereCommand.ScheduleBatch(overlapCommands, overlapHits, 1, 1, createCommands);
        JobHandle raycasts = RaycastCommand.ScheduleBatch(rayCommands, rayHits, 1, 1, createCommands);
        JobHandle physicsChecks = JobHandle.CombineDependencies(overlapSpheres, raycasts);
        
        JobHandle countBackfaceHits = CountBackfaceHits.ScheduleBatch(pointBackfaceHits, overlapHits, rayHits, ClippingUtils.VoidColliderInstanceID, 1, physicsChecks);
        
        MainJob = CalculateAverageDirection.Schedule(exitDirection, points, pointBackfaceHits, transform.position, countBackfaceHits);
        
#if UNITY_EDITOR
        if (shouldDrawGizmos)
        {
            double time = Time.realtimeSinceStartupAsDouble;
            MainJob.Complete();
            if (printJobTime)
            {
                Debug.Log((Time.realtimeSinceStartupAsDouble - time) * 1000.0);
            }
            
            // Hits
            if (showBalls)
            {
                for (int i = 0; i < NUM_POINTS; i++)
                {
                    GizmoAnywhere.SubmitRequest(new GizmoAnywhere.GizmoDrawRequest
                    {
                        owner = this.transform, criteria = GizmoAnywhere.DrawCriteria.SELECTED_EXCLUSIVE, shape = GizmoAnywhere.Shape.SPHERE,
                        position = points[i], radius = 0.05f, ragdollRelative = GizmoAnywhere.RagdollRelative.TRUE,
                        color = overlapHits[i].instanceID != 0
                            ? Color.magenta
                            : new Color((float)pointBackfaceHits[i] / ClippingUtils.NUM_CASTS, 0.0f, 0.0f, 1.0f)
                    });
                }
            }
            
            // Exit direction
            for (int i = 0; i < 10; i++)
            {
                GizmoAnywhere.SubmitRequest(new GizmoAnywhere.GizmoDrawRequest
                {
                    owner = this.transform, criteria = GizmoAnywhere.DrawCriteria.SELECTED_EXCLUSIVE, shape = GizmoAnywhere.Shape.SPHERE,
                    position = transform.position + Vector3.Lerp(Vector3.zero, exitDirection.Value, i / 10.0f),
                    radius = Mathf.Sqrt((i + 1) / 10.0f) * 0.2f, ragdollRelative = GizmoAnywhere.RagdollRelative.TRUE,
                    color = Color.green
                });
            }
            
            shouldDrawGizmos = false;
        }
#endif

        overlapCommands.Dispose(overlapSpheres);
        overlapHits.Dispose(countBackfaceHits);
        rayCommands.Dispose(raycasts);
        rayHits.Dispose(countBackfaceHits);
        points.Dispose(MainJob);
        pointBackfaceHits.Dispose(MainJob);
    }
    
    [RuntimeInitializeOnLoadMethod]
    private static void SetRawPoints()
    {
        rawPoints = SpherePointGenerator.GenerateGoldenSpiralShell(NUM_POINTS);
        for (int i = 0; i < NUM_POINTS; i++)
        {
            // These numbers are shamelessly hardcoded to facilitate a relatively-evenly-spread arrangement
            // with the points generally clustered closer to the center
            rawPoints[i] *= Mathf.Pow(1.25f, ((i * 3141592653) % NUM_POINTS) / 200.0f);
        }
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Selection.activeGameObject == this.gameObject)
        {
            shouldDrawGizmos = true;
            
            if (!Application.isPlaying)
            {
                if (rawPoints == null)
                {
                    SetRawPoints();
                }
                ScheduleJobs();
            }
        }
    }
#endif
    
    public void Awake()
    {
        SetRawPoints();
    }
}

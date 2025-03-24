using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ExitDirectionFinder : MonoBehaviour
{
    private const int NUM_RAYS = 2048;
    private const int NUM_RETRIES = 8;
    private const float RAY_SEPARATION = 0.1f;
    
    [SerializeField] private float distance = 100.0f;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private int debugRay = 0;
    [SerializeField] private bool showBalls = true;
    
    private NativeReference<Vector3> exitDirection = new (Vector3.zero, Allocator.Persistent);
    
    private struct RayCreationParameters
    {
        public Vector3 origin;
        public QueryParameters query;
        public QueryParameters nullQuery;
        public float distance;
    }

    private struct RaycastData
    {
        public Vector3 point;
        public float distance;
        public float cumulativeDistance;
        public bool hitAnything;
        public bool hitBackface;
    }
    
    [BurstCompile]
    private struct CreateRays : IJobFor
    {
        private NativeArray<RaycastCommand> rayCommands;
        [ReadOnly] private NativeArray<RaycastData> rayData;
        [ReadOnly] private NativeArray<Vector3> directions;
        [ReadOnly] private RayCreationParameters rayParameters;
        [ReadOnly] private int retryIndex;
                
        public static JobHandle ScheduleBatch(
            NativeArray<RaycastCommand> rayCommands, 
            NativeArray<RaycastData> rayData,
            NativeArray<Vector3> directions,
            RayCreationParameters rayParameters,
            int retryIndex,
            int minCommandsPerJob,
            JobHandle dependsOn = default) =>
            new CreateRays
            {
                rayCommands = rayCommands,
                rayData = rayData,
                directions = directions,
                rayParameters = rayParameters,
                retryIndex = retryIndex
            }.ScheduleParallel(NUM_RAYS, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            if (retryIndex == 0)
            {
                rayCommands[index] = new RaycastCommand
                {
                    direction = directions[index],
                    distance = rayParameters.distance,
                    from = rayParameters.origin,
                    queryParameters = rayParameters.query
                };
            }
            else
            {
                int prevImportantBitsIndex = index * NUM_RETRIES + retryIndex - 1;

                if (!rayData[prevImportantBitsIndex].hitAnything ||
                    rayData[prevImportantBitsIndex].cumulativeDistance >= rayParameters.distance - 1.0f)
                {
                    rayCommands[index] = new RaycastCommand
                    {
                        queryParameters = rayParameters.nullQuery
                    };
                }
                else
                {
                    rayCommands[index] = new RaycastCommand
                    {
                        direction = directions[index],
                        distance = rayParameters.distance - rayData[prevImportantBitsIndex].cumulativeDistance - RAY_SEPARATION,
                        from = rayData[prevImportantBitsIndex].point + directions[index] * RAY_SEPARATION,
                        queryParameters = rayParameters.query
                    };
                }
            }
        }
    }
    
    [BurstCompile]
    private struct ProcessHits : IJobFor
    {
        [NativeDisableParallelForRestriction] private NativeArray<RaycastData> rayData;
        [ReadOnly] private NativeArray<Vector3> directions;
        [ReadOnly] private NativeArray<RaycastHit> rayHits;
        [ReadOnly] private int retryIndex;
        
        public static JobHandle ScheduleBatch(
            NativeArray<RaycastData> rayData,
            NativeArray<Vector3> directions,
            NativeArray<RaycastHit> rayHits,
            int retryIndex,
            int minCommandsPerJob,
            JobHandle dependsOn = default) =>
            new ProcessHits
            {
                rayData = rayData,
                directions = directions,
                retryIndex = retryIndex,
                rayHits = rayHits
            }.ScheduleParallel(NUM_RAYS, minCommandsPerJob, dependsOn);
        
        public void Execute(int index)
        {
            int importantBitsIndex = index * NUM_RETRIES + retryIndex;
            rayData[importantBitsIndex] = new RaycastData
            {
                point = rayHits[index].point,
                distance = rayHits[index].distance,
                cumulativeDistance = rayHits[index].distance + (retryIndex > 0
                    ? rayData[importantBitsIndex - 1].cumulativeDistance + RAY_SEPARATION
                    : 0.0f),
                hitAnything = rayHits[index].colliderInstanceID != 0,
                hitBackface = Vector3.Dot(rayHits[index].normal, directions[index]) > 0.0f
            };
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
        NativeArray<RaycastCommand> rayCommands = new (NUM_RAYS, Allocator.TempJob);
        NativeArray<Vector3> rayDirections = new (NUM_RAYS, Allocator.TempJob);
        NativeArray<RaycastHit> rayHits = new (NUM_RAYS, Allocator.TempJob);
        NativeArray<RaycastData> rayData = new (NUM_RAYS * NUM_RETRIES, Allocator.TempJob);
        
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
            nullQuery = new QueryParameters
            {
                layerMask = 0
            },
            distance = distance
        };

        exitDirection.Value = Vector3.zero;

        JobHandle curHandle = default;

        for (int retryIndex = 0; retryIndex < NUM_RETRIES; retryIndex++)
        {
            curHandle = CreateRays.ScheduleBatch(rayCommands, rayData, rayDirections, rayParameters, retryIndex, 32, curHandle);
            curHandle = RaycastCommand.ScheduleBatch(rayCommands, rayHits, 1, 1, curHandle);
            curHandle = ProcessHits.ScheduleBatch(rayData, rayDirections, rayHits, retryIndex, 32, curHandle);
        }
        
        //JobHandle calculateAverageDirection = CalculateAverageDirection.Schedule(finalHits, exitDirection, rayParameters, curHandle);
        
#if UNITY_EDITOR
        //double time = Time.realtimeSinceStartupAsDouble;
        curHandle.Complete();
        //Debug.Log((Time.realtimeSinceStartupAsDouble - time) / 1000.0);
        for (int i = 0; i < NUM_RAYS; i++)
        {
            for (int j = 0; j < NUM_RETRIES; j++)
            {
                RaycastData data = rayData[i * NUM_RETRIES + j];
                Gizmos.color = data.hitBackface ? Color.green : Color.blue;
                Gizmos.DrawRay(data.point, -rayDirections[i] * data.distance);
            }
        }
        rayCommands.Dispose();
        rayDirections.Dispose();
        rayHits.Dispose();
        rayData.Dispose();
        return default;
#else
        rayCommands.Dispose(curHandle);
        rayDirections.Dispose(curHandle);
        rayHits.Dispose(curHandle);
        rayData.Dispose(curHandle);
        return default;
#endif
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        ScheduleJobs();
        /*
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

                Vector3 rayNormal = rayHits[rayIndex].normal;
                Vector3 colorNormal = (rayNormal + Vector3.one) * 0.5f;
                Gizmos.color = new Color(colorNormal.x, colorNormal.y, colorNormal.z, 1.0f);
                Gizmos.DrawLine(rayHits[rayIndex].point, rayHits[rayIndex].point + rayNormal);

                if (showBalls)
                {
                    Gizmos.color = Color.HSVToRGB((float)i / HITS_PER_RAY, 1.0f, 1.0f);
                    Gizmos.DrawWireSphere(rayHits[rayIndex].point, 0.2f);
                    Debug.Log(
                        $"{rayHits[rayIndex].colliderInstanceID} / {rayHits[rayIndex].distance} / {rayHits[rayIndex].point}");
                }
            }
        }

        rayHits.Dispose();
        */
    }
#endif
}

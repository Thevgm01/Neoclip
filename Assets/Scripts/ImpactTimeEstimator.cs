using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class ImpactTimeEstimator : MonoBehaviour
{
    [SerializeField] private RagdollHelper ragdollHelper;
    [SerializeField] private LayerMask sphereCastLayers;
    [SerializeField] private float sphereCastRadius = 1.0f;
    [SerializeField] private float timeToLookAhead = 5.0f;
    [SerializeField] private int steps = 10;
    
    private int panicID;
    
    private void Awake()
    {
        panicID = Animator.StringToHash("Panic");
    }
    
    public float Estimate()
    {
        Vector3 position = ragdollHelper.AveragePosition;
        Vector3 velocity = ragdollHelper.AverageLinearVelocity;
        float stepDeltaTime = timeToLookAhead / steps;
        float timeToHit = -1.0f;
        
        GizmoAnywhere.SubmitRequest(new GizmoAnywhere.Request
        {
            owner = transform, selected = GizmoAnywhere.Selected.PARENT_OF_OWNER, shape = GizmoAnywhere.Shape.WIRE_SPHERE,
            position = position, color = Color.cyan, radius = sphereCastRadius, ragdollRelative = GizmoAnywhere.RagdollRelative.YES,
        });
        
        for (int i = 0; i < steps; i++)
        {
            float velocityMagnitude = velocity.magnitude;
            Vector3 velocityNormalized = velocity / velocityMagnitude;

            using (new IgnoreBackfacesTemporary())
            {
                if (Physics.SphereCast(position, sphereCastRadius, velocityNormalized,
                        out RaycastHit hit, velocityMagnitude * stepDeltaTime, sphereCastLayers.value))
                {
                    GizmoAnywhere.RepeatRequest(new GizmoAnywhere.Request
                        { position = position + velocityNormalized * hit.distance, color = Color.red });
                    timeToHit = i * stepDeltaTime + hit.distance / velocityMagnitude;
                    break;
                }
            }
            
            position += velocity * stepDeltaTime;
            
            // Don't apply gravity while clipping: a crude shortcut
            if (!ClippingUtils.CheckOrCastRays(position, sphereCastRadius))
            {
                velocity += Physics.gravity * stepDeltaTime;
                GizmoAnywhere.RepeatRequest(new GizmoAnywhere.Request { position = position, color = Color.cyan });
            }
            else
            {
                GizmoAnywhere.RepeatRequest(new GizmoAnywhere.Request { position = position, color = Color.blue });

            }
        }
        
        return timeToHit;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, sphereCastRadius);
        }
    }
#endif
}

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class PanicEstimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private RagdollHelper ragdollHelper;
    [SerializeField] private float sphereCastRadius = 1.0f;
    [SerializeField] private int steps = 10;
    [SerializeField] private AnimationCurve panicAtImpactTime;
    [SerializeField] private AnimationCurve panicMultAtSpeed;
    [SerializeField] private AnimationCurve panicAtAngularVelocity;
    
    private int panicID;
    
    private void Awake()
    {
        panicID = Animator.StringToHash("Panic");
    }
    
    public float EstimateTimeToHit()
    {
        using (new IgnoreBackfacesTemporary())
        {
            Vector3 position = ragdollHelper.AveragePosition;
            Vector3 velocity = ragdollHelper.AverageLinearVelocity;
            float stepDeltaTime = panicAtImpactTime.keys[^1].time / steps;
            float timeToHit = -1.0f;
            
            GizmoAnywhere.SubmitRequest(new GizmoAnywhere.GizmoDrawRequest
            {
                owner = transform, criteria = GizmoAnywhere.DrawCriteria.SELECTED_ANY, shape = GizmoAnywhere.Shape.WIRE_SPHERE,
                position = position, color = Color.cyan, radius = sphereCastRadius, ragdollRelative = GizmoAnywhere.RagdollRelative.TRUE
            });
            
            for (int i = 0; i < steps; i++)
            {
                float velocityMagnitude = velocity.magnitude;
                Vector3 velocityNormalized = velocity / velocityMagnitude;
                if (Physics.SphereCast(position, sphereCastRadius, velocityNormalized,
                        out RaycastHit hit, velocityMagnitude * stepDeltaTime, ClippingUtils.ShapeCastLayerMask))
                {
                    GizmoAnywhere.RepeatRequest(new GizmoAnywhere.GizmoDrawRequest
                        { position = position + velocityNormalized * hit.distance, color = Color.red });
                    timeToHit = i * stepDeltaTime + hit.distance / velocityMagnitude;
                    break;
                }
                
                position += velocity * stepDeltaTime;
                velocity += Physics.gravity * stepDeltaTime;
                
                GizmoAnywhere.RepeatRequest(new GizmoAnywhere.GizmoDrawRequest
                    { position = position, color = Color.cyan });
            }
            
            animator.SetFloat(
                panicID,
                Mathf.Max(
                    panicAtImpactTime.Evaluate(timeToHit) * panicMultAtSpeed.Evaluate(ragdollHelper.AverageLinearVelocity.magnitude),
                    panicAtAngularVelocity.Evaluate(ragdollHelper.AverageAngularVelocity.magnitude)));
            
            return timeToHit;
        }
    }
}

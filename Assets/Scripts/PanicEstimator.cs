using System;
using UnityEditor;
using UnityEngine;

public class PanicEstimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private float sphereCastRadius = 1.0f;
    [SerializeField] private LayerMask sphereCastLayers;
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
            Vector3 position = ragdollAverages.AveragePosition;
            Vector3 velocity = ragdollAverages.AverageLinearVelocity;
            float stepDeltaTime = panicAtImpactTime.keys[^1].time / steps;
            float timeToHit = -1.0f;
            
            GizmoQueue.SubmitRequest(new GizmoQueue.GizmoDrawRequest
            {
                owner = transform, criteria = GizmoQueue.DrawCriteria.SELECTED_ANY, shape = GizmoQueue.Shape.WIRE_SPHERE,
                position = position, color = Color.cyan, radius = sphereCastRadius
            });
                        
            for (int i = 0; i < steps; i++)
            {
                float velocityMagnitude = velocity.magnitude;
                Vector3 velocityNormalized = velocity / velocityMagnitude;
                if (Physics.SphereCast(position, sphereCastRadius, velocityNormalized,
                        out RaycastHit hit, velocityMagnitude, sphereCastLayers.value))
                {
                    GizmoQueue.RepeatRequest(new GizmoQueue.GizmoDrawRequest
                        { position = position + velocityNormalized * hit.distance, color = Color.red });
                    timeToHit = i * stepDeltaTime + hit.distance / velocityMagnitude;
                    break;
                }
                
                GizmoQueue.RepeatRequest(new GizmoQueue.GizmoDrawRequest
                    { position = position + velocity, color = Color.cyan });
                
                position += velocity * stepDeltaTime;
                velocity += Physics.gravity * stepDeltaTime;
            }
            
            animator.SetFloat(
                panicID,
                Mathf.Max(
                    panicAtImpactTime.Evaluate(timeToHit) * panicMultAtSpeed.Evaluate(ragdollAverages.AverageLinearVelocity.magnitude),
                    panicAtAngularVelocity.Evaluate(ragdollAverages.AverageAngularVelocity.magnitude)));

            return timeToHit;
        }
    }
}

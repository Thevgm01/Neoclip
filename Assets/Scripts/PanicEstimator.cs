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
    [SerializeField] private float stepDeltaTime = 0.5f;
    [SerializeField] private AnimationCurve panicAtImpactTime;
    [SerializeField] private AnimationCurve panicMultAtSpeed;

    private int panicID;
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        if (Application.isPlaying)
        {
            float timeToHit = EstimateTimeToHit();
            if (Selection.activeGameObject == this.gameObject)
            {
                Debug.Log(timeToHit);
            }
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, sphereCastRadius);
        }
    }
#endif

    private void Start()
    {
        panicID = Animator.StringToHash("Panic");
    }
    
    private float EstimateTimeToHit()
    {
        Physics.queriesHitBackfaces = false;
        
        Vector3 position = ragdollAverages.AveragePosition;
        Vector3 velocity = ragdollAverages.AverageVelocity;
        float timeToHit = -1.0f;
        
        Gizmos.DrawWireSphere(position, sphereCastRadius);
        
        for (int i = 0; i < steps; i++)
        {
            float velocityMagnitude = velocity.magnitude;
            Vector3 velocityNormalized = velocity / velocityMagnitude;
            if (Physics.SphereCast(position, sphereCastRadius, velocityNormalized, 
                    out RaycastHit hit, velocityMagnitude, sphereCastLayers.value))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(position + velocityNormalized * hit.distance, sphereCastRadius);
                timeToHit = i * stepDeltaTime + hit.distance / velocityMagnitude;
                break;
            }
            Gizmos.DrawWireSphere(position + velocity, sphereCastRadius);
            
            position += velocity * stepDeltaTime;
            velocity += Physics.gravity * stepDeltaTime;
        }
        
        Physics.queriesHitBackfaces = true;
        animator.SetFloat(
            panicID, 
            panicAtImpactTime.Evaluate(timeToHit) * panicMultAtSpeed.Evaluate(ragdollAverages.AverageVelocity.magnitude));
        
        return timeToHit;
    }
}

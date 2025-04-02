using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Forces", menuName = "Scriptable Objects/Forces")]
public class ForcesSO : ScriptableObject
{
    [Serializable]
    public class Force
    {
        public bool enabled = true;
        public float mult = 1.0f;
        
        public virtual void TryAddForce(Rigidbody rigidbody, Vector3 input)
        {
            if (enabled)
            {
                rigidbody.AddForce(input * mult, ForceMode.Acceleration);
            }
        }
    }
    
    [Serializable]
    public class ExitDirectionForce : Force {
        public bool normalize = false;
        public float alignmentDelta = 0.0f;
        public float movementInfluence = 0.0f;
        
        public override void TryAddForce(Rigidbody rigidbody, Vector3 input)
        {
            if (enabled)
            {
                rigidbody.AddForce((normalize ? input.normalized : input) * mult, ForceMode.Acceleration);
                Vector3 rotated = Vector3.RotateTowards(rigidbody.linearVelocity, input, alignmentDelta, 0.0f);
                rigidbody.AddForce(rotated - rigidbody.linearVelocity, ForceMode.VelocityChange);
            }
        }
    }
    
    [SerializeField] private DensitySO density;
    [SerializeField] private float customDensity = 0.0f;
    [SerializeField] private Force gravity;
    [SerializeField] private Force movement;
    public ExitDirectionForce exitDirection;
    
    public float GetDensity() => density.value;
    
    public void ApplyAllForces(Rigidbody rigidbody, Vector3 move, Vector3 exit)
    {
        gravity.TryAddForce(rigidbody, Physics.gravity);
        movement.TryAddForce(rigidbody, move);
        exitDirection.TryAddForce(rigidbody, exit + move * exitDirection.movementInfluence);
    }
}

using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Forces", menuName = "Scriptable Objects/Forces")]
public class Forces : ScriptableObject
{
    [Serializable]
    public class Force
    {
        public bool enabled = true;
        public float mult = 1.0f;
        public ForceMode forceMode = ForceMode.Acceleration;

        public virtual void TryAddForce(Rigidbody rigidbody, Vector3 input)
        {
            if (enabled)
            {
                rigidbody.AddForce(input * mult, forceMode);
            }
        }
    }
    
    [Serializable]
    public class ExitDirectionForce : Force {
        public bool normalize = false;
        public float alignmentDelta = 0.0f;
        public ForceMode alignmentForceMode = ForceMode.VelocityChange;

        public override void TryAddForce(Rigidbody rigidbody, Vector3 input)
        {
            if (enabled)
            {
                rigidbody.AddForce((normalize ? input : input.normalized) * mult, forceMode);
                Vector3 rotated = Vector3.RotateTowards(rigidbody.linearVelocity, input, alignmentDelta, 0.0f);
                rigidbody.AddForce(rotated - rigidbody.linearVelocity, alignmentForceMode);
            }
        }
    }
    
    [SerializeField] private Constants.Density density = Constants.Density.Air;
    [SerializeField] private float customDensity = 0.0f;
    [SerializeField] private Force gravity;
    [SerializeField] private Force movement;
    public ExitDirectionForce exitDirection;
    
    public float GetDensity() => density == Constants.Density.Custom ? customDensity : (float)density / 1000.0f;
    public void ApplyGravity(Rigidbody rigidbody) => gravity.TryAddForce(rigidbody, Physics.gravity);
    public void ApplyMovement(Rigidbody rigidbody, Vector3 input) => movement.TryAddForce(rigidbody, input);
    public void ApplyExitDirection(Rigidbody rigidbody, Vector3 input) => exitDirection.TryAddForce(rigidbody, input);
}

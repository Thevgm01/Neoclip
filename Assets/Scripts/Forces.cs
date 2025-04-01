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

        public void TryAddForce(Rigidbody rigidbody, Vector3 input)
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
        public float rotationDelta = 0.0f;
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

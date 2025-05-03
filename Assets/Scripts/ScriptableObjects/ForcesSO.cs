using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Forces", menuName = "Scriptable Objects/Forces")]
public class ForcesSO : ScriptableObject
{
    [Serializable]
    public class Force
    {
        [Tooltip("Whether this force will be added to the rigidbody when this ForcesSO is active.")]
        public bool enabled = true;
        [Tooltip("Whether to normalize the input vector before adding it to the rigidbody.")]
        public bool normalized = false;
        [Tooltip("The mult to apply to the input vector, after any normalization.")]
        public float mult = 1.0f;
        [Tooltip("Rotate the rigidbody's velocity towards the input vector after the previous force has been applied.")]
        public float alignmentDelta = 0.0f;
        
        // Will have less of an effect as the vectors are more aligned
        public virtual void RotateVelocity(Rigidbody rigidbody, SmartVector3 target)
        {
            float alignmentDot = Vector3.Dot(rigidbody.linearVelocity.normalized, target.Normalized);
            Vector3 rotated = Vector3.RotateTowards(
                rigidbody.linearVelocity,
                target,
                alignmentDelta * (0.5f - alignmentDot * 0.5f),
                0.0f);
            rigidbody.AddForce(rotated - rigidbody.linearVelocity, ForceMode.VelocityChange);
        }

        public virtual void AddForce(Rigidbody rigidbody, SmartVector3 input)
        {
            rigidbody.AddForce((normalized ? input.Normalized : input.Value) * mult, ForceMode.Acceleration);
            if (alignmentDelta > 0.0f) RotateVelocity(rigidbody, input);
        }
    }
    
    [Tooltip("The density used for drag calculation.")]
    [SerializeField] private DensitySO density;
    [SerializeField] private Force gravity;
    [SerializeField] private Force movementInput;
    [SerializeField] private Force eject;
    [Tooltip("How much of the movement input vector should be added to the exit direction vector before the force is calculated.")]
    [SerializeField] private float ejectMovementInputInfluence = 0.0f;
    
    public float GetDensity() => density.value;
    
    public bool NeedsExitDirection() => eject.enabled;
    
    public void ApplyAllForces(Rigidbody rigidbody, SmartVector3 moveInput, SmartVector3 exitVector)
    {
        if (gravity.enabled) gravity.AddForce(rigidbody, Physics.gravity);
        if (movementInput.enabled) movementInput.AddForce(rigidbody, moveInput);
        if (eject.enabled) eject.AddForce(rigidbody, 
            exitVector + moveInput * ejectMovementInputInfluence);
    }
}

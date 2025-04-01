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
    }
    
    [Serializable]
    public class ExitDirectionForce : Force {
        public bool normalize = false;
        public float rotationDelta = 0.0f;
    }
    
    [SerializeField] private Constants.Density density = Constants.Density.Air;
    [SerializeField] private float customDensity = 0.0f;
    public Force gravity;
    public Force movement;
    public ExitDirectionForce exitDirection;
    
    public float GetDensity() => density == Constants.Density.Custom ? customDensity : (float)density / 1000.0f;
}

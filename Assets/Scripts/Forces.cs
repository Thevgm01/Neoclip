using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Forces", menuName = "Scriptable Objects/Forces")]
public class Forces : ScriptableObject
{
    [Serializable]
    public struct GravityForce
    {
        public bool enabled;
        public float mult;
    }
    
    [Serializable]
    public struct ExitDirectionForce {
        public bool enabled;
        public bool normalize;
        public float mult;
        public float rotationDelta;
    }
    
    public Constants.Density density;
    public GravityForce gravity;
    public ExitDirectionForce exitDirection;
    public float movementMult;
}

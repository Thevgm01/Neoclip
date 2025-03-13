using System;
using System.Collections.Generic;
using UnityEngine;

public class RagdollAverages : MonoBehaviour
{
    private int lastFrame = 0;
    private int lastPhysicsFrame = 0;

    public float TotalMass { get; private set; }
    private Vector3 averagePosition = Vector3.zero;
    private Vector3 averageVelocity = Vector3.zero;

    private Rigidbody[] rigidbodies;
    private Transform[] transforms;
    
    public Vector3 AveragePosition {
        get
        {
            if (lastFrame < Time.frameCount)
            {
                averagePosition = Vector3.zero;
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    // rigidbody.position is NOT interpolated, it needs to be rigidbody.TRANSFORM.position
                    averagePosition += transforms[i].position * rigidbodies[i].mass;
                }

                averagePosition /= TotalMass;
                lastFrame = Time.frameCount;
            }
            
            return averagePosition;
        }
    }
    
    public Vector3 AverageVelocity {
        get
        {
            if (lastPhysicsFrame < Utils.FixedUpdateCount)
            {
                averageVelocity = Vector3.zero;
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    averageVelocity += rigidbodies[i].linearVelocity;
                }
                averageVelocity /= rigidbodies.Length;
                lastPhysicsFrame = Utils.FixedUpdateCount;
            }
            
            return averagePosition;
        }
    }
    
    private void Awake()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        transforms = new Transform[rigidbodies.Length];
        
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            transforms[i] = rigidbodies[i].transform;
            TotalMass += rigidbodies[i].mass;
        }
    }
}

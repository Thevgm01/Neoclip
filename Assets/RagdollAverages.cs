using System;
using System.Collections.Generic;
using UnityEngine;

public class RagdollAverages : MonoBehaviour
{
    private int lastFrame = 0;
    private int lastPhysicsFrame = 0;

    private Vector3 averagePosition = Vector3.zero;
    private Vector3 averageVelocity = Vector3.zero;
    
    private Rigidbody[] rigidbodies;
    private float totalMass = 0;
    
    public Vector3 AveragePosition {
        get
        {
            if (lastFrame < Time.frameCount)
            {
                averagePosition = Vector3.zero;
                foreach (Rigidbody rb in rigidbodies)
                {
                    averagePosition += rb.position * rb.mass;
                }
                averagePosition /= totalMass;
                lastFrame = Time.frameCount;
            }
            Debug.Log(averagePosition);
            return averagePosition;
        }
    }
    
    public Vector3 AverageVelocity {
        get
        {
            if (lastPhysicsFrame < Utils.FixedUpdateCount)
            {
                averageVelocity = Vector3.zero;
                foreach (Rigidbody rb in rigidbodies)
                {
                    averageVelocity += rb.linearVelocity;
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

        foreach (Rigidbody rb in rigidbodies)
        {
            totalMass += rb.mass;
        }
    }
}

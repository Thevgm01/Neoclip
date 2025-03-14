using System;
using System.Collections.Generic;
using UnityEngine;

public class RagdollAverages : MonoBehaviour
{
    private Rigidbody[] rigidbodies;
    private Transform[] transforms;

    public Rigidbody[] Rigidbodies => (Rigidbody[])rigidbodies.Clone();
    public Transform[] Transforms => (Transform[])transforms.Clone();
    
    public float TotalMass { get; private set; }
    
    private Vector3 averagePositionInterpolated = Vector3.zero;
    private Vector3 averagePosition = Vector3.zero;
    private Vector3 averageVelocity = Vector3.zero;

    private int lastInterpolatedFrame = 0;
    private int lastPositionFrame = 0;
    private int lastVelocityFrame = 0;
    
    public Vector3 AveragePositionInterpolated {
        get
        {
            if (lastInterpolatedFrame < Time.frameCount)
            {
                averagePositionInterpolated = Vector3.zero;
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    // rigidbody.position is NOT interpolated, we need to use transform.position
                    averagePositionInterpolated += transforms[i].position * rigidbodies[i].mass;
                }

                averagePositionInterpolated /= TotalMass;
                lastInterpolatedFrame = Time.frameCount;
            }
            
            return averagePositionInterpolated;
        }
    }
    
    public Vector3 AveragePosition {
        get
        {
            if (lastPositionFrame < Utils.FixedUpdateCount)
            {
                averagePosition = Vector3.zero;
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    averagePosition += rigidbodies[i].position * rigidbodies[i].mass;
                }

                averagePosition /= TotalMass;
                lastPositionFrame = Utils.FixedUpdateCount;
            }
            
            return averagePosition;
        }
    }
    
    public Vector3 AverageVelocity {
        get
        {
            if (lastVelocityFrame < Utils.FixedUpdateCount)
            {
                averageVelocity = Vector3.zero;
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    averageVelocity += rigidbodies[i].linearVelocity;
                }
                averageVelocity /= rigidbodies.Length;
                lastVelocityFrame = Utils.FixedUpdateCount;
            }
            
            return averageVelocity;
        }
    }

    private void Init()
    {
        if (rigidbodies == null || rigidbodies.Length == 0)
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

    private void Awake()
    {
        Init();
    }

    public void AddForceToAll(Vector3 force, ForceMode forceMode = ForceMode.Force)
    {
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].AddForce(force, forceMode);
        }
    }
}

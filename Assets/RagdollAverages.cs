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

    private class AveragePositionProperty : FrameCountUpdatedProperty<Vector3>
    {
        private readonly RagdollAverages parent;
        
        protected override Vector3 PropertyFunction()
        {
            Vector3 averagePosition = Vector3.zero;
            
            for (int i = 0; i < parent.transforms.Length; i++)
            {
                averagePosition += parent.transforms[i].position * parent.rigidbodies[i].mass;
            }

            return averagePosition / parent.TotalMass;
        }

        public AveragePositionProperty(RagdollAverages parent)
        {
            this.parent = parent;
        }
    }
    
    private class AverageVelocityProperty : FixedFrameCountUpdatedProperty<Vector3>
    {
        private readonly RagdollAverages parent;
        
        protected override Vector3 PropertyFunction()
        {
            Vector3 averageVelocity = Vector3.zero;
            
            for (int i = 0; i < parent.rigidbodies.Length; i++)
            {
                averageVelocity += parent.rigidbodies[i].linearVelocity;
            }
            
            return averageVelocity / parent.rigidbodies.Length;
        }

        public AverageVelocityProperty(RagdollAverages parent)
        {
            this.parent = parent;
        }
    }
    
    private AveragePositionProperty averagePosition;
    private AverageVelocityProperty averageVelocity;
    
    public Vector3 AveragePosition => averagePosition.Get();
    public Vector3 AverageVelocity => averageVelocity.Get();

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

        averagePosition = new AveragePositionProperty(this);
        averageVelocity = new AverageVelocityProperty(this);
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

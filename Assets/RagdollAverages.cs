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
    
    private FrameCountUpdatedProperty<Vector3> averagePosition;
    public Vector3 AveragePosition => averagePosition.GetValue();

    private FixedFrameCountUpdatedProperty<Vector3> averageVelocity;
    public Vector3 AverageVelocity => averageVelocity.GetValue();

    private void Init()
    {
        if (rigidbodies != null && rigidbodies.Length > 0)
        {
            return;
        }
        
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        transforms = new Transform[rigidbodies.Length];
    
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            transforms[i] = rigidbodies[i].transform;
            TotalMass += rigidbodies[i].mass;
        }
        
        averagePosition = new FrameCountUpdatedProperty<Vector3>(() =>
        {
            Vector3 temp = Vector3.zero;
            for (int i = 0; i < transforms.Length; i++)
            {
                temp += transforms[i].position * rigidbodies[i].mass;
            }
            return temp / TotalMass;
        });
        
        averageVelocity = new FixedFrameCountUpdatedProperty<Vector3>(() =>
        {
            Vector3 temp = Vector3.zero;
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                temp += rigidbodies[i].linearVelocity;
            }
            return temp / rigidbodies.Length;
        });
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

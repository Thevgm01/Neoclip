using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class RagdollAverages : NeoclipCharacterComponent
{
    public float TotalMass { get; private set; }
    public int NumBones { get; private set; }

    private Rigidbody[] rigidbodies;
    private Transform[] transforms;
    private Collider[] colliders;

    public Rigidbody[] Rigidbodies => (Rigidbody[])rigidbodies.Clone();
    public Transform[] Transforms => (Transform[])transforms.Clone();
    public Collider[] Colliders => (Collider[])colliders.Clone();
    
    public Rigidbody GetRigidbody(int index) => rigidbodies[index];
    public Transform GetTransform(int index) => transforms[index];
    public Collider GetCollider(int index) => colliders[index];

    private Vector3 CalculateAveragePosition()
    {
        Vector3 temp = Vector3.zero;
        for (int i = 0; i < NumBones; i++)
        {
            temp += GetTransform(i).position * GetRigidbody(i).mass;
        }
        return temp / TotalMass;
    }
    private TimeUpdatedProperty<Vector3> averagePositionProperty;
    public Vector3 AveragePosition => averagePositionProperty.GetValue();

    private Vector3 CalculateAverageLinearVelocity()
    {
        Vector3 temp = Vector3.zero;
        for (int i = 0; i < NumBones; i++)
        {
            temp += GetRigidbody(i).linearVelocity;
        }
        return temp / NumBones;
    }
    private FixedTimeUpdatedProperty<Vector3> averageLinearVelocityProperty;
    public Vector3 AverageLinearVelocity => averageLinearVelocityProperty.GetValue();
    
    private Vector3 CalculateAverageAngularVelocity()
    {
        Vector3 temp = Vector3.zero;
        for (int i = 0; i < NumBones; i++)
        {
            temp += GetRigidbody(i).angularVelocity;
        }
        return temp / NumBones;
    }
    private FixedTimeUpdatedProperty<Vector3> averageAngularVelocityProperty;
    public Vector3 AverageAngularVelocity => averageAngularVelocityProperty.GetValue();
    
    private void FillArrays()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        NumBones = rigidbodies.Length;
        transforms = new Transform[NumBones];
        colliders = new Collider[NumBones];
        
        for (int i = 0; i < NumBones; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];
            TotalMass += rigidbody.mass;
            
            transforms[i] = rigidbody.transform;
            colliders[i] = rigidbody.GetComponent<Collider>();;
        }
    }
    
    public override void Init()
    {
        DestroyImmediate(GetComponent<ConfigurableJoint>());
        DestroyImmediate(GetComponent<Rigidbody>());
        FillArrays();
        averagePositionProperty = new TimeUpdatedProperty<Vector3>(CalculateAveragePosition);
        averageLinearVelocityProperty = new FixedTimeUpdatedProperty<Vector3>(CalculateAverageLinearVelocity);
        averageAngularVelocityProperty = new FixedTimeUpdatedProperty<Vector3>(CalculateAverageAngularVelocity);
    }
}

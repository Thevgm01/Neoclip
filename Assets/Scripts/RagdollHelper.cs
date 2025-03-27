using System;
using UnityEngine;

public class RagdollHelper : MonoBehaviour
{
    public float TotalMass { get; private set; }
    public int NumBones { get; private set; }
    public int NumJoints { get; private set; }

    private Rigidbody[] rigidbodies;
    private Transform[] transforms;
    private Collider[] colliders;
    private ConfigurableJoint[] joints;

    public Rigidbody[] Rigidbodies => (Rigidbody[])rigidbodies.Clone();
    public Transform[] Transforms => (Transform[])transforms.Clone();
    public Collider[] Colliders => (Collider[])colliders.Clone();
    public ConfigurableJoint[] Joints => (ConfigurableJoint[])joints.Clone();
    
    public Rigidbody GetRigidbody(int index) => rigidbodies[index];
    public Transform GetTransform(int index) => transforms[index];
    public Collider GetCollider(int index) => colliders[index];
    public ConfigurableJoint GetJoint(int index) => joints[index];
    
    public Vector3 AveragePosition { get; private set; }
    public Vector3 AverageLinearVelocity { get; private set; }
    public Vector3 AverageAngularVelocity { get; private set; }
    
    private void Awake()
    {
        DestroyImmediate(GetComponent<ConfigurableJoint>());
        DestroyImmediate(GetComponent<Rigidbody>()); // Must come second because Rigidbody depends on ConfigurableJoint
        
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        NumBones = rigidbodies.Length;
        transforms = new Transform[NumBones];
        colliders = new Collider[NumBones];
        
        joints = GetComponentsInChildren<ConfigurableJoint>();
        NumJoints = joints.Length;
        
        for (int i = 0; i < NumBones; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];
            TotalMass += rigidbody.mass;
            
            transforms[i] = rigidbody.transform;
            colliders[i] = rigidbody.GetComponent<Collider>();
        }
    }

    private void Update()
    {
        AveragePosition = Vector3.zero;
        for (int i = 0; i < NumBones; i++)
        {
            AveragePosition += transforms[i].position * rigidbodies[i].mass;
        }
        AveragePosition /= TotalMass;
    }
    
    private void FixedUpdate()
    {
        AveragePosition = Vector3.zero;
        AverageLinearVelocity = Vector3.zero;
        AverageAngularVelocity = Vector3.zero;
        for (int i = 0; i < NumBones; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];
            AveragePosition += rigidbody.position * rigidbody.mass;
            AverageLinearVelocity += rigidbody.linearVelocity;
            AverageAngularVelocity += rigidbody.angularVelocity;
        }
        AveragePosition /= TotalMass;
        AverageLinearVelocity /= NumBones;
        AverageAngularVelocity /= NumBones;
    }
}

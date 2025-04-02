using System;
using UnityEngine;
using UnityEngine.Serialization;

public class RagdollHelper : MonoBehaviour
{
    public float TotalMass { get; private set; }
    public int NumBones { get; private set; }
    public int NumJoints { get; private set; }

    private Rigidbody[] rigidbodies;
    private GameObject[] gameObjects;
    private Transform[] transforms;
    private Collider[] colliders;
    private ConfigurableJoint[] joints;
    private Vector3[] linearVelocities;
    private Vector3[] angularVelocities;
    
    public GameObject[] BameObjects => (GameObject[])gameObjects.Clone();
    public Rigidbody[] Rigidbodies => (Rigidbody[])rigidbodies.Clone();
    public Transform[] Transforms => (Transform[])transforms.Clone();
    public Collider[] Colliders => (Collider[])colliders.Clone();
    public ConfigurableJoint[] Joints => (ConfigurableJoint[])joints.Clone();
    
    public GameObject GetGameObject(int index) => gameObjects[index];
    public Rigidbody GetRigidbody(int index) => rigidbodies[index];
    public Transform GetTransform(int index) => transforms[index];
    public Collider GetCollider(int index) => colliders[index];
    public ConfigurableJoint GetJoint(int index) => joints[index];
    
    public SmartVector3 AveragePosition { get; private set; }
    public SmartVector3 AverageLinearVelocity { get; private set; }
    public SmartVector3 AverageAngularVelocity { get; private set; }

    [Serializable]
    private struct PhysicsIgnoreCollisionPair
    {
        public Collider main;
        public Collider[] ignores;
    }
    
#if  UNITY_EDITOR
    [SerializeField] private JointRotationValues[] jointRotationValues;
#endif
    
    [SerializeField] [Tooltip("Adjacent bones are already ignored, use this to add extras.")]
    private PhysicsIgnoreCollisionPair[] additionalPhysicsIgnorePairs;
    
    private void Awake()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        NumBones = rigidbodies.Length;
        gameObjects = new GameObject[NumBones];
        transforms = new Transform[NumBones];
        colliders = new Collider[NumBones];
        linearVelocities = new Vector3[NumBones];
        angularVelocities = new Vector3[NumBones];
        
        joints = GetComponentsInChildren<ConfigurableJoint>();
        NumJoints = joints.Length;
        
        for (int i = 0; i < NumBones; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];
            TotalMass += rigidbody.mass;
            
            gameObjects[i] = rigidbody.gameObject;
            transforms[i] = rigidbody.transform;
            colliders[i] = rigidbody.GetComponent<Collider>();
        }
        
        for (int i = 0; i < additionalPhysicsIgnorePairs.Length; i++)
        {
            foreach (Collider collider in additionalPhysicsIgnorePairs[i].ignores)
            {
                Physics.IgnoreCollision(additionalPhysicsIgnorePairs[i].main, collider);
            }
        }
        
        Debug.Log($"{nameof(RagdollHelper)}.{nameof(Awake)} mass is {TotalMass} kg.");
    }
    
    private void Update()
    {
        Vector3 averagePosition = Vector3.zero;
        for (int i = 0; i < NumBones; i++)
        {
            averagePosition += transforms[i].position * rigidbodies[i].mass;
        }
        AveragePosition = averagePosition / TotalMass;
    }
    
    private void FixedUpdate()
    {
        Vector3 averagePosition = Vector3.zero;
        Vector3 averageLinearVelocity = Vector3.zero;
        Vector3 averageAngularVelocity = Vector3.zero;
        for (int i = 0; i < NumBones; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];
            averagePosition += rigidbody.position * rigidbody.mass;
            averageLinearVelocity += rigidbody.linearVelocity;
            averageAngularVelocity += rigidbody.angularVelocity;
        }
        AveragePosition = averagePosition / TotalMass;
        AverageLinearVelocity = averageLinearVelocity / NumBones;
        AverageAngularVelocity = averageAngularVelocity / NumBones;
        
#if UNITY_EDITOR
        foreach (JointRotationValues values in jointRotationValues)
        {
            if (values.enabled)
            {
                ConfigurableJoint[] jointsToOverride =
                    values.jointsToOverride.Length > 0 ? values.jointsToOverride : this.joints;
                foreach (ConfigurableJoint joint in jointsToOverride)
                {
                    values.Override(joint);
                }
            }
        }
#endif
    }

    public void SetLayers(int layer)
    {
        for (int i = 0; i < NumBones; i++)
        {
            gameObjects[i].layer = layer;
        }
    }

    public void Freeze()
    {
        for (int i = 0; i < NumBones; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];
            linearVelocities[i] = rigidbody.linearVelocity;
            angularVelocities[i] = rigidbody.angularVelocity;
            rigidbody.isKinematic = true;
        }
    }

    public void Unfreeze()
    {
        for (int i = 0; i < NumBones; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];
            rigidbody.isKinematic = false;
            rigidbody.linearVelocity = linearVelocities[i];
            rigidbody.angularVelocity = angularVelocities[i];
        }
    }
}

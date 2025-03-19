using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class RagdollAverages : NeoclipCharacterComponent
{
    public float TotalMass { get; private set; }
    public int NumBones { get; private set; }
    
    private const int STRIDE = 4;
    private Object[] objects;
    
    private ColliderUtils.ColliderType[] colliderTypes;
    private Vector3[] colliderCenters;
    
    private List<int> boxColliderIndexes;
    private List<int> capsuleColliderIndexes;
    private List<int> sphereColliderIndexes;

    public int NumBoxColliders => boxColliderIndexes.Count;
    public int NumCapsuleColliders => capsuleColliderIndexes.Count;
    public int NumSphereColliders => sphereColliderIndexes.Count;
    
    public Rigidbody GetRigidbody(int boneIndex) => (Rigidbody)objects[boneIndex * STRIDE + 0];
    public GameObject GetGameObject(int boneIndex) => (GameObject)objects[boneIndex * STRIDE + 1];
    public Transform GetTransform(int boneIndex) => (Transform)objects[boneIndex * STRIDE + 2];
    public Collider GetCollider(int boneIndex) => (Collider)objects[boneIndex * STRIDE + 3];
    
    public ColliderUtils.ColliderType GetColliderType(int boneIndex) => colliderTypes[boneIndex];
    public Vector3 GetColliderCenter(int boneIndex) => colliderCenters[boneIndex];
    public Vector3 TransformColliderCenter(int index) => GetTransform(index).TransformPoint(GetColliderCenter(index));
    
    public BoxCollider GetBoxCollider(int boxIndex) => (BoxCollider)objects[boxColliderIndexes[boxIndex] * STRIDE + 3];
    public CapsuleCollider GetCapsuleCollider(int capsuleIndex) => (CapsuleCollider)objects[capsuleColliderIndexes[capsuleIndex] * STRIDE + 3];
    public SphereCollider GetSphereCollider(int sphereIndex) => (SphereCollider)objects[sphereColliderIndexes[sphereIndex] * STRIDE + 3];
    
    public int BoneIndexOfBoxCollider(int boxIndex) => boxColliderIndexes[boxIndex];
    public int BoneIndexOfCapsuleCollider(int capIndex) => capsuleColliderIndexes[capIndex];
    public int BoneIndexOfSphereCollider(int sphereIndex) => sphereColliderIndexes[sphereIndex];
    
    private Vector3 CalculateAveragePosition()
    {
        Vector3 temp = Vector3.zero;
        for (int i = 0; i < NumBones; i++)
        {
            temp += GetTransform(i).position * GetRigidbody(i).mass;
        }
        return temp / TotalMass;
    }
    private TimeUpdatedProperty<Vector3> averagePosition;
    public Vector3 AveragePosition => averagePosition.GetValue();

    private Vector3 CalculateAverageVelocity()
    {
        Vector3 temp = Vector3.zero;
        for (int i = 0; i < NumBones; i++)
        {
            temp += GetRigidbody(i).linearVelocity;
        }
        return temp / NumBones;
    }
    private FixedTimeUpdatedProperty<Vector3> averageVelocity;
    public Vector3 AverageVelocity => averageVelocity.GetValue();

    private void FillObjectArray()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        NumBones = rigidbodies.Length;
        objects = new Object[NumBones * STRIDE];
        colliderTypes = new ColliderUtils.ColliderType[NumBones];
        colliderCenters = new Vector3[NumBones];
    
        for (int i = 0; i < NumBones; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];
            TotalMass += rigidbody.mass;

            Collider collider = rigidbody.GetComponent<Collider>();

            objects[i * STRIDE + 0] = rigidbody;
            objects[i * STRIDE + 1] = rigidbody.gameObject;
            objects[i * STRIDE + 2] = rigidbody.transform;
            objects[i * STRIDE + 3] = collider;

            switch (collider)
            {
                case BoxCollider boxCollider:
                    colliderTypes[i] = ColliderUtils.ColliderType.Box;
                    colliderCenters[i] = boxCollider.center;
                    boxColliderIndexes.Add(i);
                    break;
                case CapsuleCollider capsuleCollider:
                    colliderTypes[i] = ColliderUtils.ColliderType.Capsule;
                    colliderCenters[i] = capsuleCollider.center;
                    capsuleColliderIndexes.Add(i);
                    break;
                case SphereCollider sphereCollider:
                    colliderTypes[i] = ColliderUtils.ColliderType.Sphere;
                    colliderCenters[i] = sphereCollider.center;
                    sphereColliderIndexes.Add(i);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    public override void Init()
    {
        FillObjectArray();
        averagePosition = new TimeUpdatedProperty<Vector3>(CalculateAveragePosition);
        averageVelocity = new FixedTimeUpdatedProperty<Vector3>(CalculateAverageVelocity);
    }
}

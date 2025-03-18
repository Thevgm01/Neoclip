using UnityEngine;

public class RagdollAverages : NeoclipCharacterComponent
{
    public float TotalMass { get; private set; }
    public int NumBones { get; private set; }

    private const int STRIDE = 6;
    private Object[] objects;
    
    public Rigidbody GetRigidbody(int index) => (Rigidbody)objects[index * STRIDE + 0];
    public GameObject GetGameObject(int index) => (GameObject)objects[index * STRIDE + 1];
    public Transform GetTransform(int index) => (Transform)objects[index * STRIDE + 2];
    public Collider GetCollider(int index) => (Collider)objects[index * STRIDE + 3];
    public Collider GetTrigger(int index) => (Collider)objects[index * STRIDE + 4];
    public NoclipDetector GetNoclipDetector(int index) => (NoclipDetector)objects[index * STRIDE + 5];

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
    
        for (int i = 0; i < NumBones; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];
            Collider[] colliders = rigidbody.GetComponents<Collider>();

            objects[i * STRIDE + 0] = rigidbody;
            objects[i * STRIDE + 1] = rigidbody.gameObject;
            objects[i * STRIDE + 2] = rigidbody.transform;
            objects[i * STRIDE + 3] = colliders[0];
            objects[i * STRIDE + 4] = colliders[1];
            objects[i * STRIDE + 5] = rigidbody.GetComponent<NoclipDetector>();
            
            TotalMass += rigidbody.mass;
        }
    }
    
    public override void Init()
    {
        FillObjectArray();
        averagePosition = new TimeUpdatedProperty<Vector3>(CalculateAveragePosition);
        averageVelocity = new FixedTimeUpdatedProperty<Vector3>(CalculateAverageVelocity);
    }
}

using UnityEngine;

public class RagdollAverages : NeoclipCharacterComponent
{
    public float TotalMass { get; private set; }
    public int NumBones { get; private set; }

    private const int STRIDE = 5;
    private Object[] objects;
    
    public Rigidbody GetRigidbody(int index) => (Rigidbody)objects[index * STRIDE + 0];
    public GameObject GetGameObject(int index) => (GameObject)objects[index * STRIDE + 1];
    public Transform GetTransform(int index) => (Transform)objects[index * STRIDE + 2];
    public Collider GetCollider(int index) => (Collider)objects[index * STRIDE + 3];
    public Collider GetTrigger(int index) => (Collider)objects[index * STRIDE + 4];

    private TimeUpdatedProperty<Vector3> averagePosition;
    public Vector3 AveragePosition => averagePosition.GetValue();

    private FixedTimeUpdatedProperty<Vector3> averageVelocity;
    public Vector3 AverageVelocity => averageVelocity.GetValue();

    public override void Init()
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
            
            TotalMass += rigidbody.mass;
        }
        
        averagePosition = new TimeUpdatedProperty<Vector3>(() =>
        {
            Vector3 temp = Vector3.zero;
            for (int i = 0; i < NumBones; i++)
            {
                temp += GetTransform(i).position * rigidbodies[i].mass;
            }
            return temp / TotalMass;
        });
        
        averageVelocity = new FixedTimeUpdatedProperty<Vector3>(() =>
        {
            Vector3 temp = Vector3.zero;
            for (int i = 0; i < NumBones; i++)
            {
                temp += GetRigidbody(i).linearVelocity;
            }
            return temp / NumBones;
        });
    }
}

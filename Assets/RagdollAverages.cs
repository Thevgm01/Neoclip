using UnityEngine;

public class RagdollAverages : NeoclipCharacterComponent
{
    private Rigidbody[] rigidbodies;
    private Transform[] transforms;

    public Rigidbody[] Rigidbodies => (Rigidbody[])rigidbodies.Clone();
    public Transform[] Transforms => (Transform[])transforms.Clone();
    
    public float TotalMass { get; private set; }
    public int NumRigidbodies { get; private set; }
    
    private FrameCountUpdatedProperty<Vector3> averagePosition;
    public Vector3 AveragePosition => averagePosition.GetValue();

    private FixedFrameCountUpdatedProperty<Vector3> averageVelocity;
    public Vector3 AverageVelocity => averageVelocity.GetValue();

    public override void Init()
    {
        if (rigidbodies != null && rigidbodies.Length > 0)
        {
            return;
        }
        
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        transforms = new Transform[rigidbodies.Length];
        NumRigidbodies = rigidbodies.Length;
    
        for (int i = 0; i < NumRigidbodies; i++)
        {
            transforms[i] = rigidbodies[i].transform;
            TotalMass += rigidbodies[i].mass;
        }
        
        averagePosition = new FrameCountUpdatedProperty<Vector3>(() =>
        {
            Vector3 temp = Vector3.zero;
            for (int i = 0; i < NumRigidbodies; i++)
            {
                temp += transforms[i].position * rigidbodies[i].mass;
            }
            return temp / TotalMass;
        });
        
        averageVelocity = new FixedFrameCountUpdatedProperty<Vector3>(() =>
        {
            Vector3 temp = Vector3.zero;
            for (int i = 0; i < NumRigidbodies; i++)
            {
                temp += rigidbodies[i].linearVelocity;
            }
            return temp / NumRigidbodies;
        });
    }
    
    public void AddForceToAll(Vector3 force, ForceMode forceMode = ForceMode.Force)
    {
        for (int i = 0; i < NumRigidbodies; i++)
        {
            rigidbodies[i].AddForce(force, forceMode);
        }
    }
}

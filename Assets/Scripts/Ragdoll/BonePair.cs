using UnityEngine;

public abstract class BonePair
{
    protected readonly Transform sourceBone;
    protected readonly Transform targetBone;
    
    public Transform SourceBone => sourceBone;
    public Transform TargetBone => targetBone;
        
    public BonePair(Transform sourceBone, Transform targetBone)
    {
        this.sourceBone = sourceBone;
        this.targetBone = targetBone;
    }
        
    public BonePair(BonePair other) : this(other.sourceBone, other.targetBone) {}
        
    public override string ToString()
    {
        return sourceBone.name;
    }

    public override bool Equals(object obj)
    {
        BonePair other = obj as BonePair;
        return other != null && other.sourceBone == sourceBone && other.targetBone == targetBone;
    }

    public override int GetHashCode()
    {
        // https://stackoverflow.com/a/27952689
        return sourceBone.GetHashCode() * 3 + targetBone.GetHashCode();
    }
        
    public abstract void WriteValues();
}

public class BonePairRotationOnly : BonePair
{
    public BonePairRotationOnly(Transform sourceBone, Transform targetBone) : base(sourceBone, targetBone) { }
    public override void WriteValues() => targetBone.localRotation = sourceBone.localRotation;
}

public class BonePairPositionAndRotation : BonePair
{
    public BonePairPositionAndRotation(Transform sourceBone, Transform targetBone) : base(sourceBone, targetBone) { }
    
    public override void WriteValues()
    {
        sourceBone.GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);
        targetBone.SetLocalPositionAndRotation(position, rotation);
    }
}

public class BonePairJoint : BonePair
{
    private readonly ConfigurableJoint joint;

    private readonly Quaternion worldToStartSpace;
    private readonly Quaternion jointToWorldSpace;
        
    public BonePairJoint(Transform sourceBone, ConfigurableJoint joint) : base(sourceBone, joint.transform)
    {
        this.joint = joint;

        // https://gist.github.com/mstevenson/7b85893e8caf5ca034e6
        var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
        var up = Vector3.Cross(forward, joint.axis).normalized;
        Quaternion worldToJointSpace = Quaternion.LookRotation(forward, up);
        jointToWorldSpace = Quaternion.Inverse(worldToJointSpace);
        worldToStartSpace = targetBone.localRotation * worldToJointSpace;
    }
    
    public override void WriteValues() =>
        joint.targetRotation = jointToWorldSpace * Quaternion.Inverse(sourceBone.localRotation) * worldToStartSpace;
}
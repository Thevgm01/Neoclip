using UnityEngine;

public class BonePair
{
    public Transform AnimatedBone { get; }
    public Transform DrivenBone { get; }
        
    public BonePair(Transform animatedBone, Transform drivenBone)
    {
        this.AnimatedBone = animatedBone;
        this.DrivenBone = drivenBone;
    }
        
    public BonePair(BonePair other) : this(other.AnimatedBone, other.DrivenBone) {}
        
    public override string ToString()
    {
        return AnimatedBone.name;
    }

    public override bool Equals(object obj)
    {
        BonePair other = obj as BonePair;
        return other != null && other.AnimatedBone == AnimatedBone && other.DrivenBone == DrivenBone;
    }

    public override int GetHashCode()
    {
        return AnimatedBone.GetHashCode() ^ DrivenBone.GetHashCode();
    }
        
    public virtual void SetRotation() => DrivenBone.localRotation = AnimatedBone.localRotation;
}

public class JointBonePair : BonePair
{
    private readonly ConfigurableJoint joint;

    private readonly Quaternion worldToStartSpace;
    private readonly Quaternion jointToWorldSpace;
        
    public JointBonePair(BonePair original, ConfigurableJoint joint) : base(original)
    {
        this.joint = joint;

        // https://gist.github.com/mstevenson/7b85893e8caf5ca034e6
        var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
        var up = Vector3.Cross(forward, joint.axis).normalized;
        Quaternion worldToJointSpace = Quaternion.LookRotation(forward, up);
        jointToWorldSpace = Quaternion.Inverse(worldToJointSpace);
        worldToStartSpace = DrivenBone.localRotation * worldToJointSpace;
    }

    public override void SetRotation() =>
        joint.targetRotation = jointToWorldSpace * Quaternion.Inverse(AnimatedBone.localRotation) * worldToStartSpace;
}
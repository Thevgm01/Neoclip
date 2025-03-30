using UnityEngine;

public class BonePair
{
    public Transform AnimatedBone { get; }
    public Transform RagdollBone { get; }
        
    public BonePair(Transform animatedBone, Transform ragdollBone)
    {
        this.AnimatedBone = animatedBone;
        this.RagdollBone = ragdollBone;
    }
        
    public BonePair(BonePair other) : this(other.AnimatedBone, other.RagdollBone) {}
        
    public override string ToString()
    {
        return AnimatedBone.name;
    }

    public override bool Equals(object obj)
    {
        BonePair other = obj as BonePair;
        return other != null && other.AnimatedBone == AnimatedBone && other.RagdollBone == RagdollBone;
    }

    public override int GetHashCode()
    {
        return AnimatedBone.GetHashCode() ^ RagdollBone.GetHashCode();
    }
        
    public virtual void SetRotation() => RagdollBone.localRotation = AnimatedBone.localRotation;
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
        worldToStartSpace = RagdollBone.localRotation * worldToJointSpace;
    }

    public override void SetRotation() =>
        joint.targetRotation = jointToWorldSpace * Quaternion.Inverse(AnimatedBone.localRotation) * worldToStartSpace;
}
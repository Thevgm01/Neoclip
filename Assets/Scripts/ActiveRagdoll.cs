using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ActiveRagdoll : MonoBehaviour
{
    [SerializeField] private Transform driverSkeleton;
    [SerializeField] private Transform ragdollSkeleton;
    
    private class BonePair
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
        
        public virtual void SetRotation() {}
    }

    private class TransformBonePair : BonePair
    {
        public TransformBonePair(BonePair original) : base(original) {}
        
        public override void SetRotation() => DrivenBone.localRotation = AnimatedBone.localRotation;
    }

    private class JointBonePair : BonePair
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
    
    private HashSet<BonePair> bonePairs;
    
    private TreeNode<BonePair> BuildBonePairTreeRecursive(Transform driverTransform, Transform ragdollTransform)
    {
        TreeNode<BonePair> node = new TreeNode<BonePair>(
            new BonePair(driverTransform, ragdollTransform));

        if (ragdollTransform.TryGetComponent(out ConfigurableJoint joint))
        {
            bonePairs.Add(new JointBonePair(node.value, joint));
        }
        
        int childCount = driverTransform.childCount;
        if (childCount > 0)
        {
            node.children = new TreeNode<BonePair>[childCount];

            for (int i = 0; i < childCount; i++)
            {
                node.children[i] = BuildBonePairTreeRecursive(driverTransform.GetChild(i), ragdollTransform.GetChild(i));
                node.children[i].parent = node;
            }
        }

        return node;
    }
    
    private void Awake()
    {
        bonePairs = new HashSet<BonePair>();
        TreeNode<BonePair> activeRagdollTree = BuildBonePairTreeRecursive(driverSkeleton, ragdollSkeleton);

        foreach (TreeNode<BonePair> leafNode in activeRagdollTree.Leaves())
        {
            TreeNode<BonePair> node = leafNode;
            while (node != null && node.value.DrivenBone.GetComponent<Rigidbody>() == null)
            {
                bonePairs.Add(new TransformBonePair(node.value));
                node = node.parent;
            }
        }
    }

    private void LateUpdate()
    {
        foreach (BonePair bonePair in bonePairs)
        {
            bonePair.SetRotation();
        }
    }
}

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

        public override bool Equals(object obj)
        {
            BonePair other = obj as BonePair;
            return other != null && other.AnimatedBone == AnimatedBone && other.DrivenBone == DrivenBone;
        }

        public override int GetHashCode()
        {
            return AnimatedBone.GetHashCode() ^ DrivenBone.GetHashCode();
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
    
    private HashSet<TransformBonePair> transformBonePairs;
    private List<JointBonePair> jointBonePairs;
    
    private TreeNode<BonePair> BuildBonePairTreeRecursive(Transform driverTransform, Transform ragdollTransform)
    {
        TreeNode<BonePair> node = new TreeNode<BonePair>(
            new BonePair(driverTransform, ragdollTransform));

        if (ragdollTransform.TryGetComponent(out ConfigurableJoint joint))
        {
            jointBonePairs.Add(new JointBonePair(node.value, joint));
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
        transformBonePairs = new HashSet<TransformBonePair>();
        jointBonePairs = new List<JointBonePair>();
        
        TreeNode<BonePair> bonePairTree = BuildBonePairTreeRecursive(driverSkeleton, ragdollSkeleton);

        foreach (TreeNode<BonePair> leafNode in bonePairTree.Leaves())
        {
            TreeNode<BonePair> node = leafNode;
            while (node != null && node.value.DrivenBone.GetComponent<Rigidbody>() == null)
            {
                transformBonePairs.Add(new TransformBonePair(node.value));
                node = node.parent;
            }
        }
    }

    private void LateUpdate()
    {
        foreach (TransformBonePair transformBonePair in transformBonePairs)
        {
            transformBonePair.SetRotation();
        }
    }

    private void FixedUpdate()
    {
        foreach (JointBonePair jointBonePair in jointBonePairs)
        {
            jointBonePair.SetRotation();
        }
    }
}

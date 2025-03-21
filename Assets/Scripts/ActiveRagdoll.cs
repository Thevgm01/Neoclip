using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ActiveRagdoll : MonoBehaviour
{
    [SerializeField] private Transform driverSkeleton;
    [SerializeField] private Transform ragdollSkeleton;
    
    private class ActiveRagdollBone
    {
        public Transform driverBone { get; }
        public Transform ragdollBone { get; }
        
        public ActiveRagdollBone(Transform driverBone, Transform ragdollBone)
        {
            this.driverBone = driverBone;
            this.ragdollBone = ragdollBone;
        }
        
        public ActiveRagdollBone(ActiveRagdollBone other) : this(other.driverBone, other.ragdollBone) {}
        
        public override string ToString()
        {
            return driverBone.name;
        }
        
        public virtual void SetRotation() {}
    }

    private class ActiveRagdollTransformBone : ActiveRagdollBone
    {
        public ActiveRagdollTransformBone(ActiveRagdollBone original) : base(original) {}
        
        public override void SetRotation() => ragdollBone.localRotation = driverBone.localRotation;
    }

    private class ActiveRagdollJointBone : ActiveRagdollBone
    {
        private readonly ConfigurableJoint joint;

        private readonly Quaternion worldToStartSpace;
        private readonly Quaternion jointToWorldSpace;
        
        public ActiveRagdollJointBone(ActiveRagdollBone original, ConfigurableJoint joint) : base(original)
        {
            this.joint = joint;

            // https://gist.github.com/mstevenson/7b85893e8caf5ca034e6
            var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
            var up = Vector3.Cross(forward, joint.axis).normalized;
            Quaternion worldToJointSpace = Quaternion.LookRotation(forward, up);
            jointToWorldSpace = Quaternion.Inverse(worldToJointSpace);
            worldToStartSpace = ragdollBone.localRotation * worldToJointSpace;
        }

        public override void SetRotation() =>
            joint.targetRotation = jointToWorldSpace * Quaternion.Inverse(driverBone.localRotation) * worldToStartSpace;
    }
    
    private HashSet<ActiveRagdollBone> bones;
    
    private TreeNode<ActiveRagdollBone> BuildTreeRecursive(Transform driverTransform, Transform ragdollTransform)
    {
        TreeNode<ActiveRagdollBone> node = new TreeNode<ActiveRagdollBone>(
            new ActiveRagdollBone(driverTransform, ragdollTransform));

        if (ragdollTransform.TryGetComponent(out ConfigurableJoint joint))
        {
            bones.Add(new ActiveRagdollJointBone(node.value, joint));
        }
        
        int childCount = driverTransform.childCount;
        if (childCount > 0)
        {
            node.children = new TreeNode<ActiveRagdollBone>[childCount];

            for (int i = 0; i < childCount; i++)
            {
                node.children[i] = BuildTreeRecursive(driverTransform.GetChild(i), ragdollTransform.GetChild(i));
                node.children[i].parent = node;
            }
        }

        return node;
    }
    
    private void Awake()
    {
        bones = new HashSet<ActiveRagdollBone>();
        TreeNode<ActiveRagdollBone> activeRagdollTree = BuildTreeRecursive(driverSkeleton, ragdollSkeleton);

        foreach (TreeNode<ActiveRagdollBone> leafNode in activeRagdollTree.Leaves())
        {
            TreeNode<ActiveRagdollBone> treeNode = leafNode;
            while (treeNode != null && treeNode.value.ragdollBone.GetComponent<Rigidbody>() == null)
            {
                bones.Add(new ActiveRagdollTransformBone(treeNode.value));
                treeNode = treeNode.parent;
            }
        }
    }

    private void LateUpdate()
    {
        foreach (ActiveRagdollBone bone in bones)
        {
            bone.SetRotation();
        }
    }
}

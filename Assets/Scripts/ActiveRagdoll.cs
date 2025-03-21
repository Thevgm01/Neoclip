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
        public Transform driverBone;
        public Transform ragdollBone;
        
        public ActiveRagdollBone(Transform driverBone, Transform ragdollBone)
        {
            this.driverBone = driverBone;
            this.ragdollBone = ragdollBone;
        }
        
        public override string ToString()
        {
            return driverBone.name;
        }
    }

    private class ActiveRagdollJointBone : ActiveRagdollBone
    {
        public ConfigurableJoint joint;

        public Quaternion worldToStartSpace;
        public Quaternion jointToWorldSpace;
        
        public ActiveRagdollJointBone(ActiveRagdollBone bone, ConfigurableJoint joint) : base(bone.driverBone, bone.ragdollBone)
        {
            this.joint = joint;

            // https://gist.github.com/mstevenson/7b85893e8caf5ca034e6
            var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
            var up = Vector3.Cross(forward, joint.axis).normalized;
            Quaternion worldToJointSpace = Quaternion.LookRotation(forward, up);
            jointToWorldSpace = Quaternion.Inverse(worldToJointSpace);
            worldToStartSpace = ragdollBone.localRotation * worldToJointSpace;
        }

        public void SetTargetRotation() =>
            joint.targetRotation = jointToWorldSpace * Quaternion.Inverse(driverBone.localRotation) * worldToStartSpace;
    }
    
    private List<ActiveRagdollJointBone> joints;

    private List<Transform> copyTransformDrivers;
    private List<Transform> copyTransformRagdolls;
    
    private TreeNode<ActiveRagdollBone> BuildTreeRecursive(Transform driverTransform, Transform ragdollTransform)
    {
        TreeNode<ActiveRagdollBone> node = new TreeNode<ActiveRagdollBone>(
            new ActiveRagdollBone(driverTransform, ragdollTransform));

        if (ragdollTransform.TryGetComponent(out ConfigurableJoint joint))
        {
            joints.Add(new ActiveRagdollJointBone(node.value, joint));
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
        joints = new List<ActiveRagdollJointBone>();
        TreeNode<ActiveRagdollBone> activeRagdollTree = BuildTreeRecursive(driverSkeleton, ragdollSkeleton);

        copyTransformDrivers = new List<Transform>();
        copyTransformRagdolls = new List<Transform>();
        // FIXME this is potentially adding the same nodes multiple times
        foreach (TreeNode<ActiveRagdollBone> leafNode in activeRagdollTree.Leaves())
        {
            TreeNode<ActiveRagdollBone> treeNode = leafNode;
            while (treeNode != null && treeNode.value.ragdollBone.GetComponent<Rigidbody>() == null)
            {
                copyTransformDrivers.Add(treeNode.value.driverBone);
                copyTransformRagdolls.Add(treeNode.value.ragdollBone);
                treeNode = treeNode.parent;
            }
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < copyTransformDrivers.Count; i++)
        {
            copyTransformDrivers[i].GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);
            copyTransformRagdolls[i].SetLocalPositionAndRotation(position, rotation);
        }

        for (int i = 0; i < joints.Count; i++)
        {
            joints[i].SetTargetRotation();
        }
    }
}

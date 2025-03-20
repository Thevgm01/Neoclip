using System;
using System.Collections.Generic;
using UnityEngine;

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
    
    private TreeNode<ActiveRagdollBone> activeRagdollTree;

    private List<Transform> copyTransformDrivers;
    private List<Transform> copyTransformRagdolls;

    private TreeNode<ActiveRagdollBone> BuildTreeRecursive(Transform driverTransform, Transform ragdollTransform)
    {
        TreeNode<ActiveRagdollBone> node = new TreeNode<ActiveRagdollBone>(
            new ActiveRagdollBone(driverTransform, ragdollTransform));

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
        activeRagdollTree = BuildTreeRecursive(driverSkeleton, ragdollSkeleton);

        copyTransformDrivers = new List<Transform>();
        copyTransformRagdolls = new List<Transform>();
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
    }
}

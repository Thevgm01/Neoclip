using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ActiveRagdoll : MonoBehaviour
{
    [SerializeField] private Transform driverSkeleton;
    [SerializeField] private Transform ragdollSkeleton;
    
    private HashSet<BonePair> basicBonePairs;
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
        basicBonePairs = new HashSet<BonePair>();
        jointBonePairs = new List<JointBonePair>();
        
        TreeNode<BonePair> bonePairTree = BuildBonePairTreeRecursive(driverSkeleton, ragdollSkeleton);

        foreach (TreeNode<BonePair> leafNode in bonePairTree.Leaves())
        {
            TreeNode<BonePair> node = leafNode;
            while (node != null && node.value.RagdollBone.GetComponent<Rigidbody>() == null)
            {
                basicBonePairs.Add(node.value);
                node = node.parent;
            }
        }
    }

    private void LateUpdate()
    {
        foreach (BonePair basicBonePair in basicBonePairs)
        {
            basicBonePair.SetRotation();
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

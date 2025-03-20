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
        
        public bool hasRigidbodyChild;

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
    
    private TreeNode<ActiveRagdollBone> activeRagdollParentBone;

    private TreeNode<ActiveRagdollBone> SetRecursive(Transform driverTransform, Transform ragdollTransform)
    {
        TreeNode<ActiveRagdollBone> node = new TreeNode<ActiveRagdollBone>(
            new ActiveRagdollBone(driverTransform, ragdollTransform));

        int childCount = driverTransform.childCount;
        if (childCount > 0)
        {
            node.children = new TreeNode<ActiveRagdollBone>[childCount];

            for (int i = 0; i < childCount; i++)
            {
                node.children[i] = SetRecursive(driverTransform.GetChild(i), ragdollTransform.GetChild(i));
                node.children[i].parent = node;
            }
        }

        return node;
    }
    
    private void Awake()
    {
        activeRagdollParentBone = SetRecursive(driverSkeleton, ragdollSkeleton);

        List<ActiveRagdollBone> boneList = new List<ActiveRagdollBone>();
        foreach (ActiveRagdollBone bone in activeRagdollParentBone.Leaves())
        {
            boneList.Add(bone);
        }
        Debug.Log(string.Join("\n", boneList));
    }
}

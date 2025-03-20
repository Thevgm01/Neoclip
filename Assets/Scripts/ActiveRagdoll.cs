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

        public Quaternion startRotation;
        public Quaternion worldToJointSpace;
        public Quaternion jointToWorldSpace;
        
        public ActiveRagdollJointBone(ActiveRagdollBone bone, ConfigurableJoint joint) : base(bone.driverBone, bone.ragdollBone)
        {
            this.joint = joint;

            // https://gist.github.com/mstevenson/7b85893e8caf5ca034e6
            startRotation = ragdollBone.localRotation;

            Vector3 right = joint.axis;
            Vector3 forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized; // Is this needed?
        }

        public void SetTargetRotation()
        {
            Quaternion targetRotation = driverBone.localRotation;
            
            // Calculate the rotation expressed by the joint's axis and secondary axis
            var right = joint.axis;
            var forward = Vector3.Cross (joint.axis, joint.secondaryAxis).normalized;
            var up = Vector3.Cross (forward, right).normalized;
            Quaternion worldToJointSpace = Quaternion.LookRotation (forward, up);
		
            // Transform into world space
            Quaternion resultRotation = Quaternion.Inverse (worldToJointSpace);
		
            // Counter-rotate and apply the new local rotation.
            // Joint space is the inverse of world space, so we need to invert our value
            //if (space == Space.World) {
            //    resultRotation *= startRotation * Quaternion.Inverse (targetRotation);
            //} else {
                resultRotation *= Quaternion.Inverse (targetRotation) * startRotation;
            //}
		
            // Transform back into joint space
            resultRotation *= worldToJointSpace;
		
            // Set target rotation to our newly calculated rotation
            joint.targetRotation = resultRotation;
        }
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

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RagdollBuilder))]
public class RagdollBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RagdollBuilder builder = (RagdollBuilder)target;

        if (DrawDefaultInspector())
        {
            
        }

        if (GUILayout.Button("Create All Joints"))
        {
            Joint[] oldJoints = builder.GetComponentsInChildren<Joint>();
            if (oldJoints.Length > 0)
            {
                foreach (Joint oldJoint in oldJoints)
                {
                    Undo.DestroyObjectImmediate(oldJoint);
                }

                Debug.Log($"RagdollBuilder: Destroyed {oldJoints.Length} Joints");
            }
            
            Rigidbody[] rigidbodies = builder.GetComponentsInChildren<Rigidbody>();
            if (rigidbodies.Length > 0)
            {
                foreach (Rigidbody rigidbody in rigidbodies)
                {
                    Rigidbody parentRigidbody = rigidbody.transform.parent.GetComponentInParent<Rigidbody>();
                    if (parentRigidbody)
                    {
                        ConfigurableJoint newJoint = Undo.AddComponent<ConfigurableJoint>(rigidbody.gameObject);
                        
                        newJoint.connectedBody = parentRigidbody;
                        newJoint.enablePreprocessing = false;
                        newJoint.xMotion = ConfigurableJointMotion.Locked;
                        newJoint.yMotion = ConfigurableJointMotion.Locked;
                        newJoint.zMotion = ConfigurableJointMotion.Locked;
                        
                        JointDrive defaultJointDrive = new JointDrive
                        {
                            positionSpring = 15.0f,
                            positionDamper = 1.0f,
                            maximumForce = 3.402823e+38f // Copied from default
                        };
                        newJoint.angularXDrive = defaultJointDrive;
                        newJoint.angularYZDrive = defaultJointDrive;
                    }
                }
                
                // -1 because the root rigidbody has no parent, so it won't get a joint
                Debug.Log($"RagdollBuilder: Created {rigidbodies.Length - 1} ConfigurableJoints");
            }
        }

        if (GUILayout.Button("Select All Joints"))
        {
            Selection.objects = builder.GetComponentsInChildren<ConfigurableJoint>();
        }
    }
}
#endif
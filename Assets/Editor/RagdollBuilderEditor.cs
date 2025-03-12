#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RagdollBuilder))]
public class RagdollBuilderEditor : Editor
{
    private RagdollBuilder builder;
    private Rigidbody head;
    
    public override void OnInspectorGUI()
    {
        if (!builder)
        {
            builder = (RagdollBuilder)serializedObject.targetObject;
        }

        if (head == null && builder != null)
        {
            foreach (Rigidbody rigidbody in builder.GetComponentsInChildren<Rigidbody>())
            {
                if (rigidbody.name.ToLower().Contains("head"))
                {
                    head = rigidbody;
                    break;
                }
            }
        }

        DrawDefaultInspector();

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
                float totalMass = 0.0f;

                foreach (Rigidbody rigidbody in rigidbodies)
                {
                    Undo.RecordObject(rigidbody, $"Set mass by density");
                    // rigidbody.SetDensity() does NOTHING!!!
                    rigidbody.mass = Utils.CalculateVolume(rigidbody.GetComponent<Collider>()) * 
                                     Utils.Density.WATER * 
                                     builder.initialMassMult;
                    totalMass += rigidbody.mass;

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
                            positionSpring = 35.0f,
                            positionDamper = 1.0f,
                            maximumForce = 3.402823e+38f // Copied from default
                        };
                        newJoint.angularXDrive = defaultJointDrive;
                        newJoint.angularYZDrive = defaultJointDrive;
                    }
                }
                
                Debug.Log($"RagdollBuilder: Set the mass of {rigidbodies.Length} rigidbodies. Total mass is {totalMass} kg.");

                // -1 because the root rigidbody has no parent, so it won't get a joint
                Debug.Log($"RagdollBuilder: Created {rigidbodies.Length - 1} ConfigurableJoints.");
            }
        }

        if (GUILayout.Button("Select All Joints"))
        {
            Selection.objects = builder.GetComponentsInChildren<ConfigurableJoint>();
        }
        
        if (GUILayout.Button(head.isKinematic ? "Free head" : "Lock head"))
        {
            Undo.RecordObject(head, $"Set head kinematic {!head.isKinematic}");
            head.isKinematic = !head.isKinematic;
        }
    }
}
#endif
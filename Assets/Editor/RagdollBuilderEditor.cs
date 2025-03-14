#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RagdollBuilder))]
public class RagdollBuilderEditor : Editor
{
    private RagdollBuilder builder;
    private Rigidbody head;
    
    private void TryDestroyObjectsImmediate(Object[] objs)
    {
        foreach (Object obj in objs)
        {
            TryDestroyObjectImmediate(obj);
        }
    }
    
    private void TryDestroyObjectImmediate(Object obj)
    {
        if (obj != null)
        {
            Undo.DestroyObjectImmediate(obj);
        }
    }
    
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

        builder.dragMeshLayer = EditorGUILayout.LayerField("Drag Mesh Layer", builder.dragMeshLayer);
        
        if (GUILayout.Button("Build Ragdoll"))
        {
            // Delete legacy objects
            TryDestroyObjectsImmediate(GameObject.FindGameObjectsWithTag("DragMesh"));
            
            Rigidbody[] rigidbodies = builder.GetComponentsInChildren<Rigidbody>();
            if (rigidbodies.Length > 0)
            {
                float totalMass = 0.0f;
                int dragMeshesCreated = 0;

                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    Rigidbody rigidbody = rigidbodies[i];
                    
                    GameObject gameObject = rigidbody.gameObject;
                    gameObject.layer = builder.dragMeshLayer;
                    
                    TryDestroyObjectImmediate(gameObject.GetComponent<Joint>());
                    TryDestroyObjectImmediate(gameObject.GetComponent<MeshFilter>());
                    TryDestroyObjectImmediate(gameObject.GetComponent<MeshRenderer>());
                    
                    Collider collider = rigidbody.GetComponent<Collider>();
                    int colliderHash = Utils.HashCollider(collider);
                    
                    // Set the mass
                    Undo.RecordObject(rigidbody, $"Set rigidbody values");
                    // rigidbody.SetDensity() does NOTHING!!!
                    rigidbody.mass = Utils.CalculateVolume(collider) * 
                                     Utils.Density.WATER * 
                                     builder.initialMassMult;
                    rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                    rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                    // Keep track of the total mass
                    totalMass += rigidbody.mass;
                    
                    // Add a ConfigurableJoint
                    Rigidbody parentRigidbody = rigidbody.transform.parent.GetComponentInParent<Rigidbody>();
                    if (parentRigidbody)
                    {
                        ConfigurableJoint newJoint = Undo.AddComponent<ConfigurableJoint>(gameObject);
                        
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

                    Mesh dragMesh = Utils.ColliderToMesh(collider, new Color32((byte)(i * 8), 0, 0, 255));
                    if (dragMesh)
                    {
                        MeshFilter meshFilter = Undo.AddComponent<MeshFilter>(gameObject);
                        MeshRenderer meshRenderer = Undo.AddComponent<MeshRenderer>(gameObject);
                        
                        meshFilter.sharedMesh = dragMesh;
                        meshRenderer.material = builder.dragMeshMaterial;
                        
                        dragMeshesCreated++;
                    }
                }
                
                Debug.Log($"RagdollBuilder: Set the mass of {rigidbodies.Length} rigidbodies. Total mass is {totalMass} kg.");

                // -1 because the root rigidbody has no parent, so it won't get a joint
                Debug.Log($"RagdollBuilder: Created {rigidbodies.Length - 1} ConfigurableJoints.");
                
                Debug.Log($"RagdollBuilder: Created {dragMeshesCreated} drag meshes.");
            }
        }

        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Select all...");
        EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Joints"))
            {
                Selection.objects = builder.GetComponentsInChildren<ConfigurableJoint>();
            }
            if (GUILayout.Button("Rigidbodies"))
            {
                Selection.objects = builder.GetComponentsInChildren<Rigidbody>();
            }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button(head.isKinematic ? "Free head" : "Lock head"))
        {
            Undo.RecordObject(head, $"Set head kinematic {!head.isKinematic}");
            head.isKinematic = !head.isKinematic;
        }
    }
}
#endif
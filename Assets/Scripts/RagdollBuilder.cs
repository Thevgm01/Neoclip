#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;

[ExecuteInEditMode]
public class RagdollBuilder : MonoBehaviour
{
    public bool autoselectMirrorBone = true;
    public float initialMassMult = 1.0f;
    public float angularDamping = 0.05f;
    public Material dragMeshMaterial;
    public PhysicsMaterial physicsMaterial;
    public LayerNumber defaultLayer;
        
    private UnityEngine.Object lastSelectedObject = null; // Double-clicking won't select the mirror
    private UnityEngine.Object mirrorBoneObject = null;
    
    private void OnEnable()
    {
        Selection.selectionChanged += SelectOtherBone;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= SelectOtherBone;
    }

    private void SelectOtherBone()
    {
        if (autoselectMirrorBone &&
            Selection.count == 1 &&
            Selection.activeGameObject &&
            Selection.activeObject != lastSelectedObject &&
            Selection.activeGameObject.GetComponentInParent<RagdollBuilder>() == this)
        {
            string path = "";
            Transform temp = Selection.activeTransform;
            while (temp != transform)
            {
                path = temp.name + "/" + path;
                temp = temp.parent;
            }

            if (path.ToLower().Contains("left"))
            {
                path = path.Replace("left", "right");
                path = path.Replace("Left", "Right");
                path = path.Replace("LEFT", "RIGHT");
            }
            else if(path.ToLower().Contains("right"))
            {
                path = path.Replace("right", "left");
                path = path.Replace("Right", "Left");
                path = path.Replace("RIGHT", "LEFT");
            }
            else
            {
                lastSelectedObject = Selection.activeObject;
                return;
            }

            Transform other = transform.Find(path);

            if (other)
            {
                mirrorBoneObject = (UnityEngine.Object)other.gameObject;
                //EditorGUIUtility.PingObject(other.gameObject);
                Debug.Log($"RagdollBuilder: Selecting mirror bone at {path}");
            }
            else
            {
                Debug.Log($"RagdollBuilder: Could not find mirror bone at {path}");
            }
        }
        
        lastSelectedObject = Selection.activeObject;
    }

    private void Update()
    {
        if (mirrorBoneObject)
        {
            Selection.objects = new[] { Selection.activeObject, mirrorBoneObject };
            mirrorBoneObject = null;
        }
    }
    
    public void BuildRagdoll()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        
        if (rigidbodies.Length > 0)
        {
            Rigidbody tempRigidbody = gameObject.AddComponent<Rigidbody>();
            ConfigurableJoint tempJoint = gameObject.AddComponent<ConfigurableJoint>();
            tempJoint.xMotion = ConfigurableJointMotion.Locked;
            tempJoint.yMotion = ConfigurableJointMotion.Locked;
            tempJoint.zMotion = ConfigurableJointMotion.Locked;
            
            float totalMass = 0.0f;
            int dragMeshesCreated = 0;
            
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rigidbody = rigidbodies[i];
                GameObject gameObject = rigidbody.gameObject;
                
                gameObject.layer = defaultLayer.value;
                
                //UndoUtils.TryDestroyObjectsImmediate(gameObject.GetComponents<Joint>());
                UndoUtils.TryDestroyObjectImmediate(gameObject.GetComponent<MeshFilter>());
                UndoUtils.TryDestroyObjectImmediate(gameObject.GetComponent<MeshRenderer>());
                
                // Modify the primary collider, add a second trigger collider if necessary
                Collider collider;
                Collider[] colliders = rigidbody.GetComponents<Collider>();
                if (colliders.Length == 0)
                {
                    Debug.LogError($"RagdollBuilder: No collider for rigidbody on {gameObject.name}");
                    return;
                }
                else
                {
                    collider = colliders[0];
                    Undo.RecordObject(collider, $"Set colliders");
                    collider.enabled = true;
                    collider.sharedMaterial = physicsMaterial;
                    collider.isTrigger = false;

                    for (int j = 1; j < colliders.Length; j++)
                    {
                        Undo.DestroyObjectImmediate(colliders[j]);
                    }
                }
                
                // Set the mass
                Undo.RecordObject(rigidbody, $"Set rigidbody values");
                // rigidbody.SetDensity() does NOTHING!!!
                rigidbody.mass = collider.CalculateVolume() * 
                                 Constants.Density.MEAT * 
                                 initialMassMult;
                rigidbody.angularDamping = angularDamping;
                rigidbody.useGravity = false; // We're doing gravity manually
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                
                // Keep track of the total mass
                totalMass += rigidbody.mass;
                
                // Add a ConfigurableJoint
                Rigidbody parentRigidbody = rigidbody.transform.parent.GetComponentInParent<Rigidbody>();
                if (parentRigidbody && parentRigidbody != tempRigidbody)
                {
                    ConfigurableJoint joint = UndoUtils.GetOrAddComponent<ConfigurableJoint>(gameObject);
                    
                    GenericUtils.CopyConfigurableJointValues(tempJoint, joint);
                    joint.connectedBody = parentRigidbody;
                }
                
                // Create and add the DragMesh
                Mesh dragMesh = collider.ToMeshWithVertexColor(new Color32((byte)(dragMeshesCreated * 8), 0, 0, 255));
                if (dragMesh)
                {
                    dragMesh.normals = Array.Empty<Vector3>();
                    
                    MeshFilter meshFilter = Undo.AddComponent<MeshFilter>(gameObject);
                    MeshRenderer meshRenderer = Undo.AddComponent<MeshRenderer>(gameObject);
                    
                    meshFilter.sharedMesh = dragMesh;
                    meshRenderer.material = dragMeshMaterial;
                    
                    dragMeshesCreated++;
                }
            }
            
            DestroyImmediate(tempJoint);
            DestroyImmediate(tempRigidbody);
            
            Debug.Log($"RagdollBuilder: Set the mass of {rigidbodies.Length - 1} rigidbodies. Total mass is {totalMass} kg.");

            // -1 because the root rigidbody has no parent, so it won't get a joint
            Debug.Log($"RagdollBuilder: Created {rigidbodies.Length - 2} ConfigurableJoints.");
            
            Debug.Log($"RagdollBuilder: Created {dragMeshesCreated} drag meshes.");
        }
    }
}
#endif
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
    private enum ColliderType { SPHERE, CAPSULE, BOX, NONE, IGNORE }
    
    [SerializedDictionary("Collider Type", "Matching Bone Names")]
    [SerializeField] private SerializedDictionary<string, ColliderType> collidersForBoneName;

    public bool autoselectMirrorBone = true;
    public float initialMassMult = 1.0f;
    public float jointSpringStrength = 20.0f;
    public Material dragMeshMaterial;
    public PhysicsMaterial physicsMaterial;
    public LayerNumber defaultLayer;
    public LayerNumber triggerLayer;
    
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

    private void SetElbowParameters(ConfigurableJoint joint)
    {
        SoftJointLimit jointLimit = new SoftJointLimit();
        jointLimit.limit = 150.0f;

        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        joint.highAngularXLimit = jointLimit;
    }
    
    public void BuildRagdoll()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        if (rigidbodies.Length > 0)
        {
            float totalMass = 0.0f;
            int dragMeshesCreated = 0;

            string triggerLayerName = LayerMask.LayerToName(triggerLayer.value);
            
            Utils.TryDestroyObjectsImmediate(GameObject.FindGameObjectsWithTag(triggerLayerName));

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rigidbody = rigidbodies[i];
                
                GameObject gameObject = rigidbody.gameObject;
                gameObject.layer = defaultLayer.value;
                
                Utils.TryDestroyObjectImmediate(gameObject.GetComponent<Joint>());
                Utils.TryDestroyObjectImmediate(gameObject.GetComponent<MeshFilter>());
                Utils.TryDestroyObjectImmediate(gameObject.GetComponent<MeshRenderer>());
                
                Collider collider = rigidbody.GetComponent<Collider>();
                Undo.RecordObject(collider, $"Set collider physics material");
                collider.sharedMaterial = physicsMaterial;
                int colliderHash = Utils.HashCollider(collider);

                GameObject triggerObject = new GameObject();
                triggerObject.name = gameObject.name + "_trigger";
                triggerObject.tag = triggerLayerName;
                triggerObject.layer = triggerLayer.value;
                triggerObject.transform.SetParent(gameObject.transform, false);
                Collider triggerCollider = collider.CopyTo(triggerObject);
                triggerCollider.sharedMaterial = null;
                triggerCollider.isTrigger = true;
                Undo.RegisterCreatedObjectUndo(triggerObject, $"Create trigger object");
                
                // Set the mass
                Undo.RecordObject(rigidbody, $"Set rigidbody values");
                // rigidbody.SetDensity() does NOTHING!!!
                rigidbody.mass = collider.CalculateVolume() * 
                                 Utils.Density.MEAT * 
                                 initialMassMult;
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
                        positionSpring = jointSpringStrength,
                        positionDamper = 1.0f,
                        maximumForce = 3.402823e+38f // Copied from default
                    };
                    newJoint.angularXDrive = defaultJointDrive;
                    newJoint.angularYZDrive = defaultJointDrive;

                    if (gameObject.name.Contains("LeftLeg") || gameObject.name.Contains("RightLeg"))
                    {
                        SetElbowParameters(newJoint);
                    }
                    else if (gameObject.name.Contains("LeftForeArm"))
                    {
                        newJoint.axis = Vector3.forward;
                        SetElbowParameters(newJoint);
                    }
                    else if (gameObject.name.Contains("RightForeArm"))
                    {
                        newJoint.axis = Vector3.back;
                        SetElbowParameters(newJoint);
                    }
                }

                Mesh dragMesh = collider.ToMeshWithVertexColor(new Color32((byte)(i * 8), 0, 0, 255));
                if (dragMesh)
                {
                    MeshFilter meshFilter = Undo.AddComponent<MeshFilter>(gameObject);
                    MeshRenderer meshRenderer = Undo.AddComponent<MeshRenderer>(gameObject);
                    
                    meshFilter.sharedMesh = dragMesh;
                    meshRenderer.material = dragMeshMaterial;
                    
                    dragMeshesCreated++;
                }
            }
            
            Debug.Log($"RagdollBuilder: Set the mass of {rigidbodies.Length} rigidbodies. Total mass is {totalMass} kg.");

            // -1 because the root rigidbody has no parent, so it won't get a joint
            Debug.Log($"RagdollBuilder: Created {rigidbodies.Length - 1} ConfigurableJoints.");
            
            Debug.Log($"RagdollBuilder: Created {dragMeshesCreated} drag meshes.");
        }
    }
}
#endif
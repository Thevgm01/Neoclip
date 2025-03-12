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

    [SerializeField] private bool autoselectMirrorBone = true;
    
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
            Selection.activeObject &&
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
}
#endif
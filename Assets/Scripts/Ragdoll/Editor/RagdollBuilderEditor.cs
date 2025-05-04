#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RagdollBuilder))]
public class RagdollBuilderEditor : Editor
{
    private RagdollBuilder builder;
    private Rigidbody hips;
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
                string nameLower = rigidbody.name.ToLower();
                if (nameLower.Contains("hips"))
                {
                    hips = rigidbody;
                }
                else if (nameLower.Contains("head"))
                {
                    head = rigidbody;
                }
            }
        }
        
        DrawDefaultInspector();
        
        if (GUILayout.Button("Build Ragdoll"))
        {
            builder.BuildRagdoll();
        }

        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Select all...");
        EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Colliders"))
            {
                Selection.objects = builder.GetComponentsInChildren<Collider>();
            }
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
        
        EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(hips.isKinematic ? "Free hips" : "Lock hips"))
            {
                Undo.RecordObject(head, $"Set hips kinematic {!hips.isKinematic}");
                hips.isKinematic = !hips.isKinematic;
            }
            if (GUILayout.Button(head.isKinematic ? "Free head" : "Lock head"))
            {
                Undo.RecordObject(head, $"Set head kinematic {!head.isKinematic}");
                head.isKinematic = !head.isKinematic;
            }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
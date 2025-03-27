#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[DefaultExecutionOrder(-999)]
public class GizmoQueue : MonoBehaviour
{
    public enum DrawCriteria
    {
        NULL,
        ALWAYS,
        SELECTED_ANY,
        SELECTED_EXCLUSIVE
    }
    
    public enum Shape
    {
        NULL,
        CUBE,
        WIRE_CUBE,
        SPHERE,
        WIRE_SPHERE,
    }
    
    public struct GizmoDrawRequest
    {
        public Transform owner;
        public DrawCriteria criteria;
        public Shape shape;
        public Color color;
        public Vector3 position;
        public Vector3 size;
        public float radius
        {
            get => size.x;
            set => size.x = value;
        }
        public bool ragdollRelative;
    }
    
    [SerializeField] private RagdollAverages ragdollAverages;
    
    private static List<GizmoDrawRequest> fixedUpdateRequests = new ();
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        fixedUpdateRequests.Clear();
    }

    public static void SubmitRequest(GizmoDrawRequest request)
    {
        if (Time.inFixedTimeStep) fixedUpdateRequests.Add(request);
    }

    public static void RepeatRequest(GizmoDrawRequest request)
    {
        if (Time.inFixedTimeStep)
        {
            if (fixedUpdateRequests.Count == 0)
            {
                Debug.LogError("GizmoQueue.RepeatRequest: No requests have been submitted this frame.");
                return;
            }
            
            GizmoDrawRequest lastRequest = fixedUpdateRequests[^1];
            if (request.owner == null) request.owner = lastRequest.owner;
            if (request.criteria == default) request.criteria = lastRequest.criteria;
            if (request.shape == default) request.shape = lastRequest.shape;
            if (request.color == default) request.color = lastRequest.color;
            if (request.position == default) request.position = lastRequest.position;
            if (request.size == default) request.size = lastRequest.size;
            if (request.ragdollRelative == false) request.ragdollRelative = lastRequest.ragdollRelative;
            
            fixedUpdateRequests.Add(request);
        }
    }
    
    private static bool IsChildOf(Transform child, Transform parent)
    {
        Transform temp = child;
        while (temp != null && temp != parent)
        {
            temp = temp.parent;
        }
        return temp == parent;
    }

    private static bool IsChildOfAny(Transform child, Transform[] parents)
    {
        Transform temp = child;
        while (temp != null)
        {
            foreach (Transform parent in parents)
            {
                if (temp == parent)
                {
                    return true;
                }
            }
            temp = temp.parent;
        }
        return false;
    }
    
    private void OnDrawGizmos()
    {
        Vector3 ragdollRelativePosition = default;
        if (Application.isPlaying)
        {
            ragdollRelativePosition = ragdollAverages.AverageLinearVelocity * (Time.time - Time.fixedTime);
        }

        foreach (GizmoDrawRequest request in fixedUpdateRequests)
        {
            if (request.criteria == DrawCriteria.SELECTED_EXCLUSIVE && Selection.activeTransform != request.owner ||
                request.criteria == DrawCriteria.SELECTED_ANY && !IsChildOfAny(request.owner, Selection.transforms))
                continue;
            
            if (request.color != default)
            {
                Gizmos.color = request.color;
            }

            Vector3 position = request.ragdollRelative
                ? request.position + ragdollRelativePosition
                : request.position;
            
            switch (request.shape)
            {
                case Shape.CUBE: Gizmos.DrawCube(position, request.size); break;
                case Shape.WIRE_CUBE: Gizmos.DrawWireCube(position, request.size); break;
                case Shape.SPHERE: Gizmos.DrawSphere(position, request.radius); break;
                case Shape.WIRE_SPHERE: Gizmos.DrawWireSphere(position, request.radius); break;
            }
        }
    }

    private void FixedUpdate()
    {
        fixedUpdateRequests.Clear();
    }
}
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class GizmoAnywhere : MonoBehaviour
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

    public enum RagdollRelative
    {
        NULL,
        FALSE,
        TRUE
    }
    
    public struct GizmoDrawRequest
    {
        public Transform owner;
        public DrawCriteria criteria;
        public Shape shape;
        public RagdollRelative ragdollRelative;
        public Color color;
        public Vector3 position;
        public Vector3 size;
        public float radius
        {
            get => size.x;
            set => size.x = value;
        }
        public bool isDummy;
    }
#if UNITY_EDITOR
    [SerializeField] private RagdollHelper ragdollHelper;
    
    private static List<GizmoDrawRequest> fixedUpdateRequests = new ();
    private static Stack<GizmoDrawRequest> requests = new ();
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        fixedUpdateRequests.Clear();
        requests.Clear();
    }
#endif
    
    public static void SubmitRequest(GizmoDrawRequest request)
    {
#if UNITY_EDITOR
        if (request.criteria == DrawCriteria.SELECTED_EXCLUSIVE && Selection.activeTransform != request.owner ||
            request.criteria == DrawCriteria.SELECTED_ANY && !GenericUtils.IsChildOfAny(request.owner, Selection.transforms))
            return;
        
        if (Time.inFixedTimeStep) fixedUpdateRequests.Add(request);
        else requests.Push(request);
#endif
    }
    
    public static void RepeatRequest(GizmoDrawRequest request)
    {
#if UNITY_EDITOR
        if (Time.inFixedTimeStep && fixedUpdateRequests.Count == 0 || !Time.inFixedTimeStep && requests.Count == 0)
            return;
        
        GizmoDrawRequest lastRequest = Time.inFixedTimeStep 
            ? fixedUpdateRequests[^1] 
            : requests.Peek();
        
        if (request.owner == null) request.owner = lastRequest.owner;
        if (request.criteria == default) request.criteria = lastRequest.criteria;
        if (request.shape == default) request.shape = lastRequest.shape;
        if (request.ragdollRelative == default) request.ragdollRelative = lastRequest.ragdollRelative;
        if (request.color == default) request.color = lastRequest.color;
        if (request.position == default) request.position = lastRequest.position;
        if (request.size == default) request.size = lastRequest.size;
        // if (request.isDummy == false) request.isDummy = lastRequest.isDummy
        
        SubmitRequest(request);
#endif
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 ragdollRelativePosition = Vector3.zero;
        if (Application.isPlaying)
        {
            ragdollRelativePosition = ragdollHelper.AverageLinearVelocity.Value * (Time.time - Time.fixedTime);
        }

        int numGizmos = fixedUpdateRequests.Count + requests.Count;
        for (int i = 0; i < numGizmos; i++)
        {
            GizmoDrawRequest request = i < fixedUpdateRequests.Count
                ? fixedUpdateRequests[i]
                : requests.Pop();

            if (request.isDummy)
            {
                continue;
            }
            
            if (request.color != default)
            {
                Gizmos.color = request.color;
            }

            Vector3 position = i < fixedUpdateRequests.Count && request.ragdollRelative == RagdollRelative.TRUE
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
#endif
}
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class GizmoAnywhere : MonoBehaviour
{
    public enum Selected
    {
        REPEAT,
        ALWAYS,
        OWNER,
        PARENT_OF_OWNER
    }
    
    public enum Shape
    {
        REPEAT,
        CUBE,
        WIRE_CUBE,
        SPHERE,
        WIRE_SPHERE,
    }

    public enum RagdollRelative
    {
        REPEAT,
        NO,
        YES
    }
    
    public struct Request
    {
        public bool isDummy;
        public Transform owner;
        public Selected selected;
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
    }
#if UNITY_EDITOR
    [SerializeField] private RagdollHelper targetForRagdollRelative;
    
    private static Stack<Request> requests = new ();
    
    private static List<GizmoDrawRequest> fixedUpdateRequests = new ();
    private static Stack<GizmoDrawRequest> requests = new ();
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        requests.Clear();
        fixedUpdateRequests.Clear();
    }
#endif
    
    public static void SubmitRequest(GizmoDrawRequest request)
    public static void SubmitRequest(Request request)
    {
#if UNITY_EDITOR
        if (request.selected == Selected.OWNER && Selection.activeTransform != request.owner ||
            request.selected == Selected.PARENT_OF_OWNER && !GenericUtils.IsChildOfAny(request.owner, Selection.transforms))
            return;
        
        if (Time.inFixedTimeStep) fixedUpdateRequests.Add(request);
        else requests.Push(request);
#endif
    }
    
    public static void RepeatRequest(GizmoDrawRequest request)
    public static void RepeatRequest(Request request)
    {
#if UNITY_EDITOR
        if (Time.inFixedTimeStep && fixedUpdateRequests.Count == 0 || !Time.inFixedTimeStep && requests.Count == 0)
            return;
        
        Request previousRequest = Time.inFixedTimeStep 
            ? fixedUpdateRequests[^1] 
            : requests.Peek();
        
        // if (request.isDummy == false) request.isDummy = lastRequest.isDummy
        if (request.owner == null) request.owner = previousRequest.owner;
        if (request.selected == default) request.selected = previousRequest.selected;
        if (request.shape == default) request.shape = previousRequest.shape;
        if (request.ragdollRelative == default) request.ragdollRelative = previousRequest.ragdollRelative;
        if (request.color == default) request.color = previousRequest.color;
        if (request.position == default) request.position = previousRequest.position;
        if (request.size == default) request.size = previousRequest.size;
        
        SubmitRequest(request);
#endif
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 ragdollRelativePosition = Vector3.zero;
        if (Application.isPlaying)
        {
            // Predict where the ragdoll would be based on its velocity and the last time it was updated
            float timeSinceFixedUpdate = Time.time - Time.fixedTime;
            ragdollRelativePosition = timeSinceFixedUpdate * targetForRagdollRelative.AverageLinearVelocity.Value;
        }

        int numGizmos = fixedUpdateRequests.Count + requests.Count;
        for (int i = 0; i < numGizmos; i++)
        {
            bool isFixedUpdateRequest = i < fixedUpdateRequests.Count;
            
            Request request = isFixedUpdateRequest
                ? fixedUpdateRequests[i]
                : requests.Pop();

            if (request.isDummy)
            {
                continue;
            }
            
            Gizmos.color = request.color;
            
            Vector3 position = request.position;
            
            if (isFixedUpdateRequest && request.ragdollRelative == RagdollRelative.YES)
            {
                position += ragdollRelativePosition;
            }
            
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
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Editor-only class that adds additional Gizmo drawing functionality, including:
/// <list type="bullet">
/// <item><description>Drawing Gizmos in <c>FixedUpdate()</c></description></item>
/// <item><description>Making Gizmos move smoothly with the main ragdoll.</description></item>
/// <item><description>The ability to repeat requests, keeping most of the information the same but changing some parameters.</description></item>
/// </list>
/// </summary>
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
        /// <summary>
        /// If true, record this request, but don't actually draw it. used for <c>RepeatRequest()</c>.
        /// </summary>
        public bool isDummy;
        /// <summary>
        /// The Transform that owns this request, to be used with <c>selected</c>.
        /// </summary>
        public Transform owner;
        /// <summary>
        /// The conditions for drawing this request, based on the objects selected in the hierarchy and the <c>owner</c>.
        /// </summary>
        public Selected selected;
        /// <summary>
        /// Which primitive Gizmo shape to draw.
        /// </summary>
        public Shape shape;
        /// <summary>
        /// If true and this request is submitted during <c>FixedUpdate()</c>, the request's position will move relative to the interpolated position of the ragdoll.
        /// </summary>
        public RagdollRelative ragdollRelative;
        /// <summary>
        /// The color of the Gizmo.
        /// </summary>
        public Color color;
        /// <summary>
        /// The position of the Gizmo.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The size of the Gizmo. For spheres use <c>radius</c> instead.
        /// </summary>
        public Vector3 size;
        /// <summary>
        /// The radius of the Gizmo. For cubes use <c>size</c> instead.
        /// </summary>
        public float radius
        {
            get => size.x;
            set => size.x = value;
        }
    }
#if UNITY_EDITOR
    [SerializeField] private RagdollHelper targetForRagdollRelative;
    
    // Keep track of requests submitted this frame.
    private static Stack<Request> requests = new ();
    
    // FixedUpdate() usually runs less frequently than Update(). If we used a Stack like with the standard requests it
    // would flicker because new requests aren't being submitted quickly enough. Instead keep them as a list so they can
    // be drawn multiple times until we hit FixedUpdate() again.
    private static List<Request> fixedUpdateRequests = new ();
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        requests.Clear();
        fixedUpdateRequests.Clear();
    }
#endif
    
    /// <summary>
    /// Draw a gizmo during the next OnDrawGizmos() call. Can be run from any context, even FixedUpdate().
    /// </summary>
    /// <param name="request"></param>
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
    
    /// <summary>
    /// Submit a new request using the properties of the previous request as a base, if new properties are not explicitly assigned.
    /// </summary>
    /// <seealso cref="SubmitRequest"/>
    /// <param name="request"></param>
    public static void RepeatRequest(Request request)
    {
#if UNITY_EDITOR
        // Can't repeat what ain't there!
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
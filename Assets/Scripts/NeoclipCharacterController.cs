using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class NeoclipCharacterController : MonoBehaviour
{
    [SerializeField] private RagdollHelper ragdollHelper;
    [SerializeField] private DragCamera dragCamera;
    [SerializeField] private NeoclipCameraController cameraController;
    [SerializeField] private ActiveRagdoll activeRagdoll;
    [SerializeField] private ExitDirectionFinder exitDirectionFinder;
    [SerializeField] private ImpactTimeEstimator impactTimeEstimator;
    [SerializeField] private Animator animator;

    [Space]
    [SerializeField] private ForcesSO fallingForces;
    [SerializeField] private ForcesSO clippingForces;
    [SerializeField] private ForcesSO ejectingForces;
    [SerializeField] private float secondsToStayClipping = 0.2f;
    [SerializeField] private float angularSlowdown = 0.1f;
    [SerializeField] private float antiSkateVelocityThreshold = 0.2f;
    [SerializeField] private float maxMoveSpeed = 5.0f;
    [SerializeField] private float moveAcceleration = 1.0f;
    [SerializeField] private LayerNumber defaultLayer;
    [SerializeField] private LayerNumber noclipLayer;
    [SerializeField] private AnimationCurve panicAtImpactTime;
    [SerializeField] private AnimationCurve panicMultAtSpeed;
    [SerializeField] private AnimationCurve panicAtAngularVelocity;
    
    [Space]
    [SerializeField] private InputActionReference mouseLookAction;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference noclipAction;
    [SerializeField] private InputActionReference bounceAction;
    [SerializeField] private InputActionReference advanceGameStateAction;
    [SerializeField] private InputActionReference reverseGameStateAction;
    private Vector2 moveInput;
    private bool desiredNoclipState;

    [Flags]
    private enum GameState
    {
        PAUSED = 0,
        SIMULATING = 1,
        FOCUSED = 2
    }
    private GameState gameState = GameState.SIMULATING;
            
    private float[] boneSurfaceAreas;
    private ClippingUtils.ClipState[] boneClipStates;
    private int noclipBufferFrames;
    private int animPanicID;
    
    public event Action OnNoclipStarted;
    public event Action OnNoclipStopped;
    public bool IsClipping { get; private set; }
        
    private void OnMoveInput(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    private void OnNoclipInput(InputAction.CallbackContext context)
    {
        desiredNoclipState = context.ReadValueAsButton();
    }
    private void OnBounceInput(InputAction.CallbackContext context)
    {
        bool enabled = context.ReadValueAsButton();
        
        if (enabled)
        {
            for (int i = 0; i < ragdollHelper.NumBones; i++)
            {
                Collider collider = ragdollHelper.GetCollider(i);
                collider.material.bounciness = 1.0f;
                collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
            }
            
            for (int i = 0; i < ragdollHelper.NumJoints; i++)
            {
                ConfigurableJoint joint = ragdollHelper.GetJoint(i);
                joint.angularXDrive = new JointDrive
                {
                    positionSpring = joint.angularXDrive.positionSpring * 2000.0f,
                    positionDamper = joint.angularXDrive.positionDamper * 5000.0f,
                    maximumForce = joint.angularXDrive.maximumForce,
                    useAcceleration = joint.angularXDrive.useAcceleration
                };
                joint.angularYZDrive = new JointDrive
                {
                    positionSpring = joint.angularYZDrive.positionSpring * 2000.0f,
                    positionDamper = joint.angularYZDrive.positionDamper * 5000.0f,
                    maximumForce = joint.angularYZDrive.maximumForce,
                    useAcceleration = joint.angularYZDrive.useAcceleration
                };
            }
        }
    }

    private void AdvanceGameState()
    {
        if ((gameState & GameState.FOCUSED) == 0)
        {
            cameraController.BindMouseLook(mouseLookAction, true);
            moveAction.action.performed += OnMoveInput; moveAction.action.canceled += OnMoveInput;
            noclipAction.action.performed += OnNoclipInput; noclipAction.action.canceled += OnNoclipInput;
            bounceAction.action.performed += OnBounceInput; bounceAction.action.canceled += OnBounceInput;
            gameState |= GameState.FOCUSED;
        }
        else if ((gameState & GameState.SIMULATING) == 0)
        {
            ragdollHelper.Unfreeze();
            gameState |= GameState.SIMULATING;
        }

        if (gameState == (GameState.FOCUSED | GameState.SIMULATING))
        {
            advanceGameStateAction.action.performed -= AdvanceGameState;
        }
        else if (gameState == GameState.PAUSED)
        {
            reverseGameStateAction.action.performed += ReverseGameState;
        }
        
        Debug.Log($"{nameof(NeoclipCharacterController)}.{nameof(AdvanceGameState)}: {gameState}");
    }
    private void AdvanceGameState(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton()) AdvanceGameState();
    }

    private void ReverseGameState()
    {
        if (gameState == (GameState.FOCUSED | GameState.SIMULATING))
        {
            advanceGameStateAction.action.performed += AdvanceGameState;
        }
        
        if (gameState == GameState.PAUSED)
        {
#if UNITY_EDITOR
            EditorApplication.isPaused = true;
#endif
        }
        else if ((gameState & GameState.FOCUSED) > 0)
        {
            cameraController.BindMouseLook(mouseLookAction, false);
            moveAction.action.performed -= OnMoveInput; moveAction.action.canceled -= OnMoveInput;
            noclipAction.action.performed -= OnNoclipInput; noclipAction.action.canceled -= OnNoclipInput;
            bounceAction.action.performed -= OnBounceInput; bounceAction.action.canceled -= OnBounceInput;
            gameState ^= GameState.FOCUSED;
        }
        else if ((gameState & GameState.SIMULATING) > 0)
        {
            ragdollHelper.Freeze();
            gameState ^= GameState.SIMULATING;
        }
        
#if !UNITY_EDITOR
        if (gameState == GameState.PAUSED)
        {
            reverseGameStateAction.action.performed -= ReverseGameState;
        }
#endif
        
        Debug.Log($"{nameof(NeoclipCharacterController)}.{nameof(ReverseGameState)}: {gameState}");
    }
    private void ReverseGameState(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton()) ReverseGameState();
    }
    
    private void SetNoclipLayers() => ragdollHelper.SetLayers(noclipLayer.value);
    private void SetDefaultLayers() => ragdollHelper.SetLayers(defaultLayer.value);
    
    private void Awake()
    {
        boneSurfaceAreas = new float[ragdollHelper.NumBones];
        boneClipStates = new ClippingUtils.ClipState[ragdollHelper.NumBones];
        animPanicID = Animator.StringToHash("Panic");

        OnNoclipStarted += SetNoclipLayers;
        OnNoclipStopped += SetDefaultLayers;
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void OnEnable()
    {
        advanceGameStateAction.action.performed += AdvanceGameState;
        reverseGameStateAction.action.performed += ReverseGameState;
    }
    
    private void OnDisable()
    {
        advanceGameStateAction.action.performed -= AdvanceGameState;
        reverseGameStateAction.action.performed -= ReverseGameState;
    }
    
    private void FixedUpdate()
    {
        if ((gameState & GameState.SIMULATING) == 0)
        {
            return;
        }
        
        SmartVector3 movement = cameraController.GetCameraRelativeMoveVector(moveInput) * moveAcceleration;
        
        // Set the animator's panic value based on the time to impact
        animator.SetFloat(animPanicID, Mathf.Max(
            panicAtImpactTime.Evaluate(impactTimeEstimator.Estimate()) * panicMultAtSpeed.Evaluate(ragdollHelper.AverageLinearVelocity.Magnitude),
            panicAtAngularVelocity.Evaluate(ragdollHelper.AverageAngularVelocity.Magnitude)));
        
        // Grab the last frame's drag data
        bool shouldApplyDrag = dragCamera.TryUpdateSurfaceAreas(boneSurfaceAreas);
        
        // Grab last frame's exit direction data
        if (!exitDirectionFinder.MainJob.IsCompleted)
        {
            double startTime = Time.realtimeSinceStartupAsDouble;
            exitDirectionFinder.MainJob.Complete();
            float timeDiffTruncated = (int)((Time.realtimeSinceStartupAsDouble - startTime) * 100) / 100.0f;
            if (timeDiffTruncated > 0.0f)
            {
                Debug.LogWarning($"{nameof(NeoclipCharacterController)}.{nameof(FixedUpdate)}(): Had to wait {timeDiffTruncated}ms for exitDirectionJob to complete!");
            }
        }
        else
        {
            exitDirectionFinder.MainJob.Complete();
        }
        SmartVector3 exitDirection = exitDirectionFinder.GetExitDirection();

        // Test all bones to see if they're actively clipping
        bool anyBoneClipping = false;
        bool anyBoneRayHit = false;
        bool allBonesClipping = true;
        if (desiredNoclipState || noclipBufferFrames > 0)
        {
            for (int i = 0; i < ragdollHelper.NumBones; i++)
            {
                boneClipStates[i] = ClippingUtils.CheckColliderOrCastRaysDetailed(ragdollHelper.GetCollider(i));
                anyBoneClipping = anyBoneClipping || (boneClipStates[i] & ClippingUtils.ClipState.IsClipping) > 0;
                anyBoneRayHit = anyBoneRayHit || (boneClipStates[i] & ClippingUtils.ClipState.RayBackfaceMask) > 0;
                allBonesClipping = allBonesClipping && (boneClipStates[i] & ClippingUtils.ClipState.IsClipping) > 0;
            }
        }
        
        //Debug.Log(boneClipStates[0]);
        
        // Determine if we're "skating" along the ground, which we don't want
        float antiSkateVelocityDot = Vector3.Dot(ragdollHelper.AverageLinearVelocity, -Physics.gravity.normalized);
        bool antiSkate = !desiredNoclipState && anyBoneClipping && !anyBoneRayHit && !allBonesClipping && antiSkateVelocityDot > 0.0f && antiSkateVelocityDot < antiSkateVelocityThreshold;
        if (antiSkate)
        {
            Debug.Log($"{nameof(NeoclipCharacterController)}.{nameof(FixedUpdate)}: Anti-skating mechanism triggered.");
            anyBoneClipping = false;
            anyBoneRayHit = false;
            allBonesClipping = false;
            for (int i = 0; i < ragdollHelper.NumBones; i++)
            {
                boneClipStates[i] = ClippingUtils.ClipState.None;
            }
            noclipBufferFrames = 0;
        }
        
        // Fire events
        if (noclipBufferFrames == 0)
        {
            if (desiredNoclipState && !IsClipping) // If we want to noclip and we weren't already noclipping
            {
                Debug.Log($"{nameof(NeoclipCharacterController)}.{nameof(FixedUpdate)}: {nameof(OnNoclipStarted)}");
                OnNoclipStarted?.Invoke();
                IsClipping = true;
            }
            else if (!desiredNoclipState && IsClipping) // If we want to stop noclipping and the timer has run out
            {
                Debug.Log($"{nameof(NeoclipCharacterController)}.{nameof(FixedUpdate)}: {nameof(OnNoclipStopped)}");
                OnNoclipStopped?.Invoke();
                IsClipping = false;
            }
        }
        
        // Calculate the exit direction for next frame if necessary
        if (anyBoneClipping && desiredNoclipState && clippingForces.exitDirection.enabled ||
            anyBoneClipping && !desiredNoclipState && ejectingForces.exitDirection.enabled ||
            !anyBoneClipping && fallingForces.exitDirection.enabled)
        {
            exitDirectionFinder.transform.position = ragdollHelper.AveragePosition;
            exitDirectionFinder.ScheduleJobs();
        }
        else
        {
            exitDirectionFinder.ResetExitDirection();
        }
        
        // Iterate through all bones
        for (int i = 0; i < ragdollHelper.NumBones; i++)
        {
            Rigidbody rigidbody = ragdollHelper.GetRigidbody(i);
               
            ForcesSO forces = (boneClipStates[i] & ClippingUtils.ClipState.IsClipping) == 0
                ? fallingForces // This bone is in open space
                : desiredNoclipState
                    ? clippingForces // This bone is clipping (and the button is held down)
                    : ejectingForces; // This bone is clipping (but the button isn't held down)
            
            forces.ApplyAllForces(rigidbody, movement, exitDirection);
                        
            if (shouldApplyDrag)
            {
                Vector3 projectedVelocity = Vector3.Project(rigidbody.linearVelocity, ragdollHelper.AverageLinearVelocity.Normalized);
                
                rigidbody.AddForce(
                    0.5f * 
                    forces.GetDensity() *
                    projectedVelocity.sqrMagnitude * 
                    0.7f *
                    boneSurfaceAreas[i] * 
                    -ragdollHelper.AverageLinearVelocity.Normalized,
                    ForceMode.Force);
            }
            
            if (angularSlowdown > 0.0f)
            {
                rigidbody.angularVelocity = Vector3.RotateTowards(rigidbody.angularVelocity, Vector3.zero, 0.0f, angularSlowdown);
            }
        }
        
        // If we're trying to noclip, or already were, then reset the timer
        if (desiredNoclipState || anyBoneClipping)
        {
            noclipBufferFrames = Mathf.CeilToInt(secondsToStayClipping / Time.fixedDeltaTime);
        }
        else if (noclipBufferFrames > 0)
        {
            noclipBufferFrames--;
        }
    }
}

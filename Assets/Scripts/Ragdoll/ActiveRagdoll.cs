using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ActiveRagdoll : MonoBehaviour
{
    [SerializeField] private Transform driverSkeleton;
    [SerializeField] private Transform ragdollSkeleton;
    [SerializeField] private Transform customFramerateSkeleton;

    [SerializeField] private GameObject ragdollModel;
    [SerializeField] private GameObject customFramerateModel;

    [SerializeField] private bool useCustomFramerate = false;
    [SerializeField] private float customFramerate = 12;
    private float customFramerateTime = 0.0f;
    private BonePairPositionAndRotation customFramerateBase;
    
    private List<BonePairJoint> jointBonePairs;
    private List<BonePairRotationOnly> extremityBonePairs;
    private List<BonePairPositionAndRotation> customFramerateBonePairs;
    
    private void MakeBonePairsRecursive(Transform driverTransform, Transform ragdollTransform, Transform customFramerateTransform)
    {
        if (ragdollTransform.TryGetComponent(out ConfigurableJoint joint))
        {
            // If the ragdoll has a joint, add it as a joint pair
            jointBonePairs.Add(new BonePairJoint(driverTransform, joint));
            // The customframerate skeleton needs to use the ragdoll's values in this case, not the driver skeleton's
            customFramerateBonePairs.Add(new BonePairPositionAndRotation(ragdollTransform, customFramerateTransform));
        }
        else
        {
            // If there are no rigidbodies in the ragdoll bone's children, it must be an extremity
            // So set the ragdoll bone's values directly
            if (ragdollTransform.GetComponentInChildren<Rigidbody>() == null)
            {
                extremityBonePairs.Add(new BonePairRotationOnly(driverTransform, ragdollTransform));
            }
            // The customframerate skeleton also needs to reference these bones
            customFramerateBonePairs.Add(new BonePairPositionAndRotation(driverTransform, customFramerateTransform));
        }
        
        // Do a recursion
        int childCount = driverTransform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            MakeBonePairsRecursive(driverTransform.GetChild(i), ragdollTransform.GetChild(i), customFramerateTransform.GetChild(i));
        }
    }
    
    private void Awake()
    {
        jointBonePairs = new List<BonePairJoint>();
        extremityBonePairs = new List<BonePairRotationOnly>();
        customFramerateBonePairs = new List<BonePairPositionAndRotation>();

        MakeBonePairsRecursive(driverSkeleton, ragdollSkeleton, customFramerateSkeleton);

        // The first bone pair for customframerate will be the hips of the DRIVER skeleton, because the ragdoll's hip bone
        // doesn't have a joint. We're moving the hips separately anyway, so simply delete it.
        customFramerateBonePairs.RemoveAt(0);
        // Since the customframerate skeleton updates its base postion and rotation smoothly, keep track of that separately
        customFramerateBase = new BonePairPositionAndRotation(ragdollSkeleton, customFramerateSkeleton);
    }
    
    private void LateUpdate()
    {
        ragdollModel.SetActive(!useCustomFramerate);
        customFramerateModel.SetActive(useCustomFramerate);
        
        if (useCustomFramerate)
        {
            customFramerateBase.WriteValues();
            customFramerateTime -= Time.deltaTime;
            
            if (customFramerateTime <= 0)
            {
                foreach (BonePairPositionAndRotation customFramerateBonePair in customFramerateBonePairs)
                {
                    customFramerateBonePair.WriteValues();
                }
                customFramerateTime = 1.0f / customFramerate;
            }
        }
        else
        {
            foreach (BonePairRotationOnly extremityBonePair in extremityBonePairs)
            {
                extremityBonePair.WriteValues();
            }
        }
    }
    
    private void FixedUpdate()
    {
        foreach (BonePairJoint jointBonePair in jointBonePairs)
        {
            jointBonePair.WriteValues();
        }
    }
}

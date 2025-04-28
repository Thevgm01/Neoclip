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
    private PositionAndRotationBonePair customFrameRateBaseBonePair;
    
    private List<JointBonePair> jointBonePairs;
    private List<RotationBonePair> extremityBonePairs;
    private List<PositionAndRotationBonePair> customFramerateBonePairs;
    
    private void BuildBonePairTreeRecursive(Transform driverTransform, Transform ragdollTransform, Transform customFramerateTransform)
    {
        if (ragdollTransform.TryGetComponent(out ConfigurableJoint joint))
        {
            jointBonePairs.Add(new JointBonePair(driverTransform, ragdollTransform, joint));
            
            customFramerateBonePairs.Add(new PositionAndRotationBonePair(ragdollTransform, customFramerateTransform));
        }
        else
        {
            if (ragdollTransform.GetComponentInChildren<Rigidbody>() == null)
            {
                extremityBonePairs.Add(new RotationBonePair(driverTransform, ragdollTransform));
            }
            customFramerateBonePairs.Add(new PositionAndRotationBonePair(driverTransform, customFramerateTransform));
        }
        
        int childCount = driverTransform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            BuildBonePairTreeRecursive(driverTransform.GetChild(i), ragdollTransform.GetChild(i), customFramerateTransform.GetChild(i));
        }
    }
    
    private void Awake()
    {
        jointBonePairs = new List<JointBonePair>();
        extremityBonePairs = new List<RotationBonePair>();
        customFramerateBonePairs = new List<PositionAndRotationBonePair>();

        BuildBonePairTreeRecursive(driverSkeleton, ragdollSkeleton, customFramerateSkeleton);

        customFramerateBonePairs.RemoveAt(0);
        customFrameRateBaseBonePair = new PositionAndRotationBonePair(ragdollSkeleton, customFramerateSkeleton);
    }
    
    private void LateUpdate()
    {
        ragdollModel.SetActive(!useCustomFramerate);
        customFramerateModel.SetActive(useCustomFramerate);
        
        if (useCustomFramerate)
        {
            customFrameRateBaseBonePair.WriteValues();
            customFramerateTime -= Time.deltaTime;
            
            if (customFramerateTime <= 0)
            {
                foreach (PositionAndRotationBonePair customFramerateBonePair in customFramerateBonePairs)
                {
                    customFramerateBonePair.WriteValues();
                }
                customFramerateTime = 1.0f / customFramerate;
            }
        }
        else
        {
            foreach (RotationBonePair extremityBonePair in extremityBonePairs)
            {
                extremityBonePair.WriteValues();
            }
        }
    }
    
    private void FixedUpdate()
    {
        foreach (JointBonePair jointBonePair in jointBonePairs)
        {
            jointBonePair.WriteValues();
        }
    }
}

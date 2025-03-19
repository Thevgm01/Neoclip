using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ConcaveClipHelper : NeoclipCharacterComponent
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private LayerMask layerMask;
    
    private readonly Vector3[] directions =
    {
        Vector3.up,
        Vector3.forward, Vector3.right, Vector3.back, Vector3.left,
        Vector3.down
    };

    private QueryParameters standardQuery;
    private QueryParameters backfaceQuery;
    
    public override void Init()
    {
        standardQuery.layerMask = layerMask.value;
        standardQuery.hitBackfaces = false;
        
        backfaceQuery.layerMask = layerMask.value;
        backfaceQuery.hitBackfaces = true;
    }

    private bool CheckBoxOrAddCommands(BoxCollider boxCollider, List<BoxcastCommand> boxcastCommands)
    {
        Transform boxTransform = boxCollider.transform;

        BoxcastCommand boxcastCommand = new BoxcastCommand(
            boxTransform.TransformPoint(boxCollider.center),
            boxCollider.size / 2.0f,
            boxTransform.rotation,
            directions[0],
            standardQuery);
            
        if (Physics.CheckBox(
                boxcastCommand.center,
                boxcastCommand.halfExtents,
                boxcastCommand.orientation,
                layerMask.value))
        {
            return true;
        }

        for (int j = 0; j < directions.Length - 1; j++)
        {
            boxcastCommands.Add(boxcastCommand);

            boxcastCommand.queryParameters = backfaceQuery;
            boxcastCommands.Add(boxcastCommand);
                
            boxcastCommand.queryParameters = standardQuery;
            boxcastCommand.direction = directions[j + 1];
        }
        
        return false;
    }

    private bool CheckCapsuleOrAddCommands(CapsuleCollider capsuleCollider, List<CapsulecastCommand> capsulecastCommands)
    {
        Transform capsuleTransform = capsuleCollider.transform;
            
        Vector3 origin = capsuleTransform.TransformPoint(capsuleCollider.center);
        Vector3 axis = capsuleTransform.TransformDirection(capsuleCollider.GetAxis() * capsuleCollider.height / 2.0f);
        
        CapsulecastCommand capsuleCommand = new CapsulecastCommand(
            origin + axis,
            origin - axis,
            capsuleCollider.radius,
            directions[0],
            standardQuery);
            
        if (Physics.CheckCapsule(
                capsuleCommand.point1,
                capsuleCommand.point1,
                capsuleCommand.radius,
                layerMask.value))
        {
            return true;
        }

        for (int j = 0; j < directions.Length - 1; j++)
        {
            capsulecastCommands.Add(capsuleCommand);

            capsuleCommand.queryParameters = backfaceQuery;
            capsulecastCommands.Add(capsuleCommand);
                
            capsuleCommand.queryParameters = standardQuery;
            capsuleCommand.direction = directions[j + 1];
        }
        
        return false;
    }
    
    private bool CheckSphereOrAddCommands(SphereCollider sphereCollider, List<SpherecastCommand> spherecastCommands)
    {
        SpherecastCommand sphereCommand = new SpherecastCommand(
            sphereCollider.transform.TransformPoint(sphereCollider.center),
            sphereCollider.radius,
            directions[0],
            standardQuery);
            
        if (Physics.CheckSphere(
                sphereCommand.origin,
                sphereCommand.radius,
                layerMask.value))
        {
            return true;
        }

        for (int j = 0; j < directions.Length - 1; j++)
        {
            spherecastCommands.Add(sphereCommand);

            sphereCommand.queryParameters = backfaceQuery;
            spherecastCommands.Add(sphereCommand);
                
            sphereCommand.queryParameters = standardQuery;
            sphereCommand.direction = directions[j + 1];
        }
        
        return false;
    }
    
    public bool CheckAllBones(bool[] results)
    {
        bool anythingInside = false;
        
        List<BoxcastCommand> boxcastCommandList = new List<BoxcastCommand>();
        NativeArray<BoxcastCommand> boxcastCommands = new NativeArray<BoxcastCommand>();
        NativeArray<RaycastHit> boxcastHits = new NativeArray<RaycastHit>();
        List<int> boxcastHitToBoneIndex = new List<int>();
        JobHandle boxJob = new JobHandle();

        List<CapsulecastCommand> capsulecastCommandList = new List<CapsulecastCommand>();
        NativeArray<CapsulecastCommand> capsulecastCommands = new NativeArray<CapsulecastCommand>();
        NativeArray<RaycastHit> capsulecastHits = new NativeArray<RaycastHit>();
        List<int> capsulecastHitToBoneIndex = new List<int>();
        JobHandle capsuleJob = new JobHandle();

        List<SpherecastCommand> spherecastCommandList = new List<SpherecastCommand>();
        NativeArray<SpherecastCommand> spherecastCommands = new NativeArray<SpherecastCommand>();
        NativeArray<RaycastHit> spherecastHits = new NativeArray<RaycastHit>();
        List<int> spherecastHitToBoneIndex = new List<int>();
        JobHandle sphereJob = new JobHandle();
        
        // Check all box colliders
        for (int i = 0; i < ragdollAverages.NumBoxColliders; i++)
        {
            if (CheckBoxOrAddCommands(ragdollAverages.GetBoxCollider(i), boxcastCommandList))
            {
                anythingInside = true;
                results[ragdollAverages.BoneIndexOfBoxCollider(i)] = true;
            }
            else
            {
                boxcastHitToBoneIndex.Add(i);
            }
        }

        // Schedule the box collider batch
        if (boxcastCommandList.Count > 0)
        {
            boxcastCommands = boxcastCommandList.ToNativeArray(Allocator.Temp);
            boxcastHits = new NativeArray<RaycastHit>(boxcastCommandList.Count, Allocator.Temp);
            boxJob = BoxcastCommand.ScheduleBatch(
                boxcastCommands, 
                boxcastHits,  
                1, 1);
        }
        
        // Check all capsule colliders
        for (int i = 0; i < ragdollAverages.NumCapsuleColliders; i++)
        {
            if (CheckCapsuleOrAddCommands(ragdollAverages.GetCapsuleCollider(i), capsulecastCommandList))
            {
                anythingInside = true;
                results[ragdollAverages.BoneIndexOfCapsuleCollider(i)] = true;
            }
            else
            {
                capsulecastHitToBoneIndex.Add(i);
            }
        }

        // Schedule the capsule collider batch
        if (capsulecastCommandList.Count > 0)
        {
            capsulecastCommands = capsulecastCommandList.ToNativeArray(Allocator.Temp);
            capsulecastHits = new NativeArray<RaycastHit>(capsulecastCommandList.Count, Allocator.Temp);
            capsuleJob = CapsulecastCommand.ScheduleBatch(
                capsulecastCommands,
                capsulecastHits,
                1, 1);
        }
        
        // Check all sphere colliders
        for (int i = 0; i < ragdollAverages.NumSphereColliders; i++)
        {
            if (CheckSphereOrAddCommands(ragdollAverages.GetSphereCollider(i), spherecastCommandList))
            {
                anythingInside = true;
                results[ragdollAverages.BoneIndexOfSphereCollider(i)] = true;
            }
            else
            {
                spherecastHitToBoneIndex.Add(i);
            }
        }
        
        // Schedule the sphere collider batch
        if (spherecastCommandList.Count > 0)
        {
            spherecastCommands = spherecastCommandList.ToNativeArray(Allocator.Temp);
            spherecastHits = new NativeArray<RaycastHit>(spherecastCommandList.Count, Allocator.Temp);
            sphereJob = SpherecastCommand.ScheduleBatch(
                spherecastCommands,
                spherecastHits,
                1, 1);
        }

        boxJob.Complete();
        for (int i = 0; i < boxcastHits.Length; i += 2)
        {
            bool isInside = (boxcastHits[i].point - boxcastHits[i + 1].point).sqrMagnitude >= 0.01f;
            results[boxcastHitToBoneIndex[i / (directions.Length * 2)]] = isInside;
            anythingInside = anythingInside || isInside;
        }
        boxcastCommands.Dispose();
        boxcastHits.Dispose();

        capsuleJob.Complete();
        for (int i = 0; i < capsulecastHits.Length; i += 2)
        {
            bool isInside = (capsulecastHits[i].point - capsulecastHits[i + 1].point).sqrMagnitude >= 0.01f;
            results[capsulecastHitToBoneIndex[i / (directions.Length * 2)]] = isInside;
            anythingInside = anythingInside || isInside;
        }
        capsulecastCommands.Dispose();
        capsulecastHits.Dispose();

        sphereJob.Complete();
        for (int i = 0; i < spherecastHits.Length; i += 2)
        {
            bool isInside = (spherecastHits[i].point - spherecastHits[i + 1].point).sqrMagnitude >= 0.01f;
            results[spherecastHitToBoneIndex[i / (directions.Length * 2)]] = isInside;
            anythingInside = anythingInside || isInside;
        }
        spherecastCommands.Dispose();
        spherecastHits.Dispose();

        return anythingInside;
    }
}

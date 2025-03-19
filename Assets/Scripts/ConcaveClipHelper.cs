using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ConcaveClipHelper : NeoclipCharacterComponent
{
    [SerializeField] private RagdollAverages ragdollAverages;
    [SerializeField] private LayerMask castMask;
    
    private readonly Vector3[] directions =
    {
        Vector3.left, Vector3.right,
        Vector3.down, Vector3.up,
        Vector3.back, Vector3.forward
    };

    private QueryParameters parametersA;
    private QueryParameters parametersB;
    
    public override void Init()
    {
        parametersA = new QueryParameters(castMask.value, hitMultipleFaces: false, hitBackfaces: false);
        parametersB = new QueryParameters(castMask.value, hitMultipleFaces: false, hitBackfaces: true);
    }
    
    public bool IsInsideSomething()
    {
        var results = new NativeArray<RaycastHit>(12, Allocator.TempJob);
        var commands = new NativeArray<SpherecastCommand>(12, Allocator.TempJob);

        SphereCollider sphereCollider = ragdollAverages.GetSphereCollider(0);
        for (int i = 0; i < 6; i++)
        {
            commands[i * 2 + 0] = sphereCollider.ToCommand(directions[i], parametersA);
            commands[i * 2 + 1] = sphereCollider.ToCommand(directions[i], parametersB);
        }
        
        JobHandle handle = SpherecastCommand.ScheduleBatch(commands, results, 1, 1, default(JobHandle));

        handle.Complete();

        Vector3[] temp = new Vector3[18];
        bool inside = false;
        
        for (int i = 0; i < 6; i++)
        {
            temp[i * 3 + 0] = results[i * 2 + 0].point;
            temp[i * 3 + 1] = results[i * 2 + 1].point;
            temp[i * 3 + 2] = results[i * 2 + 1].point - results[i * 2 + 0].point;
            inside = inside || results[i * 2 + 0].point != results[i * 2 + 1].point;
        }
        
        Debug.Log(string.Join(", ", temp));

        // Dispose the buffers
        results.Dispose();
        commands.Dispose();

        return inside;
    }
}

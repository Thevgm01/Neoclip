using UnityEngine;

public class BoundsVisualizer : MonoBehaviour
{
    void OnDrawGizmosSelected()
    {
        Bounds bounds = GetComponent<MeshFilter>().sharedMesh.bounds;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + bounds.center * transform.lossyScale.x, 0.1f);
        Gizmos.DrawWireCube(transform.position + bounds.center * transform.lossyScale.x, bounds.size * transform.lossyScale.x);
    }
}

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform followTarget = null;

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, followTarget.position + new Vector3(0.0f, 0.0f, 3.0f), 1.0f - Mathf.Exp(-15.0f * Time.deltaTime));
    }
}

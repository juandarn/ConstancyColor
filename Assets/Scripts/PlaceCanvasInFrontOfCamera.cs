using UnityEngine;

public class PlaceCanvasInFrontOfCamera : MonoBehaviour
{
    public Transform cameraTransform;   // arrastra Main Camera
    public float distance = 1.2f, heightOffset = -0.1f;
    public bool yawOnly = true;

    void OnEnable()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        if (!cameraTransform) return;

        var fwd = yawOnly ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                          : cameraTransform.forward;
        var pos = cameraTransform.position + fwd * distance + Vector3.up * heightOffset;
        transform.position = pos;

        var look = cameraTransform.position - pos;
        if (yawOnly) look.y = 0;
        if (look.sqrMagnitude < 1e-6f) look = cameraTransform.forward;
        transform.rotation = Quaternion.LookRotation(-look.normalized, Vector3.up);
    }
}

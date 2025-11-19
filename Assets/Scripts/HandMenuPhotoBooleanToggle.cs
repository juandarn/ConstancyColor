using UnityEngine;
using UnityEngine.UI;

public class HandMenuPhotoBooleanToggle : MonoBehaviour
{
    [Header("Objetivo")]
    public GameObject photoCanvas;   // Canvas de la foto (World Space)
    public RawImage photoImage;      // RawImage dentro del canvas (opcional pero útil)
    public Texture2D picture;        // Tu textura (opcional si ya la asignaste)

    [Header("Frente a la cámara")]
    public Transform cameraTransform;     // XR Origin/Camera Offset/Main Camera
    public float distance = 1.2f;
    public float heightOffset = -0.10f;
    public float heightMeters = 0.8f;     // alto visible (m)
    public bool yawOnly = true;

    [Header("Estado")]
    [SerializeField] bool isShown = false; // se invierte en cada apertura

    void OnEnable() // se llama cada vez que SE ABRE el Hand Menu (Follow GameObject se activa)
    {
        isShown = !isShown; // <<--- BOLEANO: alterna

        if (!photoCanvas) return;

        if (isShown)
        {
            // tamaño / textura
            if (photoImage && picture) photoImage.texture = picture;
            if (photoImage && photoImage.texture)
            {
                var tex = (Texture2D)photoImage.texture;
                float aspect = (float)tex.width / Mathf.Max(1, tex.height);
                float hPx = heightMeters * 1000f, wPx = hPx * aspect; // 1000px = 1m (Canvas scale 0.001)
                var rt = photoImage.rectTransform;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hPx);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, wPx);
                rt.anchoredPosition = Vector2.zero;
            }

            PlaceInFront();
            photoCanvas.SetActive(true);
        }
        else
        {
            photoCanvas.SetActive(false);
        }
    }

    void PlaceInFront()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        if (!cameraTransform) return;

        Vector3 fwd = cameraTransform.forward;
        if (yawOnly) fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;

        Vector3 pos = cameraTransform.position + fwd * distance + Vector3.up * heightOffset;
        photoCanvas.transform.position = pos;

        Vector3 look = cameraTransform.position - pos;
        if (yawOnly) look.y = 0f;
        if (look.sqrMagnitude < 1e-4f) look = cameraTransform.forward;
        photoCanvas.transform.rotation = Quaternion.LookRotation(-look.normalized, Vector3.up);
    }
}

using UnityEngine;

public class IntroRulesFlow : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTransform;   // XR Origin/Camera Offset/Main Camera
    public Canvas rulesCanvas;          // RulesCanvas (World Space)
    public GameObject mainPanelRoot;    // Tu panel/menú raíz (se activa al continuar)

    [Header("Colocación")]
    public float distance = 1.2f;       // metros delante
    public float heightOffset = -0.10f; // un pelín bajo
    public bool yawOnly = true;         // seguir solo en horizontal (recomendado VR)

    [Header("Seguimiento en vivo")]
    public bool followWhileShowing = true; // <- ACTÍVALO en el Inspector
    public float positionLerp = 12f;       // suavizado (mayor = más rápido)
    public float rotationLerp = 18f;

    [Header("Auto-continue (0 = off)")]
    public float autoContinueSeconds = 0f;

    bool showing;

    void Awake()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    void Start() { ShowRules(); }

    void LateUpdate()
    {
        if (showing && followWhileShowing && rulesCanvas && rulesCanvas.gameObject.activeSelf)
            SmoothFollow(rulesCanvas.transform);
    }

    // --- Público ---
    public void ShowRules()
    {
        if (mainPanelRoot) mainPanelRoot.SetActive(false);
        if (rulesCanvas)
        {
            SnapPlace(rulesCanvas.transform);
            rulesCanvas.gameObject.SetActive(true);
        }
        showing = true;

        if (autoContinueSeconds > 0f) Invoke(nameof(Continue), autoContinueSeconds);
    }

    public void Continue()
    {
        if (!showing) return;
        if (rulesCanvas) rulesCanvas.gameObject.SetActive(false);
        if (mainPanelRoot) mainPanelRoot.SetActive(true);
        showing = false;
    }

    // --- Colocación ---
    void GetTarget(out Vector3 pos, out Quaternion rot)
    {
        pos = rulesCanvas.transform.position;
        rot = rulesCanvas.transform.rotation;
        if (!cameraTransform) return;

        var fwd = cameraTransform.forward;
        if (yawOnly) fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;

        var desiredPos = cameraTransform.position + fwd * distance + Vector3.up * heightOffset;
        var toCam = cameraTransform.position - desiredPos;
        if (yawOnly) toCam.y = 0f;
        if (toCam.sqrMagnitude < 1e-6f) toCam = cameraTransform.forward;

        pos = desiredPos;
        rot = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
    }

    void SnapPlace(Transform t)
    {
        GetTarget(out var p, out var r);
        t.position = p;
        t.rotation = r;
    }

    void SmoothFollow(Transform t)
    {
        GetTarget(out var p, out var r);
        t.position = Vector3.Lerp(t.position, p, 1f - Mathf.Exp(-positionLerp * Time.deltaTime));
        t.rotation = Quaternion.Slerp(t.rotation, r, 1f - Mathf.Exp(-rotationLerp * Time.deltaTime));
    }
}

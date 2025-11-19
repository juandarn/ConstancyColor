using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class BoxColliderMatchRect : MonoBehaviour
{
    public RectTransform rect;           // el RectTransform del panel (por ej. Background Panel)
    public float topHeightMeters = 0.05f; // alto de la barra de agarre (en metros)
    public float zThickness = 0.01f;      // grosor del collider hacia cámara

    void LateUpdate()
    {
        if (!rect) return;

        // Obtén las esquinas en mundo del rect
        var w = new Vector3[4];
        rect.GetWorldCorners(w); // 0=BL 1=TL 2=TR 3=BR

        float width = Vector3.Distance(w[1], w[2]);
        float height = Vector3.Distance(w[0], w[1]);
        float h = Mathf.Min(topHeightMeters, height);

        // Dirección "adelante" del canvas
        var up = (w[1] - w[0]).normalized;
        var right = (w[2] - w[1]).normalized;
        var forward = Vector3.Cross(right, up).normalized;

        // Centro de la franja superior
        var topCenter = (w[1] + w[2]) * 0.5f - up * (h * 0.5f);

        // Coloca/rota este GO (el del BoxCollider) en la misma plana del canvas
        transform.position = topCenter + forward * (zThickness * 0.5f);
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        // Ajusta el BoxCollider
        var col = GetComponent<BoxCollider>();
        col.size = new Vector3(width, h, zThickness);
        col.center = Vector3.zero; // ya posicionamos el transform en el centro del collider
    }
}

using UnityEngine;
using UnityEngine.UI;

public class PhotoPopupController : MonoBehaviour
{
    [Header("Refs")]
    public Canvas photoCanvas;        // PhotoCanvas
    public RawImage photoImage;       // RawImage de la foto
    public Transform cameraTransform; // Main Camera / XR Camera

    [Header("Botón que controla todo")]
    public Text buttonLabel;          // Texto del botón (UGUI)
    public string labelVer = "Ver foto";
    public string labelSiguiente = "Siguiente";
    public string labelCerrar = "Cerrar";

    [Header("Fotos")]
    public Texture2D[] pictures;      // Array de fotos (3 o las que quieras)
    public float heightMeters = 0.8f; // alto en metros aprox (Canvas con scale 0.001)

    [Header("Posicion frente a la camara")]
    public float distance = 1.2f;
    public float heightOffset = -0.10f;
    public bool yawOnly = true;

    int currentIndex = 0;
    bool isOpen = false;

    void Awake()
    {
        if (!cameraTransform && Camera.main)
            cameraTransform = Camera.main.transform;

        if (photoCanvas)
            photoCanvas.gameObject.SetActive(false);

        SetButtonLabel(labelVer);
    }

    void SetButtonLabel(string text)
    {
        if (buttonLabel)
            buttonLabel.text = text;
    }

    // Este es el método que debe llamar tu botón
    public void OnButtonPressed()
    {
        if (!photoCanvas) return;

        // Caso 1: está cerrado -> abrir y mostrar primera foto
        if (!isOpen)
        {
            OpenAndShowFirst();
        }
        else
        {
            // Caso 2: está abierto
            if (pictures == null || pictures.Length == 0)
            {
                CloseAndReset();
                return;
            }

            // Si NO estamos en la última foto -> pasar a la siguiente
            if (currentIndex < pictures.Length - 1)
            {
                currentIndex++;
                ApplyCurrentPhoto();

                // Si ahora estamos en la última -> cambiar texto a "Cerrar"
                if (currentIndex == pictures.Length - 1)
                    SetButtonLabel(labelCerrar);
                else
                    SetButtonLabel(labelSiguiente);
            }
            else
            {
                // Ya estábamos en la última y el botón dice "Cerrar"
                CloseAndReset();
            }
        }
    }

    void OpenAndShowFirst()
    {
        if (pictures != null && pictures.Length > 0)
        {
            currentIndex = 0;
            ApplyCurrentPhoto();

            // Si solo hay una foto, el siguiente estado lógico es cerrar
            if (pictures.Length == 1)
                SetButtonLabel(labelCerrar);
            else
                SetButtonLabel(labelSiguiente);
        }
        else
        {
            // No hay fotos, nada que mostrar
            return;
        }

        PositionInFront();
        photoCanvas.gameObject.SetActive(true);
        isOpen = true;
    }

    void CloseAndReset()
    {
        photoCanvas.gameObject.SetActive(false);
        isOpen = false;
        currentIndex = 0;
        SetButtonLabel(labelVer);
    }

    void ApplyCurrentPhoto()
    {
        if (pictures == null || pictures.Length == 0 || !photoImage)
            return;

        Texture2D t = pictures[currentIndex];
        if (!t) return;

        photoImage.texture = t;

        // Ajustar tamaño segun proporción
        float aspect = (float)t.width / Mathf.Max(1, t.height);
        float h = heightMeters * 1000f;   // Canvas con escala 0.001 => 1000px 1m
        float w = h * aspect;

        RectTransform rt = photoImage.rectTransform;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        rt.anchoredPosition = Vector2.zero;
    }

    void PositionInFront()
    {
        if (!cameraTransform || !photoCanvas) return;

        Vector3 fwd = cameraTransform.forward;
        if (yawOnly)
            fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;

        Vector3 pos = cameraTransform.position + fwd * distance + Vector3.up * heightOffset;
        photoCanvas.transform.position = pos;

        Vector3 look = cameraTransform.position - pos;
        if (yawOnly) look.y = 0f;
        if (look.sqrMagnitude < 1e-4f) look = cameraTransform.forward;

        photoCanvas.transform.rotation = Quaternion.LookRotation(-look.normalized, Vector3.up);
    }
}

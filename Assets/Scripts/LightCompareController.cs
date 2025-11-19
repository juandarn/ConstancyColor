using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System;
using System.Collections.Generic;

public class LightCompareController : MonoBehaviour
{
    // ===== Camara / Overlay =====
    [Header("Camara / Overlay")]
    public Transform cameraTransform;
    public GameObject mainPanelRoot;
    public Canvas compareCanvas;
    public TMP_Text compareText;

    [Header("Colocacion")]
    public float distance = 1.2f;
    public float heightOffset = -0.10f;
    public bool yawOnly = true;

    [Header("Seguimiento en vivo")]
    public bool followWhileShowing = true;
    public float positionLerp = 12f;
    public float rotationLerp = 18f;

    [Header("Auto-cerrar (0 = no)")]
    public float autoCloseSeconds = 4f;

    // ===== Logica de comparacion =====
    [Header("Luces / Texto opcional")]
    public LightUIController lightsUI;
    public TMP_Text resultText;

    [Header("Boton unico (etiqueta flexible)")]
    public TMP_Text compareButtonLabelTMP;
    public Text compareButtonLabelUGUI;
    public string labelGuardar = "Guardar objetivo";
    public string labelComparar = "Comparar";

    [Header("Escala/normalizacion")]
    public float maxIntensityForScore = 10f;   // IMPORTANTE: 10 porque tienes una luz a 10
    public float kelvinNormRange = 3000f;      // p.ej. diferencia 0-3000K

    [Header("Pesos para puntaje")]
    [Range(0, 1)] public float wColor = 0.5f;
    [Range(0, 1)] public float wIntensity = 0.3f;
    [Range(0, 1)] public float wKelvin = 0.2f;

    [Header("Ganador")]
    [Range(0, 100)] public float winThreshold = 85f;

    // ===== Estado en vivo =====
    [Header("Estado en vivo")]
    public TMP_Text statusText;
    public bool showPerLamp = true;
    public float statusRefreshHz = 5f;

    // ===== Logging comparaciones =====
    [Header("Logging comparaciones")]
    public bool logCompareToConsole = true;
    public bool keepCompareHistory = true;
    [TextArea(3, 10)]
    public List<string> compareHistory = new List<string>();

    bool hasTarget = true;   // usamos objetivos fijos, así que siempre true
    bool showing;

    void Awake()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        SetBtnLabel(labelComparar); // ya no necesitamos guardar un objetivo a mano
        if (compareCanvas) compareCanvas.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (statusRefreshHz <= 0f) statusRefreshHz = 5f;
        CancelInvoke(nameof(RefreshStatus));
        InvokeRepeating(nameof(RefreshStatus), 0f, 1f / statusRefreshHz);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(RefreshStatus));
    }

    void LateUpdate()
    {
        if (showing && followWhileShowing && compareCanvas && compareCanvas.gameObject.activeSelf)
            SmoothFollow(compareCanvas.transform);
    }

    // ========== Boton unico ==========
    // Ahora solo compara (ya tenemos objetivos fijos por lampara)
    public void SaveOrCompare()
    {
        CompareNow();
    }

    void SetBtnLabel(string s)
    {
        if (compareButtonLabelTMP) compareButtonLabelTMP.text = s;
        if (compareButtonLabelUGUI) compareButtonLabelUGUI.text = s;
    }

    // ========== Flujo principal ==========
    public void CompareNow()
    {
        if (!lightsUI || lightsUI.lights == null || lightsUI.lights.Length == 0)
        {
            ShowText("No hay luces configuradas para comparar.");
            return;
        }

        // Calcula score global en base a TODAS las lamparas + objetivos fijos
        float score01, sColor, sInt, sK;
        string perLampText = BuildPerLampDetailsAndScore(out score01, out sColor, out sInt, out sK);
        float score = Mathf.Round(score01 * 100f);

        string header = BuildFriendlyHeader(score, sColor, sInt, sK);
        string fullReport = header + "\n\n" + perLampText;

        ShowText(fullReport);
        ShowOverlay(fullReport);

        // Logs
        LogAllLamps("Comparar (estado actual del usuario)");
        LogComparison("Comparacion final", fullReport);
    }

    void ShowOverlay(string report)
    {
        if (mainPanelRoot) mainPanelRoot.SetActive(false);
        if (compareText) compareText.text = report;

        if (compareCanvas)
        {
            SnapPlace(compareCanvas.transform);
            compareCanvas.gameObject.SetActive(true);
        }

        showing = true;
        if (autoCloseSeconds > 0f) Invoke(nameof(CloseOverlay), autoCloseSeconds);
    }

    public void CloseOverlay()
    {
        if (!showing) return;
        if (compareCanvas) compareCanvas.gameObject.SetActive(false);
        if (mainPanelRoot) mainPanelRoot.SetActive(true);
        showing = false;
        CancelInvoke(nameof(CloseOverlay));
    }

    // ========== Colocacion/seguimiento ==========
    void GetTarget(out Vector3 pos, out Quaternion rot)
    {
        pos = compareCanvas ? compareCanvas.transform.position : Vector3.zero;
        rot = compareCanvas ? compareCanvas.transform.rotation : Quaternion.identity;
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

    // ========== Utilidades basicas ==========
    void ShowText(string s)
    {
        if (resultText) resultText.text = s;
        else Debug.Log(s);
    }

    Color KelvinToRGB(float kelvin)
    {
        float temp = kelvin / 100f;
        float r, g, b;

        if (temp <= 66f) r = 255f;
        else
        {
            r = temp - 60f;
            r = 329.698727446f * Mathf.Pow(r, -0.1332047592f);
            r = Mathf.Clamp(r, 0f, 255f);
        }

        if (temp <= 66f)
        {
            g = temp;
            g = 99.4708025861f * Mathf.Log(g) - 161.1195681661f;
            g = Mathf.Clamp(g, 0f, 255f);
        }
        else
        {
            g = temp - 60f;
            g = 288.1221695283f * Mathf.Pow(g, -0.0755148492f);
            g = Mathf.Clamp(g, 0f, 255f);
        }

        if (temp >= 66f) b = 255f;
        else if (temp <= 19f) b = 0f;
        else
        {
            b = temp - 10f;
            b = 138.5177312231f * Mathf.Log(b) - 305.0447927307f;
            b = Mathf.Clamp(b, 0f, 255f);
        }

        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    // ===== Panel de estado en vivo =====
    void RefreshStatus()
    {
        if (!statusText || !lightsUI || lightsUI.lights == null) return;

        var sb = new StringBuilder(256);
        int active = GetActiveIndex();

        if (active < 0) sb.AppendLine("<b>Luz seleccionada: Todas</b>");
        else
        {
            var l = (active < lightsUI.lights.Length) ? lightsUI.lights[active] : null;
            sb.AppendLine("<b>Luz seleccionada:</b> " + (l ? l.name : "N/D"));
        }

        if (showPerLamp)
        {
            for (int i = 0; i < lightsUI.lights.Length; i++)
            {
                var l = lightsUI.lights[i];
                if (!l) continue;
                sb.Append("<b>• ").Append(l.name).Append("</b>  ");
                AppendLampValues(sb, l);
                sb.AppendLine();
            }
        }
        else
        {
            var l = GetOneLight();
            sb.Append("<b>• ").Append(l ? l.name : "N/D").Append("</b>  ");
            AppendLampValues(sb, l);
        }

        statusText.text = sb.ToString();
    }

    void AppendLampValues(StringBuilder sb, Light l)
    {
        if (!l) { sb.Append("Sin luz."); return; }
        string hex = ColorUtility.ToHtmlStringRGB(l.color);
        sb.Append("Int: ").Append(l.intensity.ToString("0.00"))
          .Append("  Color: #").Append(hex);

        if (l.useColorTemperature)
            sb.Append("  K: ").Append(Mathf.RoundToInt(l.colorTemperature));
    }

    int GetActiveIndex()
    {
        if (!lightsUI) return -1;
        var f = typeof(LightUIController).GetField(
            "activeIndex",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return f != null ? (int)f.GetValue(lightsUI) : -1;
    }

    Light GetOneLight()
    {
        var arr = lightsUI != null ? lightsUI.lights : null;
        if (arr == null) return null;
        foreach (var l in arr) if (l) return l;
        return null;
    }

    // ===== SCORE GLOBAL basado en TODAS las lamparas =====
    string BuildFriendlyHeader(float score, float sColor, float sInt, float sK)
    {
        string nivel;
        if (score >= 90) nivel = "¡Excelente! Casi perfecto.";
        else if (score >= 75) nivel = "Muy bien, estás bastante cerca.";
        else if (score >= 50) nivel = "Aproximado, pero aún puedes ajustar.";
        else nivel = "Lejos del objetivo, prueba otra combinación.";

        return
            "RESULTADO GENERAL\n" +
            "Puntaje total: " + score.ToString("0") + "%\n" +
            "Coincidencia de color: " + Mathf.RoundToInt(sColor * 100) + "%\n" +
            "Coincidencia de intensidad: " + Mathf.RoundToInt(sInt * 100) + "%\n" +
            "Coincidencia de temperatura (K): " + Mathf.RoundToInt(sK * 100) + "%\n\n" +
            nivel;
    }

    string BuildPerLampDetailsAndScore(out float score01, out float sColorAvg, out float sIntAvg, out float sKAvg)
    {
        score01 = 0f;
        sColorAvg = 0f;
        sIntAvg = 0f;
        sKAvg = 0f;

        if (lightsUI == null || lightsUI.lights == null || lightsUI.lights.Length == 0)
            return "No hay luces configuradas.";

        var sb = new StringBuilder(512);
        sb.AppendLine("DETALLE POR LAMPARA");

        int count = 0;
        float sumColor = 0f, sumInt = 0f, sumK = 0f;

        for (int i = 0; i < lightsUI.lights.Length; i++)
        {
            var l = lightsUI.lights[i];
            if (!l) continue;

            string userHex = ColorUtility.ToHtmlStringRGB(l.color);
            float userK = l.useColorTemperature ? l.colorTemperature : -1f;
            float userInt = l.intensity;

            if (TryGetTargetForLight(l.name, out float tInt, out Color tColor, out float tKelvin))
            {
                string targetHex = ColorUtility.ToHtmlStringRGB(tColor);

                // --- sub-scores ---
                // Intensidad (0..1)
                float dInt = Mathf.Abs(userInt - tInt);
                float sInt = 1f - Mathf.Clamp01(dInt / Mathf.Max(0.0001f, maxIntensityForScore));

                // Kelvin (0..1)
                float sK = 0f;
                if (userK >= 0f)
                {
                    float dK = Mathf.Abs(userK - tKelvin);
                    sK = 1f - Mathf.Clamp01(dK / Mathf.Max(1f, kelvinNormRange));
                }

                // Color (0..1) usando HSV
                Color.RGBToHSV(l.color, out float hU, out float sU, out float vU);
                Color.RGBToHSV(tColor, out float hT, out float sT, out float vT);
                float dH = Mathf.Min(Mathf.Abs(hU - hT), 1f - Mathf.Abs(hU - hT));
                float dS = Mathf.Abs(sU - sT);
                float dV = Mathf.Abs(vU - vT);
                float deltaColor = (dH + dS + dV) / 3f;
                float sColor = 1f - Mathf.Clamp01(deltaColor);

                // Score por lampara
                float sLamp = Mathf.Clamp01(wColor * sColor + wIntensity * sInt + wKelvin * sK);

                // acumular
                count++;
                sumColor += sColor;
                sumInt += sInt;
                sumK += sK;
                score01 += sLamp;

                // Texto amigable
                string colorDesc;
                if (sColor > 0.9f) colorDesc = "muy parecido";
                else if (sColor > 0.7f) colorDesc = "bastante cercano";
                else if (sColor > 0.5f) colorDesc = "algo diferente";
                else colorDesc = "bastante diferente";

                sb.AppendLine("\nLámpara: " + l.name);
                sb.AppendLine("  Objetivo -> Int: " + tInt.ToString("0.00") +
                              " | K: " + Mathf.RoundToInt(tKelvin) +
                              " | Color: #" + targetHex);
                sb.AppendLine("  Tú      -> Int: " + userInt.ToString("0.00") +
                              " | K: " + (userK >= 0 ? Mathf.RoundToInt(userK).ToString() : "-") +
                              " | Color: #" + userHex);
                sb.AppendLine("  Coincidencia aprox. -> " +
                              "Color: " + Mathf.RoundToInt(sColor * 100) + "% (" + colorDesc + "), " +
                              "Intensidad: " + Mathf.RoundToInt(sInt * 100) + "%, " +
                              "K: " + Mathf.RoundToInt(sK * 100) + "%");
            }
            else
            {
                sb.AppendLine("\nLámpara: " + l.name);
                sb.AppendLine("  No hay objetivo definido para esta lámpara.");
                sb.AppendLine("  Tú -> Int: " + userInt.ToString("0.00") +
                              " | K: " + (userK >= 0 ? Mathf.RoundToInt(userK).ToString() : "-") +
                              " | Color: #" + userHex);
            }
        }

        if (count > 0)
        {
            score01 /= count;
            sColorAvg = sumColor / count;
            sIntAvg = sumInt / count;
            sKAvg = sumK / Mathf.Max(1, count);
        }

        return sb.ToString();
    }

    // Objetivos fijos basados en tu log:
    // 0) Lamp_Light1  Int=3.05  Color=#D2204C  K=6000
    // 1) Lamp_Light2  Int=10.00 Color=#1648D2  K=3000
    // 2) Lamp_Light3  Int=6.48  Color=#6E50D2  K=6000
    bool TryGetTargetForLight(string lightName, out float tInt, out Color tColor, out float tKelvin)
    {
        tInt = 0f;
        tColor = Color.white;
        tKelvin = 0f;

        switch (lightName)
        {
            case "Lamp_Light1":
                tInt = 3.05f;
                ColorUtility.TryParseHtmlString("#D2204C", out tColor);
                tKelvin = 6000f;
                return true;

            case "Lamp_Light2":
                tInt = 10.00f;
                ColorUtility.TryParseHtmlString("#1648D2", out tColor);
                tKelvin = 3000f;
                return true;

            case "Lamp_Light3":
                tInt = 6.48f;
                ColorUtility.TryParseHtmlString("#6E50D2", out tColor);
                tKelvin = 6000f;
                return true;

            default:
                return false;
        }
    }

    // ===== LOG GLOBAL A CONSOLA (NO TOCAR) =====
    void LogAllLamps(string contexto)
    {
        if (lightsUI == null || lightsUI.lights == null || lightsUI.lights.Length == 0)
        {
            Debug.Log("[LIGHT LOG] " + contexto + ": no hay luces configuradas.");
            return;
        }

        var sb = new StringBuilder(512);
        sb.AppendLine("========== LIGHT LOG: " + contexto + " ==========");

        for (int i = 0; i < lightsUI.lights.Length; i++)
        {
            var l = lightsUI.lights[i];
            if (!l) continue;

            string hex = ColorUtility.ToHtmlStringRGB(l.color);
            sb.Append(i).Append(") ").Append(l.name).Append("  ")
              .Append("Int=").Append(l.intensity.ToString("0.00")).Append("  ")
              .Append("Color=#").Append(hex).Append("  ")
              .Append("useKelvin=").Append(l.useColorTemperature ? "true" : "false").Append("  ");

            if (l.useColorTemperature)
                sb.Append("K=").Append(Mathf.RoundToInt(l.colorTemperature)).Append("  ");
            else
                sb.Append("K=-  ");

            sb.Append("Range=").Append(l.range.ToString("0.0"));
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }

    // ===== LOG DE COMPARACIONES (texto completo) =====
    void LogComparison(string contexto, string fullReport)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        string block = "[" + time + "] " + contexto + "\n" + fullReport;

        if (logCompareToConsole)
            Debug.Log(block);

        if (keepCompareHistory)
            compareHistory.Add(block);
    }
}

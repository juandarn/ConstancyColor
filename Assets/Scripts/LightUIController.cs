using System;
using System.Collections.Generic;
using UnityEngine;

public class LightUIController : MonoBehaviour
{
    [Header("Luces a controlar")]
    public Light[] lights;      // arrastra aquí tus luces (Lamp_Light, etc.)
    int activeIndex = -1;       // -1 = todas, 0..N = una luz

    [Header("Logging")]
    public bool logToConsole = true;      // mostrar en Console de Unity
    public bool keepHistory = true;       // guardar historial en memoria
    [TextArea(3, 10)]
    public List<string> history = new List<string>(); // visible en el Inspector

    // =================== Helpers ===================

    Light[] Targets()
    {
        return activeIndex < 0 ? lights : new[] { lights[activeIndex] };
    }

    string CurrentTargetLabel()
    {
        if (activeIndex < 0) return "Todas";

        if (lights != null &&
            activeIndex >= 0 &&
            activeIndex < lights.Length &&
            lights[activeIndex] != null)
        {
            return lights[activeIndex].name;
        }

        return "Index " + activeIndex;
    }

    void LogChange(string accion, string detalle)
    {
        string who = CurrentTargetLabel();
        string line = DateTime.Now.ToString("HH:mm:ss") +
                      " | " + accion +
                      " | " + detalle +
                      " | Target: " + who;

        if (logToConsole)
            Debug.Log(line);

        if (keepHistory)
            history.Add(line);
    }

    // =================== API llamada por la UI ===================

    // --- Selección desde el Dropdown (0 = Todas, 1..N = Luz i) ---
    public void SelectLight(int dropdownIndex)
    {
        activeIndex = dropdownIndex - 1;
        LogChange("SelectLight", "dropdownIndex=" + dropdownIndex +
                                 " -> activeIndex=" + activeIndex);
    }

    // --- Temperatura (botones) ---
    public void SetKelvin3000() => SetKelvin(3000f);
    public void SetKelvin4500() => SetKelvin(4500f);
    public void SetKelvin6000() => SetKelvin(6000f);

    public void SetKelvin(float k)
    {
        foreach (var l in Targets())
        {
            if (!l) continue;
            l.useColorTemperature = true;
            l.colorTemperature = k;
        }

        LogChange("SetKelvin", "k=" + k.ToString("0"));
    }

    // --- Color (Hue 0..1) ---
    public void SetHue(float h01)
    {
        float h = Mathf.Repeat(h01, 1f);
        var c = Color.HSVToRGB(h, 1f, 1f);

        foreach (var l in Targets())
        {
            if (!l) continue;
            l.useColorTemperature = false;
            l.color = c;
        }

        string hex = ColorUtility.ToHtmlStringRGB(c);
        LogChange("SetHue", "h01=" + h.ToString("0.00") + " color=#" + hex);
    }

    // --- Intensidad (0..5 aprox) ---
    public void SetIntensity(float v)
    {
        foreach (var l in Targets())
        {
            if (!l) continue;
            l.intensity = v;
        }

        LogChange("SetIntensity", "v=" + v.ToString("0.00"));
    }

    // --- Alternar modo Kelvin/Color desde un Toggle ---
    public void UseKelvin(bool on)
    {
        foreach (var l in Targets())
        {
            if (!l) continue;
            l.useColorTemperature = on;
        }

        LogChange("UseKelvin", on ? "Modo Kelvin ON" : "Modo Color/Filter ON");
    }

    // Cambia el color/filtro (si la luz está en "Filter and Temperature" esto afecta el Filter)
    public void SetFilterColor(Color c)
    {
        foreach (var l in Targets())
        {
            if (!l) continue;
            l.color = c; // NO tocamos useColorTemperature; lo maneja tu toggle UseKelvin(on/off)
        }

        string hex = ColorUtility.ToHtmlStringRGB(c);
        LogChange("SetFilterColor", "color=#" + hex +
                                    " (r=" + c.r.ToString("0.00") +
                                    ", g=" + c.g.ToString("0.00") +
                                    ", b=" + c.b.ToString("0.00") + ")");
    }

    // Atajo para pasar HSV (0..1)
    public void SetHSV(float h, float s, float v)
    {
        h = Mathf.Repeat(h, 1f);
        s = Mathf.Clamp01(s);
        v = Mathf.Clamp01(v);

        var c = Color.HSVToRGB(h, s, v);
        SetFilterColor(c); // esto también loguea el color

        LogChange("SetHSV",
            "h=" + h.ToString("0.00") +
            " s=" + s.ToString("0.00") +
            " v=" + v.ToString("0.00"));
    }
}

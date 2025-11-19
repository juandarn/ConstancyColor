using UnityEngine;

public class LightOneController : MonoBehaviour
{
    [Header("Luz a controlar")]
    public Light lamp;

    // ===== MODO TEMPERATURA =====
    public void SetKelvin(float k)
    {
        if (!lamp) return;
        lamp.useColorTemperature = true;
        lamp.colorTemperature = k;
    }
    public void SetKelvin3000() => SetKelvin(3000f);
    public void SetKelvin4500() => SetKelvin(4500f);
    public void SetKelvin6000() => SetKelvin(6000f);

    // ===== MODO COLOR (HUE 0..1) =====
    public void SetHue(float h01)
    {
        if (!lamp) return;
        lamp.useColorTemperature = false;
        var c = Color.HSVToRGB(Mathf.Repeat(h01, 1f), 1f, 1f);
        lamp.color = c;
    }

    // ===== INTENSIDAD =====
    public void SetIntensity(float value)
    {
        if (!lamp) return;
        lamp.intensity = value;
    }

    // ===== TOGGLE ENTRE MODOS =====
    public void UseKelvin(bool on)
    {
        if (!lamp) return;
        lamp.useColorTemperature = on;
    }
}

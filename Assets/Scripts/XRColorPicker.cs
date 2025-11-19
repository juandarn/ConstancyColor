using UnityEngine;
using UnityEngine.UI;

public class XRColorPicker : MonoBehaviour
{
    [Header("Sliders (0..1)")]
    public Slider hue;   // H
    public Slider sat;   // S
    public Slider val;   // V

    [Header("Preview opcional")]
    public Image preview;

    [Header("Luces destino")]
    public LightUIController lights;

    // cache actual
    float H, S = 1f, V = 1f;

    void Start()
    {
        // inicia con los valores actuales de los sliders si ya están puestos
        if (hue) H = hue.value;
        if (sat) S = sat.value;
        if (val) V = val.value;
        Apply();
    }

    public void OnHue(float x) { H = Mathf.Repeat(x, 1f); Apply(); }
    public void OnSat(float x) { S = Mathf.Clamp01(x); Apply(); }
    public void OnVal(float x) { V = Mathf.Clamp01(x); Apply(); }

    void Apply()
    {
        var c = Color.HSVToRGB(H, S, V);
        if (preview) preview.color = c;
        if (lights) lights.SetHSV(H, S, V);
    }
}

using UnityEngine;

public class RainbowDecalAndLight : MonoBehaviour
{
    [Header("Targets")]
    public Renderer decalRenderer;   // 쿼드(데칼) Renderer
    public Light spotLight;         // Spot Light

    [Header("Rainbow")]
    [Tooltip("색상 변화 속도(1 = 1초에 한 바퀴 정도 느낌).")]
    public float hueSpeed = 0.15f;

    [Range(0f, 1f)] public float saturation = 1f;
    [Tooltip("1 이상이면 더 쨍하게 보임(특히 Additive).")]
    public float value = 1.2f;

    [Header("Decal Intensity")]
    [Tooltip("머티리얼 컬러 알파(Transparent 계열이면 투명도, Additive면 강도 느낌).")]
    [Range(0f, 1f)] public float decalAlpha = 1f;

    [Header("Optional: Blink (펄스)")]
    public bool pulse = false;
    public float pulseSpeed = 6f;
    [Range(0f, 1f)] public float pulseMin = 0.35f;
    [Range(0f, 1f)] public float pulseMax = 1.0f;

    [Header("Light Intensity (optional pulse)")]
    public bool pulseLightIntensity = false;
    public float lightMinIntensity = 0.3f;
    public float lightMaxIntensity = 1.2f;

    Material _decalMat;

    void Awake()
    {
        if (decalRenderer != null)
        {
            // renderer.material: 인스턴스 생성(런타임에서 개별 제어 가능)
            _decalMat = decalRenderer.material;
        }
    }

    void Update()
    {
        // 1) 레인보우 컬러 생성 (HSV)
        float h = Mathf.Repeat(Time.time * hueSpeed, 1f);
        Color rainbow = Color.HSVToRGB(h, saturation, value);

        // 2) 펄스(선택)
        float p = 1f;
        if (pulse)
        {
            float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0~1
            p = Mathf.Lerp(pulseMin, pulseMax, wave);
        }

        // 3) 데칼 머티리얼 컬러 적용
        if (_decalMat != null)
        {
            Color c = rainbow;
            c.a = Mathf.Clamp01(decalAlpha * p);
            _decalMat.color = c;
        }

        // 4) 스팟라이트 컬러 적용
        if (spotLight != null)
        {
            spotLight.color = Color.HSVToRGB(h, saturation, 1f); // 라이트는 value 1 추천

            if (pulseLightIntensity)
            {
                float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                spotLight.intensity = Mathf.Lerp(lightMinIntensity, lightMaxIntensity, wave);
            }
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(Light))]
public class DiscoLightController : MonoBehaviour
{
    [Header("Rotation")]
    public bool enableRotation = true;
    public Vector3 rotationAxis = new Vector3(0f, 1f, 0f); // Y축 회전 기본
    public float rotationSpeed = 25f;

    [Header("Rainbow Color")]
    public bool enableRainbow = true;
    public float rainbowSpeed = 0.25f;   // 0.1~1.0 추천
    [Range(0f, 1f)] public float saturation = 1f;
    [Range(0f, 1f)] public float brightness = 1f;

    [Header("Optional Intensity Pulse")]
    public bool enableIntensityPulse = false;
    public float baseIntensity = 3f;
    public float pulseAmplitude = 1f;
    public float pulseSpeed = 1.5f;

    private Light _light;

    void Awake()
    {
        _light = GetComponent<Light>();

        // 초기값 세팅
        if (enableIntensityPulse)
            _light.intensity = baseIntensity;
    }

    void Update()
    {
        // 1) 회전
        if (enableRotation)
        {
            transform.Rotate(rotationAxis.normalized, rotationSpeed * Time.deltaTime, Space.Self);
        }

        // 2) 레인보우 컬러 변화
        if (enableRainbow)
        {
            float h = Mathf.Repeat(Time.time * rainbowSpeed, 1f);
            _light.color = Color.HSVToRGB(h, saturation, brightness);
        }

        // 3) (옵션) 밝기 숨쉬기
        if (enableIntensityPulse)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0~1
            _light.intensity = baseIntensity + (t * pulseAmplitude);
        }
    }
}

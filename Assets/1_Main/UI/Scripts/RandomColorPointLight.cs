using UnityEngine;

[RequireComponent(typeof(Light))]
public class SmoothRainbowLight : MonoBehaviour
{
    [Header("Color Settings")]
    [Range(0f, 1f)] public float saturation = 0.9f;
    [Range(0f, 1f)] public float value = 1.0f;

    [Header("Speed")]
    public float hueSpeed = 0.1f; // 값이 작을수록 더 부드러움

    private Light pointLight;
    private float hue;

    void Start()
    {
        pointLight = GetComponent<Light>();
        pointLight.type = LightType.Point;
    }

    void Update()
    {
        hue += Time.deltaTime * hueSpeed;
        hue = Mathf.Repeat(hue, 1f);

        pointLight.color = Color.HSVToRGB(hue, saturation, value);
    }
}

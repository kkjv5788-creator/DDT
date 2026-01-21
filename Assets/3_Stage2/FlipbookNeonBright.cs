using UnityEngine;

public class FlipbookNeonBright : MonoBehaviour
{
    [Header("Frames (Quads)")]
    public GameObject[] frames;

    [Header("Playback")]
    public float fps = 5f;
    public bool loop = true;

    [Header("Rainbow Color")]
    public float hueSpeed = 0.08f;
    [Range(0f, 1f)] public float saturation = 1f;

    [Tooltip("최소 밝기 보장 (어두워지지 않게)")]
    [Range(0.6f, 2.5f)] public float minValue = 1.2f;

    [Tooltip("최대 밝기(네온 강도). Additive면 1.5~2.5 추천")]
    [Range(0.8f, 4f)] public float maxValue = 2.0f;

    [Header("Smooth Pulse (강도만 변화)")]
    public bool smoothPulse = true;
    public float pulseSpeed = 1.5f;
    [Range(0f, 1f)] public float pulseMin = 0.35f;
    [Range(0f, 1f)] public float pulseMax = 1.0f;
    public AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Alpha Lock (색이 죽는 원인 제거)")]
    public bool lockAlpha = true;
    [Range(0.1f, 1f)] public float fixedAlpha = 1.0f;

    int index = 0;
    float frameTimer = 0f;
    Material currentMat;

    void Awake()
    {
        if (frames == null || frames.Length == 0)
        {
            frames = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                frames[i] = transform.GetChild(i).gameObject;
        }

        if (frames.Length == 0) return;

        ShowOnly(index);
        CacheMaterial();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        // 1) Flipbook 프레임 전환
        float frameTime = 1f / Mathf.Max(1f, fps);
        frameTimer += Time.deltaTime;
        while (frameTimer >= frameTime)
        {
            frameTimer -= frameTime;
            index = (index + 1) % frames.Length;
            ShowOnly(index);
            CacheMaterial();
        }

        if (currentMat == null) return;

        // 2) 펄스 (0~1)
        float p = 1f;
        if (smoothPulse)
        {
            float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float eased = pulseCurve.Evaluate(wave);
            p = Mathf.Lerp(pulseMin, pulseMax, eased);
        }

        // 3) 밝기(value)를 최소 보장하면서 펄스로 강도 조절
        float v = Mathf.Lerp(minValue, maxValue, p); // 항상 minValue 이상

        // 4) 레인보우 컬러 생성
        float h = Mathf.Repeat(Time.time * hueSpeed, 1f);
        Color col = Color.HSVToRGB(h, saturation, v);

        // 5) 알파는 고정(어두워 보이는 원인 제거)
        if (lockAlpha) col.a = fixedAlpha;

        currentMat.color = col;
    }

    void ShowOnly(int i)
    {
        for (int k = 0; k < frames.Length; k++)
            frames[k].SetActive(k == i);
    }

    void CacheMaterial()
    {
        var r = frames[index].GetComponent<Renderer>();
        currentMat = (r != null) ? r.material : null;
    }
}

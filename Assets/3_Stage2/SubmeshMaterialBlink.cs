using UnityEngine;

public class SignBulbBlinkPro : MonoBehaviour
{
    public enum BlinkMode
    {
        Sequential,     // 빨-노-초-파 순차(또는 슬롯 순서)
        RandomFlicker,  // 랜덤 점멸
        AllOnOff        // 전체 점등 -> 전체 소등 반복
    }

    [Header("Target")]
    public Renderer target;
    public int[] materialIndices = new int[] { 1, 2, 3, 4 }; // Red, Yellow, Green, Blue

    [Header("Mode")]
    public BlinkMode mode = BlinkMode.Sequential;

    [Header("Timing")]
    [Tooltip("전체 속도 (커질수록 빠름)")]
    public float speed = 6f;

    [Tooltip("Sequential일 때 슬롯별 시간차")]
    public float phaseOffset = 0.6f;

    [Tooltip("AllOnOff일 때 ON 유지 비율 (0~1)")]
    [Range(0f, 1f)] public float allOnDuty = 0.55f;

    [Header("Emission")]
    public float minEmission = 0f;     // OFF쪽 발광 (0 추천)
    public float maxEmission = 4.5f;   // ON쪽 발광 (3~8 추천)

    [Header("Base Color dim (OFF 어둡게)")]
    [Tooltip("OFF일 때 베이스 색 곱 (0.08~0.2 추천)")]
    public float offBaseDim = 0.12f;

    [Tooltip("ON일 때 베이스 색 곱 (보통 1)")]
    public float onBaseDim = 1f;

    [Header("Optional: Beat 느낌(계단식 점등)")]
    public bool stepLikeBeat = false;
    [Range(1, 16)] public int stepsPerCycle = 4; // 4면 4박 느낌

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    Material[] mats;
    Color[] originalBase;

    void Awake()
    {
        if (!target) target = GetComponent<Renderer>();
        if (!target) { enabled = false; return; }

        mats = target.materials; // 인스턴스 생성됨
        originalBase = new Color[mats.Length];
        for (int i = 0; i < mats.Length; i++)
            originalBase[i] = mats[i].color;

        foreach (int idx in materialIndices)
        {
            if (idx < 0 || idx >= mats.Length) continue;
            mats[idx].EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        float t = Time.time;

        for (int i = 0; i < materialIndices.Length; i++)
        {
            int idx = materialIndices[i];
            if (idx < 0 || idx >= mats.Length) continue;

            float v = Evaluate(i, t); // 0~1
            ApplyToMaterial(idx, v);
        }
    }

    float Evaluate(int slotIndex, float time)
    {
        float v = 0f;

        switch (mode)
        {
            case BlinkMode.Sequential:
                {
                    v = (Mathf.Sin((time * speed) - slotIndex * phaseOffset) + 1f) * 0.5f; // 0~1
                    break;
                }
            case BlinkMode.RandomFlicker:
                {
                    v = Mathf.PerlinNoise(time * speed * 0.35f, slotIndex * 0.37f); // 0~1
                    break;
                }
            case BlinkMode.AllOnOff:
                {
                    // 0~1 반복 신호
                    float cycle = Mathf.Repeat(time * (speed * 0.25f), 1f);
                    v = (cycle < allOnDuty) ? 1f : 0f;
                    break;
                }
        }

        if (stepLikeBeat)
        {
            // 계단식(박자처럼 딱딱 끊기게)
            float step = Mathf.Floor(v * stepsPerCycle) / Mathf.Max(1, stepsPerCycle - 1);
            v = Mathf.Clamp01(step);
        }

        return Mathf.Clamp01(v);
    }

    void ApplyToMaterial(int idx, float v)
    {
        // OFF일 때 베이스도 어둡게
        float baseMul = Mathf.Lerp(offBaseDim, onBaseDim, v);
        Color baseCol = originalBase[idx] * baseMul;
        mats[idx].SetColor(ColorId, baseCol);

        // Emission
        float e = Mathf.Lerp(minEmission, maxEmission, v);
        Color emission = originalBase[idx] * e;
        mats[idx].SetColor(EmissionId, emission);
    }

    // 외부에서 모드 바꾸고 싶을 때 호출용
    public void SetMode(int modeIndex)
    {
        mode = (BlinkMode)Mathf.Clamp(modeIndex, 0, 2);
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageUIManager : MonoBehaviour
{
    public static StageUIManager Instance;

    [Header("HUD - Judgement")]
    public TextMeshProUGUI judgeText; // "PERFECT!" 텍스트

    [Header("HUD - Gauge")]
    public Slider gaugeSlider;         // 게이지 바
    public Image gaugeFill;            // 색상 변경용
    public Gradient gaugeColor;        // 0~100% 색상

    [Header("Guides")]
    public GameObject radioGuide;      // 라디오 위 화살표
    public GameObject handTooltip;     // 왼손 메뉴 설명

    void Awake()
    {
        Instance = this;
        if (judgeText) judgeText.gameObject.SetActive(false);
    }

    // 판정 띄우기
    public void ShowJudgement(string text, Color color)
    {
        if (!judgeText) return;
        judgeText.text = text;
        judgeText.color = color;
        judgeText.gameObject.SetActive(true);

        // 연출: 커졌다가 사라짐 (임시)
        judgeText.transform.localScale = Vector3.one * 1.5f;
        CancelInvoke(nameof(HideJudge));
        Invoke(nameof(HideJudge), 0.5f);
    }
    void HideJudge() => judgeText.gameObject.SetActive(false);

    // 게이지 업데이트
    public void UpdateGauge(float current, float max)
    {
        if (!gaugeSlider) return;
        float ratio = Mathf.Clamp01(current / max);
        gaugeSlider.value = ratio;
        if (gaugeFill && gaugeColor != null) 
            gaugeFill.color = gaugeColor.Evaluate(ratio);
    }

    // 가이드 끄기
    public void SetGuides(bool showRadio, bool showHand)
    {
        if (radioGuide) radioGuide.SetActive(showRadio);
        if (handTooltip) handTooltip.SetActive(showHand);
    }
}
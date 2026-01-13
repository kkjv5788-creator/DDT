using UnityEngine;
using UnityEngine.UI;

public class ProgressGaugeUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider progressSlider;        // 0 ~ 100
    public Image fillImage;              // Slider의 Fill
    public Text percentageText;          // "75%"
    public Text countText;               // "15 / 20"

    [Header("Colors")]
    public Gradient colorGradient;       // 0% ~ 100% 색상 변화

    public void UpdateProgress(int successCount, int totalTriggers, float percentage)
    {
        percentage = Mathf.Clamp(percentage, 0f, 100f);

        // Slider 업데이트
        if (progressSlider)
        {
            progressSlider.value = percentage;
        }

        // Fill 색상 변경
        if (fillImage && colorGradient != null)
        {
            fillImage.color = colorGradient.Evaluate(percentage / 100f);
        }

        // 퍼센트 텍스트
        if (percentageText)
        {
            percentageText.text = $"{percentage:F0}%";
        }

        // 카운트 텍스트
        if (countText)
        {
            countText.text = $"{successCount} / {totalTriggers}";
        }
    }
}
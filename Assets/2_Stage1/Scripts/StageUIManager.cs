using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageUIManager : MonoBehaviour
{
    [Header("Refs - Managers")]
    public RhythmConductor conductor; // [필수] RhythmConductor 연결

    [Header("UI - Play Game Status")]
    public Slider sliderSongProgress;        // 곡 진행도 슬라이더

    [Header("UI - Sales Info")]
    public TextMeshProUGUI txtRealtimeSales; // 매출 텍스트
    public TextMeshProUGUI txtFeedback;      // 피드백 텍스트 (+5000원)

    private void Start()
    {
        if (txtFeedback) txtFeedback.gameObject.SetActive(false);
        UpdateSalesUI(0, 0, false);
    }

    void Update()
    {
        // 매 프레임 노래 시간을 체크해서 슬라이더 갱신
        if (conductor != null && sliderSongProgress != null)
        {
            // 1. 현재 노래 시간 (BgmTime 변수 사용)
            float current = conductor.BgmTime;
            
            // 2. 노래 전체 길이 (오디오 소스에서 가져옴)
            float length = 1f;
            if (conductor.bgmSource != null && conductor.bgmSource.clip != null)
            {
                length = conductor.bgmSource.clip.length;
            }

            // 3. 슬라이더 값 적용 (0~1 사이)
            if (length > 0)
            {
                sliderSongProgress.value = Mathf.Clamp01(current / length);
            }
        }
    }

    // 돈 벌었을 때 호출됨 (ResultManager에서)
    public void UpdateSalesUI(int totalSales, int addedSales, bool showFeedback = true)
    {
        if (txtRealtimeSales)
        {
            txtRealtimeSales.text = string.Format("{0:#,###}원", totalSales);
        }

        if (showFeedback && txtFeedback && addedSales > 0)
        {
            StopAllCoroutines(); 
            StartCoroutine(CoShowFeedback(addedSales));
        }
    }

    IEnumerator CoShowFeedback(int amount)
    {
        txtFeedback.text = string.Format("+ {0:#,###}원", amount);
        txtFeedback.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.0f);
        txtFeedback.gameObject.SetActive(false);
    }
}
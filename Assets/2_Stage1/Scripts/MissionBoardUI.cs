using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class MissionBoardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI textHeader;      // 제목 (예: < 실습 1단계 >)
    public TextMeshProUGUI textMissionName; // 미션 내용 (예: 김밥 3줄 썰기)
    public TextMeshProUGUI textProgress;    // 진행도 (예: □ ■ □)
    public GameObject stampObject;          // 도장 오브젝트

    [Header("Settings")]
    public string uncheckedMark = "<color=#999999>□</color>";
    public string checkedMark = "<color=red>■</color>";

    // 초기화
    public void InitializeUI()
    {
        if (stampObject) stampObject.SetActive(false);
        UpdateText("", "", "");
    }

    // ★ [추가됨] 헤더만 변경 (매니저들이 호출함)
    public void UpdateHeader(string header)
    {
        if (textHeader) textHeader.text = header;
    }

    // ★ [추가됨] 미션 이름과 진행도를 한방에 변경 (매니저들이 호출함)
    public void UpdateMission(string missionName, int current, int max)
    {
        if (textMissionName) textMissionName.text = missionName;
        UpdateProgress(current, max);
    }

    // 텍스트 전체 변경
    public void UpdateText(string header, string mission, string progress)
    {
        if (textHeader && !string.IsNullOrEmpty(header)) textHeader.text = header;
        if (textMissionName && !string.IsNullOrEmpty(mission)) textMissionName.text = mission;
        if (textProgress && !string.IsNullOrEmpty(progress)) textProgress.text = progress;
    }

    // 체크박스 생성 로직
    public void UpdateProgress(int current, int max)
    {
        if (textProgress)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < max; i++)
            {
                sb.Append((i < current) ? checkedMark + " " : uncheckedMark + " ");
            }
            textProgress.text = sb.ToString();
        }
    }

    // 도장 쾅! 연출
    public void ShowSuccessStamp(bool show)
    {
        if (stampObject)
        {
            stampObject.SetActive(show);
            if (show) StartCoroutine(StampEffect());
        }
    }

    IEnumerator StampEffect()
    {
        stampObject.transform.localScale = Vector3.one * 1.5f;
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            stampObject.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t / 0.2f);
            yield return null;
        }
        stampObject.transform.localScale = Vector3.one;
    }
}
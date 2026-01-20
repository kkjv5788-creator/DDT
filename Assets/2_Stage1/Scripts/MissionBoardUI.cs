using UnityEngine;
using TMPro;
using System.Collections;

public class MissionBoardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI textHeader;      // 예: "< 수습 교육 >"
    public TextMeshProUGUI textMissionName; // 예: "김밥 3줄 썰기"
    public TextMeshProUGUI textProgress;    // 예: "□ □ □"
    public GameObject stampObject;          // 완료 도장 이미지 (평소엔 꺼둠)

    [Header("Settings")]
    public string uncheckedMark = "<color=#999999>□</color>"; // 회색 체크박스
    public string checkedMark = "<color=red>■</color>";       // 빨간 체크박스 (또는 검정)

    /// <summary>
    /// UI 초기화 (백지 상태)
    /// </summary>
    public void InitializeUI()
    {
        if (stampObject) stampObject.SetActive(false);
        UpdateText("", "", "");
    }

    /// <summary>
    /// 텍스트 개별 업데이트
    /// </summary>
    public void UpdateText(string header, string mission, string progress)
    {
        if (textHeader) textHeader.text = header;
        if (textMissionName) textMissionName.text = mission;
        if (textProgress) textProgress.text = progress;
    }

    /// <summary>
    /// 미션 내용과 진행도(체크박스) 업데이트
    /// </summary>
    public void UpdateMission(string missionName, int currentCount, int maxCount)
    {
        if (textMissionName) textMissionName.text = missionName;

        // 체크박스 문자열 생성 (예: ■ ■ □)
        if (textProgress)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < maxCount; i++)
            {
                if (i < currentCount)
                    sb.Append(checkedMark + " ");
                else
                    sb.Append(uncheckedMark + " ");
            }
            textProgress.text = sb.ToString();
        }
    }

    /// <summary>
    /// 헤더 텍스트만 변경
    /// </summary>
    public void UpdateHeader(string header)
    {
        if (textHeader) textHeader.text = header;
    }

    /// <summary>
    /// 성공 도장 쾅! (애니메이션 포함)
    /// </summary>
    public void ShowSuccessStamp(bool show)
    {
        if (!stampObject) return;

        if (show)
        {
            stampObject.SetActive(true);
            // 쾅! 찍히는 연출 (커졌다가 작아짐)
            StartCoroutine(StampAnimation());
        }
        else
        {
            stampObject.SetActive(false);
        }
    }

    IEnumerator StampAnimation()
    {
        stampObject.transform.localScale = Vector3.one * 2.0f;
        float timer = 0f;
        while(timer < 0.2f)
        {
            timer += Time.deltaTime;
            stampObject.transform.localScale = Vector3.Lerp(Vector3.one * 2.0f, Vector3.one, timer / 0.2f);
            yield return null;
        }
        stampObject.transform.localScale = Vector3.one;
    }
}
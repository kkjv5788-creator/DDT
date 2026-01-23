// Assets/3_Stage2/Scripts/UI/Stage2UIManager_st2.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Stage2UIManager_st2 : MonoBehaviour
{
    [Header("Panels")]
    public GameObject Panel_SelectMode;
    public GameObject Panel_Dialogue;
    public GameObject Panel_PlayGame;
    public GameObject Sales_Info_Group;
    public GameObject Panel_ClosingResult;
    public GameObject Panel_Pause;

    [Header("PlayGame UI")]
    public Slider musicGauge;              // 0~1
    public TextMeshProUGUI musicTimeText;  // 선택

    [Header("Sales UI")]
    public TextMeshProUGUI totalSalesText;
    public TextMeshProUGUI addSalesText;

    [Header("Closing UI")]
    public TextMeshProUGUI closingSalesText;
    public TextMeshProUGUI closingMentText;

    public void ShowSelectMode()
    {
        SetOnly(Panel_SelectMode);
    }

    public void ShowTutorialDialogue()
    {
        SetOnly(Panel_Dialogue);
    }

    public void ShowPlayGameHUD()
    {
        // 플레이 중에는 여러 개 같이 켜질 수 있으니 여기선 개별 컨트롤
        SafeSet(Panel_SelectMode, false);
        SafeSet(Panel_Dialogue, false);
        SafeSet(Panel_PlayGame, true);
        SafeSet(Sales_Info_Group, true);
        SafeSet(Panel_ClosingResult, false);
        SafeSet(Panel_Pause, false);
    }

    public void ShowClosingResult()
    {
        SetOnly(Panel_ClosingResult);
    }

    public void ShowPause(bool show)
    {
        SafeSet(Panel_Pause, show);
    }

    public void UpdateMusicProgress(float normalized01, float seconds = -1f)
    {
        if (musicGauge != null) musicGauge.value = Mathf.Clamp01(normalized01);
        if (musicTimeText != null && seconds >= 0f) musicTimeText.text = $"{seconds:0.0}s";
    }

    public void UpdateSales(int total, int added)
    {
        if (totalSalesText != null) totalSalesText.text = $"{total:n0}";
        if (addSalesText != null) addSalesText.text = added > 0 ? $"+{added:n0}" : $"{added:n0}";
    }

    public void UpdateClosing(int total, string ment)
    {
        if (closingSalesText != null) closingSalesText.text = $"{total:n0}";
        if (closingMentText != null) closingMentText.text = ment ?? "";
    }

    private void SetOnly(GameObject panel)
    {
        SafeSet(Panel_SelectMode, panel == Panel_SelectMode);
        SafeSet(Panel_Dialogue, panel == Panel_Dialogue);
        SafeSet(Panel_PlayGame, panel == Panel_PlayGame);
        SafeSet(Sales_Info_Group, panel == Sales_Info_Group);
        SafeSet(Panel_ClosingResult, panel == Panel_ClosingResult);
        SafeSet(Panel_Pause, panel == Panel_Pause);
    }

    private void SafeSet(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }
}

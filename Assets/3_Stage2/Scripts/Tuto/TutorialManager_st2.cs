using TMPro;
using UnityEngine;

public class TutorialManager_st2 : MonoBehaviour
{
    [Header("UI(선택)")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI skipText;

    // 외부에서 밀어 넣을 “표시용 상태”
    private string _stageInfo = "Tutorial";
    private string _instruction = "";
    private string _progress = "";

    void Start()
    {
        if (skipText != null)
            skipText.text = "[Press B to Skip Tutorial]";
    }

    // ✅ Update로 진행 체크 절대 안 함
    void Update()
    {
        // 아무것도 하지 않음 (상태 갱신은 외부에서만)
    }

    // === 외부(StandaloneTutorialController)에서 호출 ===
    public void SetStageInfo(string stageInfo)
    {
        _stageInfo = stageInfo;
    }

    public void SetInstruction(string text)
    {
        _instruction = text;
        if (instructionText != null) instructionText.text = text;
    }

    public void SetProgress(string text)
    {
        _progress = text;
        if (progressText != null) progressText.text = text;
    }

    public string GetCurrentStageInfo()
    {
        // DebugHUD가 가져가서 표시하려면 이거 쓰면 됨
        return _stageInfo;
    }
}

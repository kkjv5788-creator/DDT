using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class TutorialController : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;
    public RadioClickable radio;
    public MissionBoardUI missionBoard; 

    [Header("UI Groups")]
    public GameObject salesUIGroup; 

    [Header("Monitor UI")]
    public TextMeshProUGUI monitorText; 
    public TextMeshProUGUI salesText;   

    [Header("Final Result UI")]
    public GameObject finalResultPanel;        
    public TextMeshProUGUI finalTotalText;     
    public TextMeshProUGUI finalCommentText;   

    [Header("Tutorial Settings")]
    public RhythmTriggerListSO tutorialTriggerList;
    public int requiredSuccessCount = 1; 

    [Header("Wage Settings")]
    public int maxTotalWage = 100000; 
    
    [Tooltip("대괄호 [ ] 안의 텍스트 색상")]
    public string highlightTextColor = "#FF3333"; 

    int _currentStepIndex = 0;
    int _successCountThisStep = 0; 
    bool _tutorialCompleted = false;
    bool _isProcessingResult = false;
    float _lastSkipInput = -999f;
    bool _waitForFinalInput = false; 

    void OnEnable()
    {
        _currentStepIndex = 0;
        _successCountThisStep = 0;
        _tutorialCompleted = false;
        _isProcessingResult = false;
        _waitForFinalInput = false;

        if (conductor) 
        {
            conductor.OnRoundResult.AddListener(HandleRoundResult);
            conductor.OnSliceSuccess.AddListener(HandleSliceSuccess);
        }

        StartTutorial();
    }

    void OnDisable()
    {
        if (conductor) 
        {
            conductor.OnRoundResult.RemoveListener(HandleRoundResult);
            conductor.OnSliceSuccess.RemoveListener(HandleSliceSuccess);
        }
    }

    void Update()
    {
        if (!_tutorialCompleted && OVRInput.GetDown(OVRInput.Button.One))
        {
            if (Time.timeScale > 0.1f && Time.time - _lastSkipInput > (feedbackSet ? feedbackSet.skipInputCooldown : 0.5f))
            {
                _lastSkipInput = Time.time;
                SkipTutorial();
            }
        }

        if (_waitForFinalInput)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                _waitForFinalInput = false;
                FinalizeAndStartGame();
            }
        }
    }

    public void StartTutorial()
    {
        if (!conductor || !tutorialTriggerList) return;

        if (salesUIGroup) salesUIGroup.SetActive(true);

        conductor.isTutorialMode = true;
        conductor.data = tutorialTriggerList;
        
        conductor.StartGame();
        
        // [중요] StartGame 호출 후에 Loop를 켜야 안전합니다.
        if (conductor.bgmSource) conductor.bgmSource.loop = true; 

        if (missionBoard) missionBoard.InitializeUI();
        if (finalResultPanel) finalResultPanel.SetActive(false);

        UpdateSalesUI();
        UpdateMissionUI(); 

        ShowMonitorText("<size=60%>면접 실습 시작</size>", Color.green);
        Invoke(nameof(StartFirstTrigger), 1.5f);
    }

    void StartFirstTrigger()
    {
        if (conductor)
        {
            conductor.AdvanceToNextTrigger();
            UpdateMissionUI(); 
            ShowMonitorText("", Color.white);
        }
    }

    void HandleSliceSuccess(Vector3 pos, Vector3 normal, float speed)
    {
        UpdateMissionUI();
    }

    void HandleRoundResult(bool success)
    {
        if (_tutorialCompleted || _isProcessingResult) return;
        _isProcessingResult = true;

        if (success)
        {
            _successCountThisStep++;
            ShowMonitorText("성공!", Color.green);
            
            if (_successCountThisStep >= requiredSuccessCount)
            {
                if (missionBoard) missionBoard.ShowSuccessStamp(true);
                Invoke(nameof(ShowClearMessage), 1.0f);
                
                _currentStepIndex++;
                _successCountThisStep = 0;

                if (_currentStepIndex >= tutorialTriggerList.triggers.Length)
                {
                    Invoke(nameof(CompleteTutorial), 2.5f);
                    return;
                }
                Invoke(nameof(MoveToNextStep), 2.5f);
            }
            else
            {
                Invoke(nameof(RetryCurrentStep), 1.5f);
                Invoke(nameof(ClearMonitorText), 1.5f);
            }
        }
        else
        {
            ShowMonitorText("다시!", Color.red);
            Invoke(nameof(RetryCurrentStep), 1.5f);
            Invoke(nameof(ClearMonitorText), 1.0f);
        }
    }

    void MoveToNextStep()
    {
        if (conductor) { conductor.AdvanceToNextTrigger(); UpdateMissionUI(); }
        _isProcessingResult = false;
        ClearMonitorText();
    }

    void RetryCurrentStep()
    {
        if (conductor) { conductor.RetryCurrentTrigger(); UpdateMissionUI(); }
        _isProcessingResult = false;
    }

    void UpdateMissionUI()
    {
        if (!missionBoard) return;
        
        missionBoard.ShowSuccessStamp(false);
        missionBoard.UpdateHeader($"< 실습 {_currentStepIndex + 1} 단계 >");

        int targetSliceCount = 0;
        if (tutorialTriggerList != null && _currentStepIndex < tutorialTriggerList.triggers.Length)
            targetSliceCount = tutorialTriggerList.triggers[_currentStepIndex].requiredSliceCount;

        int currentSlices = 0;
        if (conductor != null && conductor.IsJudgingWindow())
            currentSlices = conductor.SliceCount;
        
        int remainCount = Mathf.Max(0, targetSliceCount - currentSlices);
        string icons = "";
        for (int i = 0; i < remainCount; i++) icons += "@ "; 

        string progressText = $"<color={highlightTextColor}>{currentSlices}</color> / {targetSliceCount}";

        string guideText = "";
        switch (_currentStepIndex)
        {
            case 0: guideText = "망설이지 말고\n[직접] 한번 썰어보게나!"; break;
            case 1: guideText = "잘했네! 감이 좋군.\n이제 [비트]에 맞춰 썰어보자!"; break;
            default: guideText = "박자에 맞춰\n[정확히] 썰어보게!"; break;
        }

        guideText = ApplyHighlightColor(guideText);
        guideText += $"\n\n<size=120%>[ {progressText} ]</size>\n<size=150%>{icons}</size>";
        
        missionBoard.UpdateMission(guideText, 0, 0);
    }
    
    string ApplyHighlightColor(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return Regex.Replace(input, @"\[(.*?)\]", $"<color={highlightTextColor}>$0</color>");
    }

    void UpdateSalesUI() { if (salesText) salesText.text = "면접 실습\n[ 진행 중 ]"; }
    void ShowClearMessage() { ShowMonitorText("통과!", Color.cyan); }

    void SkipTutorial()
    {
        _tutorialCompleted = true;
        Cleanup();
        if (missionBoard) { missionBoard.UpdateText("< 알림 >", "교육 스킵됨", ""); missionBoard.ShowSuccessStamp(false); }
        if (conductor.OnTutorialSkipped != null) conductor.OnTutorialSkipped.Invoke();
    }

    void CompleteTutorial()
    {
        _tutorialCompleted = true;

        string bigTitle = "** 합 격 **"; 
        string finalMessage = "<color=#505050>합격이네.\n자 이제 영업을 시작하자.</color>";
        string startHint = "\n<color=#FF8000><b>[오른손 트리거] 영업 시작!</b></color>";

        if (missionBoard)
        {
            missionBoard.ShowSuccessStamp(true);
            missionBoard.UpdateHeader("< 채용 결과 >");
            missionBoard.UpdateText("< 채용 결과 >", "<size=180%><color=red>합격!</color></size>", "");
        }
        
        if (finalResultPanel)
        {
            finalResultPanel.SetActive(true);
            if (finalTotalText) finalTotalText.text = bigTitle;
            if (finalCommentText) { finalCommentText.text = finalMessage + startHint; finalCommentText.color = Color.white; }
        }
        
        ShowMonitorText(finalMessage, Color.cyan);
        _waitForFinalInput = true;
    }

    void FinalizeAndStartGame()
    {
        Cleanup();
        if (conductor.OnTutorialCompleted != null) conductor.OnTutorialCompleted.Invoke();
    }

    void Cleanup()
    {
        if (conductor) { conductor.isTutorialMode = false; conductor.StopAllGuideBeats(); }
        if (salesUIGroup) salesUIGroup.SetActive(false);
        if (radio) { radio.SetTutorialCompleted(true); radio.SetClickable(true); }
        
        if (finalResultPanel) finalResultPanel.SetActive(false);
        ClearMonitorText();
    }

    void ShowMonitorText(string text, Color color)
    {
        if (monitorText) { monitorText.text = text; monitorText.color = color; monitorText.gameObject.SetActive(true); }
    }

    void ClearMonitorText() { if (monitorText) monitorText.text = ""; }
}
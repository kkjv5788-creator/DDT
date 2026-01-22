using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class TutorialController : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;
    public RadioClickable radio;
    
    // ★ 공용 리갈패드 연결
    public MissionBoardUI missionBoard; 

    // ★ 모니터 & 패널 연결
    [Header("UI Groups")]
    public GameObject salesUIGroup; // Sales_Info_Group

    [Header("Monitor UI")]
    public TextMeshProUGUI monitorText; // Text_Feedback
    public TextMeshProUGUI salesText;   // Text_RealtimeSales

    [Header("Final Result UI")]
    public GameObject finalResultPanel;        
    public TextMeshProUGUI finalTotalText;     
    public TextMeshProUGUI finalCommentText;   

    [Header("Tutorial Settings")]
    public RhythmTriggerListSO tutorialTriggerList;
    public int requiredSuccessCount = 1; 

    [Header("Wage Settings")]
    public int maxTotalWage = 100000; 
    private int _wagePerSlice;        
    
    [Tooltip("대괄호 [ ] 안의 텍스트 색상")]
    public string highlightTextColor = "#FF3333"; 

    // 내부 변수
    int _currentStepIndex = 0;
    int _successCountThisStep = 0; 
    int _currentEarnedMoney = 0;
    int _totalPossibleSlices = 0; 
    bool _tutorialCompleted = false;
    bool _isProcessingResult = false;
    float _lastSkipInput = -999f;

    void OnEnable()
    {
        Debug.Log("[TutorialController] 튜토리얼 모드 활성화됨");

        _currentStepIndex = 0;
        _successCountThisStep = 0;
        _currentEarnedMoney = 0;
        _tutorialCompleted = false;
        _isProcessingResult = false;

        CalculateWageSettings();

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
        if (_tutorialCompleted) return;

        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (Time.time - _lastSkipInput > (feedbackSet ? feedbackSet.skipInputCooldown : 0.5f))
            {
                _lastSkipInput = Time.time;
                SkipTutorial();
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

        if (missionBoard) missionBoard.InitializeUI();
        if (finalResultPanel) finalResultPanel.SetActive(false);

        UpdateSalesUI();
        UpdateMissionUI(); 

        ShowMonitorText("<size=60%>영업 개시\n정산을 시작합니다.</size>", Color.green);
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
            _currentEarnedMoney += _wagePerSlice;
            if (_currentEarnedMoney > maxTotalWage - 100) _currentEarnedMoney = maxTotalWage;

            UpdateSalesUI();
            ShowMonitorText($"+ {_wagePerSlice:N0} 원", Color.green);
            
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
            ShowMonitorText("+ 0 원", Color.gray);
            Invoke(nameof(RetryCurrentStep), 1.5f);
            Invoke(nameof(ClearMonitorText), 1.0f);
        }
    }

    void MoveToNextStep()
    {
        if (conductor)
        {
            conductor.AdvanceToNextTrigger();
            UpdateMissionUI();
        }
        _isProcessingResult = false;
        ClearMonitorText();
    }

    void RetryCurrentStep()
    {
        if (conductor)
        {
            conductor.RetryCurrentTrigger();
            UpdateMissionUI();
        }
        _isProcessingResult = false;
    }

    void UpdateMissionUI()
    {
        if (!missionBoard) return;
        
        missionBoard.ShowSuccessStamp(false);
        missionBoard.UpdateHeader($"< 실습 {_currentStepIndex + 1} 단계 >");

        int targetSliceCount = 0;
        if (tutorialTriggerList != null && _currentStepIndex < tutorialTriggerList.triggers.Length)
        {
            targetSliceCount = tutorialTriggerList.triggers[_currentStepIndex].requiredSliceCount;
        }

        int currentSlices = 0;
        if (conductor != null && conductor.IsJudgingWindow())
        {
            currentSlices = conductor.SliceCount;
        }
        
        int remainCount = Mathf.Max(0, targetSliceCount - currentSlices);
        string icons = "";
        for (int i = 0; i < remainCount; i++) icons += "@ "; 

        string progressText = $"<color={highlightTextColor}>{currentSlices}</color> / {targetSliceCount}";

        string guideText = "";
        switch (_currentStepIndex)
        {
            case 0: 
                guideText = "망설이지 말고\n[직접] 한번 썰어보게나!"; 
                break;
                
            case 1: 
                guideText = "잘했네! 감이 좋군.\n이제 [비트]에 맞춰 썰어보자!"; 
                break;
                
            default: 
                guideText = "박자에 맞춰\n[정확히] 썰어보게!"; 
                break;
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

    void CalculateWageSettings()
    {
        _totalPossibleSlices = 0;
        if (tutorialTriggerList != null)
        {
            foreach (var trigger in tutorialTriggerList.triggers)
                _totalPossibleSlices += trigger.requiredSliceCount;
        }
        _wagePerSlice = (_totalPossibleSlices > 0) ? maxTotalWage / _totalPossibleSlices : 500;
    }

    void UpdateSalesUI()
    {
        if (salesText) salesText.text = $"누적 매출\n[ {_currentEarnedMoney:N0}원 ]";
    }

    void ShowClearMessage() { ShowMonitorText("정산 완료", Color.cyan); }

    void SkipTutorial()
    {
        _tutorialCompleted = true;
        Cleanup();
        if (missionBoard) { missionBoard.UpdateText("< 알림 >", "교육 스킵됨", ""); missionBoard.ShowSuccessStamp(false); }
        if (conductor.OnTutorialSkipped != null) conductor.OnTutorialSkipped.Invoke();
    }

    // 🔥 [수정됨] 최종 합격 멘트 적용
    void CompleteTutorial()
    {
        _tutorialCompleted = true;
        Cleanup();

        string finalMessage = "합격이네.\n자 이제 영업을 시작하자.";

        // 1. 리갈패드(주문서) 업데이트
        if (missionBoard)
        {
            missionBoard.ShowSuccessStamp(true);
            missionBoard.UpdateHeader("< 채용 결과 >");
            // 빨간색 큰 글씨로 '합격!' 표시
            missionBoard.UpdateText("< 채용 결과 >", "<size=180%><color=red>합격!</color></size>", "");
        }
        
        // 2. 모니터 결과창(분홍 패널) 업데이트
        if (finalResultPanel)
        {
            finalResultPanel.SetActive(true);
            if (finalTotalText) finalTotalText.text = $"{_currentEarnedMoney:N0}원";
            if (finalCommentText) 
            { 
                finalCommentText.text = finalMessage; 
                finalCommentText.color = Color.white; 
            }
        }
        
        // 3. 모니터 일반 텍스트 업데이트 (혹시 패널이 안 뜰 경우 대비)
        ShowMonitorText(finalMessage, Color.cyan);
        
        if (conductor.OnTutorialCompleted != null) conductor.OnTutorialCompleted.Invoke();
    }

    void Cleanup()
    {
        if (conductor) { conductor.isTutorialMode = false; conductor.StopAllGuideBeats(); }
        if (salesUIGroup) salesUIGroup.SetActive(false);
        if (radio) { radio.SetTutorialCompleted(true); radio.SetClickable(true); }
    }

    void ShowMonitorText(string text, Color color)
    {
        if (monitorText) { monitorText.text = text; monitorText.color = color; monitorText.gameObject.SetActive(true); }
    }

    void ClearMonitorText()
    {
        if (monitorText) monitorText.text = "";
    }
}
using UnityEngine;
using TMPro; // 텍스트 UI(TextMeshPro) 사용을 위해 필수

public class TutorialController : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;
    public RadioClickable radio;
    
    // ★ 공용 리갈패드 연결
    public MissionBoardUI missionBoard; 

    // ★ 모니터 피드백 텍스트 연결
    [Header("Monitor Text")]
    public TextMeshProUGUI monitorText; 

    [Header("Tutorial Settings")]
    public RhythmTriggerListSO tutorialTriggerList;
    public int requiredSuccessCount = 3; 

    // 내부 변수
    int _currentStepIndex = 0;
    int _successCountThisStep = 0;
    bool _tutorialCompleted = false;
    bool _isProcessingResult = false;
    float _lastSkipInput = -999f;

    // ★ 활성화될 때 자동 시작
    void OnEnable()
    {
        Debug.Log("[TutorialController] 튜토리얼 모드 활성화됨");

        _currentStepIndex = 0;
        _successCountThisStep = 0;
        _tutorialCompleted = false;
        _isProcessingResult = false;

        if (conductor) 
        {
            conductor.OnRoundResult.AddListener(HandleRoundResult);
        }

        StartTutorial();
    }

    void OnDisable()
    {
        if (conductor) 
        {
            conductor.OnRoundResult.RemoveListener(HandleRoundResult);
        }
        Debug.Log("[TutorialController] 튜토리얼 모드 종료");
    }

    void Update()
    {
        if (_tutorialCompleted) return;

        // A 버튼 스킵
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

        // 리듬 게임 설정
        conductor.isTutorialMode = true;
        conductor.data = tutorialTriggerList;
        conductor.StartGame();

        // UI 초기화
        if (missionBoard)
        {
            missionBoard.InitializeUI();
            UpdateMissionUI();
        }

        // 시작 멘트 (모니터)
        ShowMonitorText("가이드 소리에\n집중하세요!", Color.yellow);

        // 1초 뒤 첫 트리거 로직 시작
        Invoke(nameof(StartFirstTrigger), 1.0f);
    }

    void StartFirstTrigger()
    {
        // 컨덕터가 준비되었으면 다음 트리거로 진행
        if (conductor)
        {
            conductor.AdvanceToNextTrigger();
            UpdateMissionUI();
            // 준비 멘트 지우기
            ShowMonitorText("", Color.white);
        }
    }

    void HandleRoundResult(bool success)
    {
        if (_tutorialCompleted || _isProcessingResult) return;
        _isProcessingResult = true;

        if (success)
        {
            _successCountThisStep++;
            
            // [핵심] 성공할 때마다 즉시 UI 갱신 (칼 아이콘 줄어들게!)
            UpdateMissionUI();

            // 성공 피드백 텍스트 (모니터)
            ShowMonitorText("NICE!!", Color.cyan);
            
            if (_successCountThisStep >= requiredSuccessCount)
            {
                if (missionBoard) missionBoard.ShowSuccessStamp(true);
                ShowMonitorText("완벽합니다!", Color.green);
                
                _currentStepIndex++;
                _successCountThisStep = 0;

                // 모든 단계 완료 체크
                if (_currentStepIndex >= tutorialTriggerList.triggers.Length)
                {
                    Invoke(nameof(CompleteTutorial), 2.0f);
                    return;
                }
                Invoke(nameof(MoveToNextStep), 2.0f);
            }
            else
            {
                // 아직 횟수가 남았으면 재시도
                Invoke(nameof(RetryCurrentStep), 1.5f);
                Invoke(nameof(ClearMonitorText), 1.0f);
            }
        }
        else
        {
            // 실패 피드백
            ShowMonitorText("다시 들어보세요!", Color.red);
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
        }
        _isProcessingResult = false;
    }

    // 노트(리갈패드) UI 갱신 로직 (아이콘 자동화)
    void UpdateMissionUI()
    {
        if (!missionBoard) return;
        
        missionBoard.ShowSuccessStamp(false);
        missionBoard.UpdateHeader($"< 실습 {_currentStepIndex + 1} 단계 >");

        // 1. 데이터 가져오기
        int targetSliceCount = 0;
        if (tutorialTriggerList != null && _currentStepIndex < tutorialTriggerList.triggers.Length)
        {
            targetSliceCount = tutorialTriggerList.triggers[_currentStepIndex].requiredSliceCount;
        }

        // 2. 남은 횟수 계산 (전체 목표 - 현재 성공 횟수)
        int remainCount = Mathf.Max(0, targetSliceCount - _successCountThisStep);

        // 3. 칼 아이콘 만들기 (남은 횟수만큼 🔪 반복)
        string knifeIcons = "";
        for (int i = 0; i < remainCount; i++)
        {
            knifeIcons += "🔪 "; 
        }

        // 4. 텍스트 구성
        string highlightNum = $"<color=#FF3333><size=120%>{targetSliceCount}</size></color>"; 
        string guideText = "";

        switch (_currentStepIndex)
        {
            case 0: 
                guideText = $"👂 소리를 듣고\n{highlightNum}번 썰어보세요!"; 
                break;
            case 1: 
                guideText = $"👂 리듬을 타고\n{highlightNum}번 연속 썰기!"; 
                break;
            case 2: 
                guideText = $"👂 빠른 박자입니다.\n{highlightNum}번 집중!"; 
                break;
            default:
                guideText = $"🔪 박자에 맞춰\n{highlightNum}번 써세요!";
                break;
        }

        // 5. 텍스트 아래에 칼 아이콘 붙이기
        guideText += $"\n\n<size=150%>{knifeIcons}</size>";

        // 6. UI 적용 (진행도 숫자는 0으로 숨김)
        missionBoard.UpdateMission(guideText, 0, 0);
    }

    void SkipTutorial()
    {
        _tutorialCompleted = true;
        Cleanup();
        
        if (missionBoard)
        {
            missionBoard.UpdateText("< 알림 >", "교육 스킵됨", "");
            missionBoard.ShowSuccessStamp(false);
        }
        
        if (conductor.OnTutorialSkipped != null) 
            conductor.OnTutorialSkipped.Invoke();
    }

    void CompleteTutorial()
    {
        _tutorialCompleted = true;
        Cleanup();
        
        if (missionBoard)
        {
            missionBoard.ShowSuccessStamp(true);
            missionBoard.UpdateHeader("< 채용 확정 >");
            missionBoard.UpdateText("< 채용 확정 >", "축하합니다!\n정직원이 되셨습니다.", "");
        }
        
        if (conductor.OnTutorialCompleted != null) 
            conductor.OnTutorialCompleted.Invoke();
    }

    void Cleanup()
    {
        if (conductor)
        {
            conductor.isTutorialMode = false;
            conductor.StopAllGuideBeats(); 
        }
        if (radio)
        {
            radio.SetTutorialCompleted(true);
            radio.SetClickable(true);
        }
    }

    // ★ 모니터 텍스트 제어 함수
    void ShowMonitorText(string text, Color color)
    {
        if (monitorText)
        {
            monitorText.text = text;
            monitorText.color = color;
            monitorText.gameObject.SetActive(true);
        }
    }

    void ClearMonitorText()
    {
        if (monitorText) monitorText.text = "";
    }
}
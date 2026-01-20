using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;
    public RadioClickable radio;
    
    // ★ 공용 리갈패드 연결
    public MissionBoardUI missionBoard; 

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
        }
    }

    void HandleRoundResult(bool success)
    {
        if (_tutorialCompleted || _isProcessingResult) return;
        _isProcessingResult = true;

        if (success)
        {
            _successCountThisStep++;
            
            // ★ 미션보드 갱신 (체크박스)
            if (missionBoard)
            {
                string missionName = GetMissionName(_currentStepIndex);
                missionBoard.UpdateMission(missionName, _successCountThisStep, requiredSuccessCount);
            }

            if (_successCountThisStep >= requiredSuccessCount)
            {
                // 단계 클리어 -> 도장 쾅!
                if (missionBoard) missionBoard.ShowSuccessStamp(true);
                
                _currentStepIndex++;
                _successCountThisStep = 0;

                // 다음 단계 확인
                if (_currentStepIndex >= tutorialTriggerList.triggers.Length)
                {
                    Invoke(nameof(CompleteTutorial), 2.0f);
                    return;
                }
                Invoke(nameof(MoveToNextStep), 2.0f);
            }
            else
            {
                Invoke(nameof(RetryCurrentStep), 1.5f);
            }
        }
        else
        {
            // 실패 시 재시도
            Invoke(nameof(RetryCurrentStep), 1.5f);
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
    }

    void RetryCurrentStep()
    {
        if (conductor)
        {
            conductor.RetryCurrentTrigger();
        }
        _isProcessingResult = false;
    }

    void UpdateMissionUI()
    {
        if (!missionBoard) return;
        
        missionBoard.ShowSuccessStamp(false);
        missionBoard.UpdateHeader($"< 실습 {_currentStepIndex + 1} 단계 >");
        
        string missionName = GetMissionName(_currentStepIndex);
        missionBoard.UpdateMission(missionName, 0, requiredSuccessCount);
    }

    string GetMissionName(int index)
    {
        switch (index)
        {
            case 0: return "기본 썰기 (정지)";
            case 1: return "연속 썰기 (이동)";
            case 2: return "빠른 썰기 (심화)";
            default: return $"실전 연습 {index + 1}";
        }
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
            conductor.StopAllGuideBeats(); // 해당 함수가 Conductor에 있어야 함
        }
        if (radio)
        {
            radio.SetTutorialCompleted(true);
            radio.SetClickable(true);
        }
    }
}
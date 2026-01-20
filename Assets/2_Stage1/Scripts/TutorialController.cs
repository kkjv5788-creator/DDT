using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;
    public RadioClickable radio;
    
    // 🔥 기존 UI 대신 MissionBoardUI 사용
    public MissionBoardUI missionBoard; 

    [Header("Tutorial Settings")]
    public RhythmTriggerListSO tutorialTriggerList;
    public int requiredSuccessCount = 3; // 각 단계 3번 성공 필요

    int _currentStepIndex = 0;
    int _successCountThisStep = 0; // 현재 단계에서 성공한 횟수
    bool _tutorialCompleted = false;
    bool _isProcessingResult = false; // 중복 처리 방지

    float _lastSkipInput = -999f;

    void Update()
    {
        if (_tutorialCompleted) return;

        // A 버튼으로 튜토리얼 스킵
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (Time.time - _lastSkipInput > (feedbackSet ? feedbackSet.skipInputCooldown : 0.5f))
            {
                _lastSkipInput = Time.time;
                SkipTutorial();
            }
        }
    }

    void OnEnable()
    {
        if (conductor) conductor.OnRoundResult.AddListener(HandleRoundResult);
    }

    void OnDisable()
    {
        if (conductor) conductor.OnRoundResult.RemoveListener(HandleRoundResult);
    }

    public void StartTutorial()
    {
        if (!conductor || !tutorialTriggerList)
        {
            Debug.LogError("[TutorialController] Missing conductor or tutorialTriggerList.");
            return;
        }

        _currentStepIndex = 0;
        _successCountThisStep = 0;
        _tutorialCompleted = false;
        _isProcessingResult = false;

        // 튜토리얼 모드 활성화
        conductor.isTutorialMode = true;
        conductor.data = tutorialTriggerList;
        conductor.StartGame();

        Debug.Log("[TutorialController] Tutorial started");

        // 🔥 주문서 초기화
        if (missionBoard)
        {
            missionBoard.InitializeUI();
            missionBoard.UpdateHeader("< 수습 교육 >");
            // 아직 첫 미션 시작 전이므로 대기 멘트
            missionBoard.UpdateText("< 수습 교육 >", "준비 중...", "");
        }

        // 첫 번째 트리거 수동 시작
        Invoke(nameof(StartFirstTrigger), 1.0f);
    }

    void StartFirstTrigger()
    {
        if (conductor && conductor.CurrentTriggerIndex < 0)
        {
            Debug.Log("[TutorialController] Starting first trigger manually");
            conductor.AdvanceToNextTrigger();

            // 🔥 첫 미션 UI 표시
            UpdateMissionUI(_currentStepIndex);
        }
    }

    void HandleRoundResult(bool success)
    {
        if (_tutorialCompleted) return;
        if (_isProcessingResult) return; 

        _isProcessingResult = true;

        if (success)
        {
            _successCountThisStep++;
            Debug.Log($"[TutorialController] Step {_currentStepIndex} SUCCESS! ({_successCountThisStep}/{requiredSuccessCount})");

            // 🔥 주문서 업데이트 (체크박스 채우기)
            if (missionBoard)
            {
                string missionName = GetMissionName(_currentStepIndex);
                missionBoard.UpdateMission(missionName, _successCountThisStep, requiredSuccessCount);
            }

            if (_successCountThisStep >= requiredSuccessCount)
            {
                Debug.Log($"[TutorialController] Step {_currentStepIndex} COMPLETED!");

                // 🔥 단계 완료 도장 쾅!
                if (missionBoard) missionBoard.ShowSuccessStamp(true);

                _currentStepIndex++;
                _successCountThisStep = 0;

                // 모든 단계 완료 확인
                if (_currentStepIndex >= tutorialTriggerList.triggers.Length)
                {
                    Invoke(nameof(CompleteTutorial), 2.0f); // 도장 보여줄 시간 줌
                    return;
                }

                // 다음 단계로 이동
                Invoke(nameof(MoveToNextStep), 2.0f);
            }
            else
            {
                // 같은 단계 재시도
                Invoke(nameof(RetryCurrentStep), 1.5f);
            }
        }
        else
        {
            Debug.Log($"[TutorialController] Step {_currentStepIndex} FAILED");
            // 실패 시 재시도 (UI는 그대로 둠)
            Invoke(nameof(RetryCurrentStep), 1.5f);
        }
    }

    void MoveToNextStep()
    {
        if (conductor)
        {
            conductor.AdvanceToNextTrigger();
            // 🔥 다음 단계 UI 갱신 (도장 지우고 새 종이)
            UpdateMissionUI(_currentStepIndex);
        }
        _isProcessingResult = false;
    }

    void RetryCurrentStep()
    {
        if (conductor)
        {
            conductor.RetryCurrentTrigger();
            // 재시도 시 UI는 유지 (도장은 이미 꺼져있음)
        }
        _isProcessingResult = false;
    }

    // 🔥 단계별 미션 이름 반환 헬퍼
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

    // 🔥 주문서 갱신 통합 함수
    void UpdateMissionUI(int index)
    {
        if (!missionBoard) return;

        missionBoard.ShowSuccessStamp(false); // 도장 끄기
        missionBoard.UpdateHeader($"< 실습 {index + 1} 단계 >");
        
        string name = GetMissionName(index);
        missionBoard.UpdateMission(name, 0, requiredSuccessCount);
    }

    void SkipTutorial()
    {
        Debug.Log("[TutorialController] Skipped");
        _tutorialCompleted = true;
        CleanupTutorial();

        // UI 숨김 대신 "스킵됨" 표시
        if (missionBoard)
        {
            missionBoard.UpdateText("< 알림 >", "교육 건너뜀", "");
            missionBoard.ShowSuccessStamp(false);
        }
        
        // 스킵 이벤트 발행
        if (conductor.OnTutorialSkipped != null) conductor.OnTutorialSkipped.Invoke();
    }

    void CompleteTutorial()
    {
        Debug.Log("[TutorialController] Completed");
        _tutorialCompleted = true;
        CleanupTutorial();

        // 🔥 최종 완료 메시지
        if (missionBoard)
        {
            missionBoard.ShowSuccessStamp(true); // 마지막 도장 유지
            missionBoard.UpdateHeader("< 교육 수료 >");
            missionBoard.UpdateText("< 교육 수료 >", "정직원 채용됨", "라디오를 켜세요");
        }
        
        // 완료 이벤트 발행
        if (conductor.OnTutorialCompleted != null) conductor.OnTutorialCompleted.Invoke();
    }

    void CleanupTutorial()
    {
        if (conductor)
        {
            conductor.isTutorialMode = false;
            if (conductor.bgmSource) conductor.bgmSource.Stop();
            conductor.StopAllGuideBeats();
        }

        if (radio)
        {
            radio.SetTutorialCompleted(true);
            radio.SetClickable(true);
        }
    }
    
    // 외부 참조용
    public int GetCurrentStepIndex() => _currentStepIndex;
    public int GetSuccessCount() => _successCountThisStep;
    public int GetTotalSteps() => tutorialTriggerList ? tutorialTriggerList.triggers.Length : 0;
}
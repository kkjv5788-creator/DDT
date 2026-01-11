using System.Diagnostics;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;
    public RadioClickable radio;
    public TutorialUIController tutorialUI; // 🔥 UI 컨트롤러 연결

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
        if (conductor)
        {
            conductor.OnRoundResult.AddListener(HandleRoundResult);
        }
    }

    void OnDisable()
    {
        if (conductor)
        {
            conductor.OnRoundResult.RemoveListener(HandleRoundResult);
        }
    }

    public void StartTutorial()
    {
        if (!conductor || !tutorialTriggerList)
        {
            UnityEngine.Debug.LogError("[TutorialController] Missing conductor or tutorialTriggerList.");
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

        UnityEngine.Debug.Log("[TutorialController] Tutorial started - Need 3 successes per step");

        // 🔥 UI 초기화
        if (tutorialUI)
        {
            tutorialUI.OnTutorialStart();
        }

        // 첫 번째 트리거 수동 시작 (시간 기반 진행 차단되므로)
        Invoke(nameof(StartFirstTrigger), 1.0f);
    }

    void StartFirstTrigger()
    {
        if (conductor && conductor.CurrentTriggerIndex < 0)
        {
            UnityEngine.Debug.Log("[TutorialController] Starting first trigger manually");
            conductor.AdvanceToNextTrigger();

            // 🔥 UI에 현재 단계 정보 전달
            if (tutorialUI && tutorialTriggerList.triggers.Length > 0)
            {
                var trigger = tutorialTriggerList.triggers[0];
                tutorialUI.ShowStepInstruction(_currentStepIndex, requiredSuccessCount, trigger.judgeDuration);
            }
        }
    }

    void HandleRoundResult(bool success)
    {
        if (_tutorialCompleted) return;
        if (_isProcessingResult) return; // 중복 방지

        _isProcessingResult = true; // 처리 시작

        if (success)
        {
            _successCountThisStep++;
            UnityEngine.Debug.Log($"[TutorialController] Step {_currentStepIndex} SUCCESS! ({_successCountThisStep}/{requiredSuccessCount})");

            // 🔥 UI 업데이트 - 성공 카운트 갱신
            if (tutorialUI)
            {
                tutorialUI.UpdateSuccessCount(_currentStepIndex, _successCountThisStep, requiredSuccessCount);
            }

            if (_successCountThisStep >= requiredSuccessCount)
            {
                UnityEngine.Debug.Log($"[TutorialController] Step {_currentStepIndex} COMPLETED!");

                _currentStepIndex++;
                _successCountThisStep = 0;

                // 모든 단계 완료 확인
                if (_currentStepIndex >= tutorialTriggerList.triggers.Length)
                {
                    CompleteTutorial();
                    return;
                }

                // 다음 단계로 이동
                Invoke(nameof(MoveToNextStep), 1.5f);
            }
            else
            {
                // 같은 단계 재시도 (성공 카운트 유지)
                Invoke(nameof(RetryCurrentStep), 1.5f);
            }
        }
        else
        {
            // 실패 시 같은 단계 재시도 (성공 카운트 유지)
            UnityEngine.Debug.Log($"[TutorialController] Step {_currentStepIndex} FAILED (current: {_successCountThisStep}/{requiredSuccessCount})");
            Invoke(nameof(RetryCurrentStep), 1.5f);
        }
    }

    void MoveToNextStep()
    {
        if (conductor)
        {
            UnityEngine.Debug.Log($"[TutorialController] Moving to step {_currentStepIndex}...");
            conductor.AdvanceToNextTrigger();

            // 🔥 UI에 새 단계 정보 전달
            if (tutorialUI && _currentStepIndex < tutorialTriggerList.triggers.Length)
            {
                var trigger = tutorialTriggerList.triggers[_currentStepIndex];
                tutorialUI.ShowStepInstruction(_currentStepIndex, requiredSuccessCount, trigger.judgeDuration);
            }
        }

        _isProcessingResult = false; // 처리 완료
    }

    void RetryCurrentStep()
    {
        if (conductor)
        {
            UnityEngine.Debug.Log($"[TutorialController] Retrying step {_currentStepIndex}...");
            conductor.RetryCurrentTrigger();

            // 🔥 UI에 재시도 정보 전달
            if (tutorialUI && _currentStepIndex < tutorialTriggerList.triggers.Length)
            {
                var trigger = tutorialTriggerList.triggers[_currentStepIndex];
                tutorialUI.ShowStepInstruction(_currentStepIndex, requiredSuccessCount, trigger.judgeDuration);
            }
        }

        _isProcessingResult = false; // 처리 완료
    }

    void SkipTutorial()
    {
        UnityEngine.Debug.Log("[TutorialController] Tutorial skipped by A button!");
        _tutorialCompleted = true;

        if (conductor)
        {
            conductor.isTutorialMode = false;

            // BGM 정지
            if (conductor.bgmSource)
            {
                conductor.bgmSource.Stop();
                UnityEngine.Debug.Log("[TutorialController] Tutorial BGM stopped");
            }
        }

        // 🔥 UI 숨김
        if (tutorialUI)
        {
            tutorialUI.Hide();
        }

        // 라디오 활성화 + 클릭 가능하도록 설정
        if (radio)
        {
            radio.SetTutorialCompleted(true);
            radio.SetClickable(true);
            UnityEngine.Debug.Log("[TutorialController] Radio unlocked and clickable after skip");
        }

        // 스킵 이벤트 발행
        if (conductor.OnTutorialSkipped != null)
            conductor.OnTutorialSkipped.Invoke();
    }

    void CompleteTutorial()
    {
        UnityEngine.Debug.Log("[TutorialController] Tutorial completed! All steps cleared.");
        _tutorialCompleted = true;

        if (conductor)
        {
            conductor.isTutorialMode = false;

            // BGM 정지
            if (conductor.bgmSource)
            {
                conductor.bgmSource.Stop();
                UnityEngine.Debug.Log("[TutorialController] Tutorial BGM stopped");
            }
        }

        // 🔥 UI 완료 표시
        if (tutorialUI)
        {
            tutorialUI.ShowCompletionMessage();
            Invoke(nameof(HideTutorialUI), 3.0f);
        }

        // 라디오 활성화 + 클릭 가능하도록 설정
        if (radio)
        {
            radio.SetTutorialCompleted(true);
            radio.SetClickable(true);
            UnityEngine.Debug.Log("[TutorialController] Radio unlocked and clickable after completion");
        }

        // 완료 이벤트 발행
        if (conductor.OnTutorialCompleted != null)
            conductor.OnTutorialCompleted.Invoke();
    }

    void HideTutorialUI()
    {
        if (tutorialUI)
        {
            tutorialUI.Hide();
        }
    }

    // 🔥 외부에서 현재 진행 상황 조회
    public int GetCurrentStepIndex() => _currentStepIndex;
    public int GetSuccessCount() => _successCountThisStep;
    public int GetTotalSteps() => tutorialTriggerList ? tutorialTriggerList.triggers.Length : 0;
}
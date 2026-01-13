using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUIController : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public Canvas tutorialCanvas; // World Space Canvas
    public TutorialController tutorialController; // 🔥 TutorialController 참조

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;      // "가이드 음이 들리면 3초 안에 3번 자르세요!"
    public TextMeshProUGUI progressText;         // "진행: 2/3"
    public TextMeshProUGUI stateText;            // "준비..." / "자르세요!" / "성공!" / "실패!"
    public TextMeshProUGUI stepCountText;        // "단계: 1/3"
    public UnityEngine.UI.Image progressBarFill;     // 시간 바 (선택사항)

    [Header("Colors")]
    public Color guidingColor = Color.yellow;
    public Color judgingColor = Color.green;
    public Color successColor = Color.cyan;
    public Color failColor = Color.red;
    public Color completionColor = new Color(1f, 0.8f, 0f); // 금색

    [Header("Animation")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.3f;

    CanvasGroup _canvasGroup;
    float _fadeTimer;
    bool _isFading;
    bool _targetVisible;
    int _lastTriggerIndex = -1; // 🔥 트리거 인덱스 변경 감지용

    void Awake()
    {
        // CanvasGroup 추가 (페이드 인/아웃용)
        if (!_canvasGroup)
        {
            _canvasGroup = tutorialCanvas.gameObject.AddComponent<CanvasGroup>();
        }

        // 초기 상태: 숨김
        _canvasGroup.alpha = 0f;
        tutorialCanvas.gameObject.SetActive(false);
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

    void Update()
    {
        if (!conductor || !conductor.isTutorialMode)
        {
            // 튜토리얼 모드 아니면 숨김
            if (tutorialCanvas.gameObject.activeSelf)
            {
                tutorialCanvas.gameObject.SetActive(false);
            }
            return;
        }

        // 🔥 트리거 인덱스 변경 감지 및 단계 업데이트
        if (conductor.CurrentTriggerIndex != _lastTriggerIndex)
        {
            _lastTriggerIndex = conductor.CurrentTriggerIndex;
            UpdateStepCount();
        }

        // 페이드 처리
        HandleFade();

        // 상태에 따라 UI 업데이트
        UpdateUI();

        // 시간 바 업데이트 (선택사항)
        UpdateProgressBar();
    }

    void HandleFade()
    {
        if (!_isFading) return;

        _fadeTimer += Time.deltaTime;

        if (_targetVisible)
        {
            // Fade In
            float t = Mathf.Clamp01(_fadeTimer / fadeInDuration);
            _canvasGroup.alpha = t;

            if (t >= 1f)
            {
                _isFading = false;
            }
        }
        else
        {
            // Fade Out
            float t = Mathf.Clamp01(_fadeTimer / fadeOutDuration);
            _canvasGroup.alpha = 1f - t;

            if (t >= 1f)
            {
                _isFading = false;
                tutorialCanvas.gameObject.SetActive(false);
            }
        }
    }

    void UpdateUI()
    {
        if (conductor.CurrentTriggerIndex < 0) return;

        var trigger = conductor.GetCurrentTrigger();
        if (trigger == null) return;

        // 상태별 텍스트 & 색상
        switch (conductor.State)
        {
            case RhythmConductor.RhythmState.Guiding:
                UpdateGuidingState(trigger);
                break;

            case RhythmConductor.RhythmState.Judging:
                UpdateJudgingState(trigger);
                break;

            case RhythmConductor.RhythmState.Result:
                // Result는 HandleRoundResult에서 처리
                break;

            case RhythmConductor.RhythmState.Waiting:
                // Waiting 상태에서는 UI 숨김
                if (_targetVisible)
                {
                    FadeOut();
                }
                break;
        }
    }

    void UpdateGuidingState(RhythmTriggerListSO.Trigger trigger)
    {
        if (!_targetVisible)
        {
            FadeIn();
        }

        // 가이드 음이 들릴 때
        if (instructionText)
        {
            float judgeDuration = trigger.judgeDuration;
            instructionText.text = $"가이드 음이 들리면\n{judgeDuration:F1}초 안에 {trigger.requiredSliceCount}번 자르세요!";
            instructionText.color = guidingColor;
        }

        if (stateText)
        {
            stateText.text = "준비...";
            stateText.color = guidingColor;
        }
    }

    void UpdateJudgingState(RhythmTriggerListSO.Trigger trigger)
    {
        if (!_targetVisible)
        {
            FadeIn();
        }

        // 자르는 중
        if (instructionText)
        {
            int remaining = conductor.RequiredSliceCount - conductor.SliceCount;
            instructionText.text = $"빠르게 자르세요!\n남은 횟수: {remaining}";
            instructionText.color = judgingColor;
        }

        if (stateText)
        {
            stateText.text = "자르세요!";
            stateText.color = judgingColor;
        }

        // 🔥 진행 상황 실시간 업데이트
        if (progressText)
        {
            progressText.text = $"진행: {conductor.SliceCount} / {conductor.RequiredSliceCount}";
        }
    }

    void HandleRoundResult(bool success)
    {
        if (!conductor.isTutorialMode) return;

        // 결과 표시
        if (instructionText)
        {
            if (success)
            {
                instructionText.text = "성공!";
                instructionText.color = successColor;
            }
            else
            {
                instructionText.text = "실패... 다시 시도하세요!";
                instructionText.color = failColor;
            }
        }

        if (stateText)
        {
            if (success)
            {
                stateText.text = "성공!";
                stateText.color = successColor;
            }
            else
            {
                stateText.text = "실패!";
                stateText.color = failColor;
            }
        }

        // 잠시 후 페이드 아웃은 TutorialController에서 관리
    }

    void UpdateProgressBar()
    {
        if (!progressBarFill) return;
        if (conductor.State != RhythmConductor.RhythmState.Judging) return;

        var trigger = conductor.GetCurrentTrigger();
        if (trigger == null) return;

        // 남은 시간 비율
        float elapsed = Time.time - (conductor.BgmTime - trigger.judgeDuration);
        float progress = Mathf.Clamp01(elapsed / trigger.judgeDuration);

        progressBarFill.fillAmount = 1f - progress;
    }

    void UpdateStepCount()
    {
        if (!stepCountText) return;

        // 🔥 conductor의 CurrentTriggerIndex를 직접 사용하여 실시간 업데이트
        int currentStep = conductor.CurrentTriggerIndex + 1;
        int totalSteps = 0;

        // data에서 총 단계 수 가져오기
        if (conductor.data && conductor.data.triggers != null)
        {
            totalSteps = conductor.data.triggers.Length;
        }

        // tutorialController가 있으면 그것의 정보도 활용 (옵션)
        if (tutorialController)
        {
            int controllerTotalSteps = tutorialController.GetTotalSteps();
            if (controllerTotalSteps > 0)
            {
                totalSteps = controllerTotalSteps;
            }
        }

        stepCountText.text = $"단계: {currentStep} / {totalSteps}";
    }

    // 🔥 TutorialController에서 호출할 공개 메서드들

    public void OnTutorialStart()
    {
        UnityEngine.Debug.Log("[TutorialUIController] Tutorial UI started");
        _lastTriggerIndex = -1; // 🔥 초기화
        FadeIn();
        UpdateStepCount(); // 🔥 시작 시 즉시 업데이트
    }

    public void ShowStepInstruction(int stepIndex, int requiredCount, float judgeDuration)
    {
        FadeIn();

        if (instructionText)
        {
            instructionText.text = $"단계 {stepIndex + 1}\n가이드 음이 들리면 {judgeDuration:F1}초 안에 {requiredCount}번 자르세요!";
            instructionText.color = guidingColor;
        }

        if (stateText)
        {
            stateText.text = "준비...";
            stateText.color = guidingColor;
        }

        if (progressText)
        {
            progressText.text = $"진행: 0 / {requiredCount}";
        }

        // 🔥 단계 표시도 즉시 업데이트
        UpdateStepCount();
    }

    public void UpdateSuccessCount(int stepIndex, int successCount, int requiredCount)
    {
        if (progressText)
        {
            progressText.text = $"성공: {successCount} / {requiredCount}";
        }

        UnityEngine.Debug.Log($"[TutorialUIController] Step {stepIndex} - Success count: {successCount}/{requiredCount}");
    }

    public void ShowCompletionMessage()
    {
        FadeIn();

        if (instructionText)
        {
            instructionText.text = "튜토리얼 완료!\n라디오를 클릭해서\n메인 게임을 시작하세요!";
            instructionText.color = completionColor;
        }

        if (stateText)
        {
            stateText.text = "완료!";
            stateText.color = completionColor;
        }

        if (progressText)
        {
            progressText.text = "";
        }

        // 🔥 완료 시에는 단계 표시 숨기기 (선택사항)
        if (stepCountText)
        {
            stepCountText.text = "";
        }

        UnityEngine.Debug.Log("[TutorialUIController] Tutorial completion message shown");
    }

    public void FadeIn()
    {
        if (!tutorialCanvas.gameObject.activeSelf)
        {
            tutorialCanvas.gameObject.SetActive(true);
        }

        _targetVisible = true;
        _isFading = true;
        _fadeTimer = 0f;
    }

    public void FadeOut()
    {
        _targetVisible = false;
        _isFading = true;
        _fadeTimer = 0f;
    }

    public void Hide()
    {
        FadeOut();
    }
}
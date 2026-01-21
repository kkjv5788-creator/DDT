using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameState_st2;

public class TutorialManager_st2 : MonoBehaviour
{
    public enum TutorialStage
    {
        Telegraph,
        Pop,
        Catch,
        BagSwap,
        Sync2Pattern,
        Run3Pattern,
        Complete
    }

    [Header("현재 상태")]
    public TutorialStage currentStage = TutorialStage.Telegraph;

    private int telegraphCount = 0;
    private int popCount = 0;
    private int goodOrBetterCount = 0; // ✅ 수정: Good 이상만 카운트
    private bool hasSeenBagSwap = false;
    private bool hasSuccessSync2 = false;
    private bool hasSuccessRun3 = false;

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI skipText;

    private PatternType_st2 currentPatternType;

    void Start()
    {
        skipText.text = "[Press A to Skip]";
        ShowStageInstruction(currentStage);
    }

    void Update()
    {
        if (GameFlowController_st2.Instance.CurrentState != GameStatest2.Tutorial) return;

        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            SkipTutorial();
            return;
        }

        CheckStageProgress();
    }

    void CheckStageProgress()
    {
        switch (currentStage)
        {
            case TutorialStage.Telegraph:
                if (telegraphCount >= 2)
                    AdvanceToNextStage();
                break;

            case TutorialStage.Pop:
                if (popCount >= 2)
                    AdvanceToNextStage();
                break;

            case TutorialStage.Catch:
                // ✅ 수정: Good 이상 2회 요구
                if (goodOrBetterCount >= 2)
                    AdvanceToNextStage();
                break;

            case TutorialStage.BagSwap:
                if (hasSeenBagSwap)
                    AdvanceToNextStage();
                break;

            case TutorialStage.Sync2Pattern:
                if (hasSuccessSync2)
                    AdvanceToNextStage();
                break;

            case TutorialStage.Run3Pattern:
                if (hasSuccessRun3)
                    AdvanceToNextStage();
                break;
        }
    }

    void AdvanceToNextStage()
    {
        currentStage++;

        if (currentStage == TutorialStage.Complete)
        {
            CompleteTutorial();
        }
        else
        {
            ShowStageInstruction(currentStage);
        }
    }

    void ShowStageInstruction(TutorialStage stage)
    {
        switch (stage)
        {
            case TutorialStage.Telegraph:
                instructionText.text = "Listen to the 'Telegraph' sound!";
                progressText.text = $"Progress: {telegraphCount}/2";
                break;

            case TutorialStage.Pop:
                instructionText.text = "Watch the fish 'Pop' out!";
                progressText.text = $"Progress: {popCount}/2";
                break;

            case TutorialStage.Catch:
                // ✅ 수정: Good 이상 2회 요구 안내
                instructionText.text = "Catch fish with Good or Perfect!";
                progressText.text = $"Good+ Catches: {goodOrBetterCount}/2";
                break;

            case TutorialStage.BagSwap:
                instructionText.text = "Collect 3 fish to swap bag!";
                progressText.text = $"Catches needed for swap";
                break;

            case TutorialStage.Sync2Pattern:
                instructionText.text = "Try Sync2 pattern! (땡땡)";
                progressText.text = "Catch at least 1!";
                break;

            case TutorialStage.Run3Pattern:
                instructionText.text = "Try Run3 pattern! (딸랑)";
                progressText.text = "Catch at least 1!";
                break;
        }
    }

    public void OnTelegraphPlayed()
    {
        if (currentStage == TutorialStage.Telegraph)
        {
            telegraphCount++;
            progressText.text = $"Progress: {telegraphCount}/2";
        }
    }

    public void OnPopPlayed()
    {
        if (currentStage == TutorialStage.Pop)
        {
            popCount++;
            progressText.text = $"Progress: {popCount}/2";
        }
    }

    public void OnCatchSuccess(JudgeResult_st2 result)
    {
        // ✅ 수정: Good 이상만 처리
        if (result == JudgeResult_st2.Perfect || result == JudgeResult_st2.Good)
        {
            if (currentStage == TutorialStage.Catch)
            {
                goodOrBetterCount++;
                progressText.text = $"Good+ Catches: {goodOrBetterCount}/2";
            }
            else if (currentStage == TutorialStage.BagSwap)
            {
                // BagSwap은 OnBagSwapped 이벤트로 처리
            }
            else if (currentStage == TutorialStage.Sync2Pattern && currentPatternType == PatternType_st2.Sync2)
            {
                hasSuccessSync2 = true;
                progressText.text = "Sync2 Success!";
            }
            else if (currentStage == TutorialStage.Run3Pattern && currentPatternType == PatternType_st2.Run3)
            {
                hasSuccessRun3 = true;
                progressText.text = "Run3 Success!";
            }
        }
    }

    public void OnBagSwapped()
    {
        if (currentStage == TutorialStage.BagSwap)
        {
            hasSeenBagSwap = true;
            progressText.text = "Bag Swapped!";
        }
    }

    public void OnPatternQueued(PatternType_st2 type)
    {
        currentPatternType = type;
    }

    void SkipTutorial()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        GameFlowController_st2.Instance.TransitionToPlaying();
    }

    void CompleteTutorial()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        GameFlowController_st2.Instance.TransitionToPlaying();
    }

    // ✅ 추가: HUD에서 현재 진행 상황 문자열로 제공
    public string GetCurrentStageInfo()
    {
        switch (currentStage)
        {
            case TutorialStage.Telegraph:
                return $"Telegraph {telegraphCount}/2";
            case TutorialStage.Pop:
                return $"Pop {popCount}/2";
            case TutorialStage.Catch:
                return $"Catch {goodOrBetterCount}/2";
            case TutorialStage.BagSwap:
                return "BagSwap Wait";
            case TutorialStage.Sync2Pattern:
                return hasSuccessSync2 ? "Sync2 Done" : "Sync2 Try";
            case TutorialStage.Run3Pattern:
                return hasSuccessRun3 ? "Run3 Done" : "Run3 Try";
            case TutorialStage.Complete:
                return "Complete";
            default:
                return "Unknown";
        }
    }
}

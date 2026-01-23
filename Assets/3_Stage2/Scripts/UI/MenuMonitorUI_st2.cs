using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameState_st2;
using UnityEngine.Pool;
using System;

public class MenuMonitorUI_st2 : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI wageText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI nextPatternText;
    public TextMeshProUGUI judgeFeedbackText;
    public TextMeshProUGUI bagCountText;
    public TextMeshProUGUI bagSwapText;
    public TextMeshProUGUI songTimeText;
    public TextMeshProUGUI filledBagsText;

    private Coroutine judgeFeedbackCoroutine;
    private Coroutine bagSwapCoroutine;

    public void Initialize(
        EconomySystem_st2 economy,
        BagManager_st2 bag,
        PatternDirector_st2 pattern,
        JudgeSystem_st2 judge)
    {
        economy.OnWageChanged += UpdateWage;
        bag.OnBagSwapped += ShowBagSwap;
        bag.OnItemAdded += UpdateBagCount;
        pattern.OnPatternQueued += UpdateNextPattern;
        judge.OnJudgeResult += ShowJudgeFeedback;
    }

    void UpdateWage(int wage)
    {
        if (wageText != null)
            wageText.text = $"오늘 일급: {wage:N0}원";
    }

    void UpdateNextPattern(PatternType_st2 type)
    {
        if (nextPatternText == null) return;

        string patternName = type switch
        {
            PatternType_st2.Normal => "NORMAL",
            PatternType_st2.Sync2 => "SYNC2 (띠링)",
            PatternType_st2.Run3 => "RUN3 (딸랑)",
            _ => "NORMAL"
        };

        nextPatternText.text = $"NEXT: {patternName}";
    }

    void ShowJudgeFeedback(JudgeResult_st2 result, float delta)
    {
        if (judgeFeedbackText == null) return;

        string text = result switch
        {
            JudgeResult_st2.Perfect => "+100",
            JudgeResult_st2.Good => "+0",
            JudgeResult_st2.Miss => "MISS",
            _ => ""
        };

        if (judgeFeedbackCoroutine != null)
            StopCoroutine(judgeFeedbackCoroutine);

        judgeFeedbackCoroutine = StartCoroutine(ShowTemporaryText(judgeFeedbackText, text, 0.4f));
    }

    void UpdateBagCount(int count)
    {
        if (bagCountText != null)
            bagCountText.text = $"BAG: {count} / 3";
    }

    void ShowBagSwap()
    {
        if (bagSwapText == null) return;

        if (bagSwapCoroutine != null)
            StopCoroutine(bagSwapCoroutine);

        bagSwapCoroutine = StartCoroutine(ShowTemporaryText(bagSwapText, "BAG SWAP!", 0.6f));
    }

    public void UpdateSongTime(float current, float total)
    {
        if (songTimeText == null) return;

        int currMin = (int)(current / 60);
        int currSec = (int)(current % 60);
        int totalMin = (int)(total / 60);
        int totalSec = (int)(total % 60);

        songTimeText.text = $"SONG: {currMin:00}:{currSec:00} / {totalMin:00}:{totalSec:00}";
    }

    public void UpdateFilledBags(int count)
    {
        if (filledBagsText != null)
            filledBagsText.text = $"FILLED BAGS: {count}";
    }

    // ✅ 수정: 정상 구현으로 교체
    public void UpdateState(GameStatest2 currentState)
    {
        if (stateText == null) return;

        string stateName = currentState switch
        {
            GameStatest2.MainMenu => "MAIN MENU",
            GameStatest2.Tutorial => "TUTORIAL",
            GameStatest2.Playing => "PLAYING",
            GameStatest2.Paused => "PAUSED",
            GameStatest2.Result => "RESULT",
            _ => "UNKNOWN"
        };

        stateText.text = $"STATE: {stateName}";
    }

    IEnumerator ShowTemporaryText(TextMeshProUGUI textElement, string message, float duration)
    {
        textElement.text = message;
        textElement.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        textElement.gameObject.SetActive(false);
    }
}
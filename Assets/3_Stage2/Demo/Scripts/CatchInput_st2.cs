using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using static GameState_st2;

public class CatchInput_st2 : MonoBehaviour
{
    [Header("참조")]
    public HandCatchSensor_st2 sensor;
    public OVRInput.Controller handController;
    public Transform handTransform;

    [Header("사운드")]
    public AudioSource snapAudioSource;
    public AudioClip snapSoundClip;

    private JudgeSystem_st2 judgeSystem;
    private BagManager_st2 bagManager;
    private EconomySystem_st2 economySystem;

    // ✅ 추가: 마지막 판정/손 기록 (HUD용)
    public JudgeResult_st2 lastJudgeResult;
    public string lastHandName;

    public void Initialize(JudgeSystem_st2 judge, BagManager_st2 bag, EconomySystem_st2 economy, MoldController_st2[] moldControllers)
    {
        judgeSystem = judge;
        bagManager = bag;
        economySystem = economy;

        // 초기값 설정
        lastJudgeResult = JudgeResult_st2.Miss;
        lastHandName = handController == OVRInput.Controller.LTouch ? "L" : "R";
    }

    void Update()
    {
        if (GameFlowController_st2.Instance.CurrentState != GameStatest2.Playing) return;
        if (GameFlowController_st2.Instance.isEndingTriggered) return;

        ProcessHandInput();
    }

    void ProcessHandInput()
    {
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, handController))
            return;

        var snapshotTarget = sensor.currentTarget;
        if (snapshotTarget == null) return;
        if (snapshotTarget.isResolved) return;

        if (snapshotTarget.assignedHand == null)
        {
            snapshotTarget.assignedHand = handController;
        }

        if (snapshotTarget.assignedHand != handController)
            return;

        double catchTime = AudioSettings.dspTime;
        JudgeResult_st2 result = judgeSystem.PerformJudge(snapshotTarget, catchTime);

        // ✅ 마지막 판정 기록
        lastJudgeResult = result;
        lastHandName = handController == OVRInput.Controller.LTouch ? "L" : "R";

        if (result == JudgeResult_st2.Perfect || result == JudgeResult_st2.Good)
        {
            OnCatchSuccess(snapshotTarget, result);
        }
        else
        {
            OnCatchMiss(snapshotTarget);
        }
    }

    // ✅ 수정: 1프레임 스냅 보장
    void OnCatchSuccess(FishCatchToken_st2 fish, JudgeResult_st2 result)
    {
        fish.OnCaught();

        // 손으로 스냅
        fish.transform.position = handTransform.position;
        fish.transform.SetParent(handTransform);

        // ✅ "착" 사운드 재생
        if (snapAudioSource != null && snapSoundClip != null)
        {
            snapAudioSource.PlayOneShot(snapSoundClip);
        }

        string feedbackText = result == JudgeResult_st2.Perfect ? "PERFECT" : "GOOD";
        FeedbackManager_st2.Instance?.ShowJudgeFeedback(fish.transform.position, feedbackText);

        bagManager.AddItem();

        // ✅ 1프레임 후 풀로 반환 (손에 붙은 모습 보장)
        StartCoroutine(ReleaseFishAfterFrame(fish));
    }

    // ✅ 추가: 1프레임 대기 후 풀 반환
    IEnumerator ReleaseFishAfterFrame(FishCatchToken_st2 fish)
    {
        yield return null; // 1프레임 대기

        if (fish.ownerMold != null)
        {
            fish.ownerMold.ReleaseFish(fish);
        }
    }

    void OnCatchMiss(FishCatchToken_st2 fish)
    {
        FeedbackManager_st2.Instance?.ShowJudgeFeedback(fish.transform.position, "MISS");

        // ✅ Miss는 즉시 삭제하지 않고 낙하 유지
        // ReleaseFish 호출 제거 - 바닥에서 제거됨
    }
}
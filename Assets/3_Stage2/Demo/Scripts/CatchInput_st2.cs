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

    private JudgeSystem_st2 judgeSystem;
    private BagManager_st2 bagManager;
    private EconomySystem_st2 economySystem;

    public void Initialize(JudgeSystem_st2 judge, BagManager_st2 bag, EconomySystem_st2 economy, MoldController_st2[] moldControllers)
    {
        judgeSystem = judge;
        bagManager = bag;
        economySystem = economy;
    }

    void Update()
    {
        if (GameFlowController_st2.Instance.CurrentState != GameStatest2.Playing) return;
        if (GameFlowController_st2.Instance.isEndingTriggered) return; // 종료 시 입력 차단

        ProcessHandInput();
    }

    void ProcessHandInput()
    {
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, handController))
            return;

        var snapshotTarget = sensor.currentTarget;
        if (snapshotTarget == null) return;
        if (snapshotTarget.isResolved) return;

        // assignedHand 확정
        if (snapshotTarget.assignedHand == null)
        {
            snapshotTarget.assignedHand = handController;
        }

        // 다른 손이 할당되어 있으면 무시
        if (snapshotTarget.assignedHand != handController)
            return;

        // 판정 진행
        double catchTime = AudioSettings.dspTime;
        JudgeResult_st2 result = judgeSystem.PerformJudge(snapshotTarget, catchTime);

        // ✅ 제거: economySystem.ApplyJudgeResult(result);
        // 이제 GameFlowController에서 OnJudgeResult 이벤트로 자동 처리됨

        // 처리
        if (result == JudgeResult_st2.Perfect || result == JudgeResult_st2.Good)
        {
            OnCatchSuccess(snapshotTarget, result);
        }
        else
        {
            OnCatchMiss(snapshotTarget);
        }
    }

    // ✅ 수정: result 매개변수 추가
    void OnCatchSuccess(FishCatchToken_st2 fish, JudgeResult_st2 result)
    {
        // 손으로 스냅
        fish.transform.position = handTransform.position;
        fish.transform.SetParent(handTransform);

        // ✅ 수정: 정확한 피드백 표시
        string feedbackText = result == JudgeResult_st2.Perfect ? "PERFECT" : "GOOD";
        FeedbackManager_st2.Instance?.ShowJudgeFeedback(fish.transform.position, feedbackText);

        // 봉투에 추가
        bagManager.AddItem();

        // ✅ 수정: ownerMold로만 릴리즈
        if (fish.ownerMold != null)
        {
            fish.ownerMold.ReleaseFish(fish);
        }
    }

    void OnCatchMiss(FishCatchToken_st2 fish)
    {
        // 피드백
        FeedbackManager_st2.Instance?.ShowJudgeFeedback(fish.transform.position, "MISS");

        // ✅ 수정: ownerMold로만 릴리즈
        if (fish.ownerMold != null)
        {
            fish.ownerMold.ReleaseFish(fish);
        }
    }
}
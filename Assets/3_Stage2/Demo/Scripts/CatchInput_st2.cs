using UnityEngine;
using System.Collections;
using static GameState_st2;

public class CatchInput_st2 : MonoBehaviour
{
    [Header("참조")]
    public HandCatchSensor_st2 sensor;
    public OVRInput.Controller handController;
    public Transform handTransform; // 있어도 되고 없어도 됨(아래는 센서 기준 스냅)

    [Header("사운드")]
    public AudioSource snapAudioSource;
    public AudioClip snapSoundClip;

    [Header("스냅 연출")]
    public float snapHoldSeconds = 0.2f; // ✅ 튜토리얼과 동일하게 0.2초

    private JudgeSystem_st2 judgeSystem;
    private BagManager_st2 bagManager;
    private EconomySystem_st2 economySystem;

    // HUD용
    public JudgeResult_st2 lastJudgeResult = JudgeResult_st2.Miss;
    public string lastHandName;
    public bool hasJudgedOnce = false;

    public void Initialize(JudgeSystem_st2 judge, BagManager_st2 bag, EconomySystem_st2 economy, MoldController_st2[] moldControllers)
    {
        judgeSystem = judge;
        bagManager = bag;
        economySystem = economy;

        lastJudgeResult = JudgeResult_st2.Miss;
        lastHandName = handController == OVRInput.Controller.LTouch ? "L" : "R";
        hasJudgedOnce = false;
    }

    void Update()
    {
        var gf = GameFlowController_st2.Instance;
        if (gf == null) return;

        if (gf.CurrentState != GameStatest2.Playing) return;
        if (gf.isEndingTriggered) return;

        ProcessHandInput();
    }

    void ProcessHandInput()
    {
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, handController))
            return;

        var target = sensor != null ? sensor.currentTarget : null;
        if (target == null) return;
        if (target.isResolved) return;

        // ✅ 튜토리얼처럼 단순 처리(assignedHand 잠금 제거)
        double catchTime = AudioSettings.dspTime;
        JudgeResult_st2 result = judgeSystem.PerformJudge(target, catchTime);

        lastJudgeResult = result;
        lastHandName = handController == OVRInput.Controller.LTouch ? "L" : "R";
        hasJudgedOnce = true;

        if (result == JudgeResult_st2.Perfect || result == JudgeResult_st2.Good)
            OnCatchSuccess(target, result);
        else
            OnCatchMiss(target);
    }

    void OnCatchSuccess(FishCatchToken_st2 fish, JudgeResult_st2 result)
    {
        if (fish == null) return;

        fish.OnCaught();

        Vector3 originalPos = fish.transform.position;

        // ✅ 튜토리얼과 동일하게 “센서에 스냅” (센서 축이 가장 안정적)
        Transform snapParent = (sensor != null) ? sensor.transform : (handTransform != null ? handTransform : transform);

        fish.transform.position = snapParent.position;
        fish.transform.SetParent(snapParent, true);

        if (snapAudioSource != null && snapSoundClip != null)
            snapAudioSource.PlayOneShot(snapSoundClip);

        string feedbackText = result == JudgeResult_st2.Perfect ? "PERFECT" : "GOOD";
        FeedbackManager_st2.Instance?.ShowJudgeFeedback(originalPos, feedbackText);

        // ✅ 0.2초 후: 봉투 비주얼 추가 + 풀 반환(버전 B)
        StartCoroutine(HoldThenBagVisualAndRelease(fish));
    }

    IEnumerator HoldThenBagVisualAndRelease(FishCatchToken_st2 fish)
    {
        if (fish == null) yield break;

        var owner = fish.ownerMold; // 미리 캡처

        yield return new WaitForSeconds(snapHoldSeconds);

        if (fish == null || fish.gameObject == null || !fish.gameObject.activeInHierarchy)
            yield break;

        // 1) 봉투에 “비주얼” 1개 추가 (실물 연동 X)
        bagManager?.AddItem();

        // 2) 손(센서)에서 떼기
        fish.transform.SetParent(null, true);

        // 3) 실물은 풀로 반환(안정성 최우선)
        if (owner != null) owner.ReleaseFish(fish);
        else fish.gameObject.SetActive(false); // 최후 안전장치
    }

    void OnCatchMiss(FishCatchToken_st2 fish)
    {
        if (fish == null) return;
        FeedbackManager_st2.Instance?.ShowJudgeFeedback(fish.transform.position, "MISS");
        // 바닥 드랍 MISS는 FishCatchToken 쪽에서 처리(원하면 거기에도 MISS 피드백 추가)
    }
}

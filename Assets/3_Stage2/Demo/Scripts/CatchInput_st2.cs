using System.Collections;
using UnityEngine;
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

    [Header("스냅 연출")]
    public float snapHoldSeconds = 0.2f;

    [Header("봉투에 넣을 비주얼 프리팹")]
    public GameObject bagFishVisualPrefab;

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

        double catchTime = AudioSettings.dspTime;

        // ✅ JudgeSystem을 통해 판정 수행 (통계 자동 업데이트 + 이벤트 발행)
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

        // 센서(손)로 스냅
        Transform snapParent = (sensor != null) ? sensor.transform : (handTransform != null ? handTransform : transform);

        fish.transform.position = snapParent.position;
        fish.transform.SetParent(snapParent, true);

        if (snapAudioSource != null && snapSoundClip != null)
            snapAudioSource.PlayOneShot(snapSoundClip);

        string feedbackText = result == JudgeResult_st2.Perfect ? "PERFECT" : "GOOD";
        FeedbackManager_st2.Instance?.ShowJudgeFeedback(originalPos, feedbackText);

        // ✅ 튜토리얼과 동일: 0.2초 붙였다가 → 봉투 추가 → 풀 반환
        StartCoroutine(SnapHoldThenBagAndRelease(fish));
    }

    // ✅ 튜토리얼과 완전 동일한 로직
    IEnumerator SnapHoldThenBagAndRelease(FishCatchToken_st2 fish)
    {
        if (fish == null) yield break;

        var owner = fish.ownerMold;

        // ✅ 0.2초 홀드 (튜토리얼과 동일)
        yield return new WaitForSeconds(snapHoldSeconds);

        // 안전 체크
        if (fish == null || fish.gameObject == null || !fish.gameObject.activeInHierarchy)
        {
            UnityEngine.Debug.LogWarning("Fish was destroyed during hold");
            yield break;
        }

        // ✅ 봉투에 비주얼 추가 (BagManager가 3개 제한 자동 처리)
        if (bagManager != null)
        {
            bagManager.AddItem(bagFishVisualPrefab);
            UnityEngine.Debug.Log($"✅ Visual fish added to bag");
        }

        // ✅ 손에서 떼기
        fish.transform.SetParent(null, true);

        // ✅ 실물 붕어빵은 풀로 반환
        if (owner != null)
        {
            owner.ReleaseFish(fish);
            UnityEngine.Debug.Log($"✅ Real fish returned to pool");
        }
        else
        {
            fish.gameObject.SetActive(false);
            UnityEngine.Debug.LogWarning("Fish has no owner, deactivated instead");
        }
    }

    void OnCatchMiss(FishCatchToken_st2 fish)
    {
        if (fish == null) return;

        FeedbackManager_st2.Instance?.ShowJudgeFeedback(fish.transform.position, "MISS");

        // ✅ 혹시라도 부모에 붙어있을 가능성 대비: 무조건 떨어지는 상태 보장
        fish.transform.SetParent(null, true);

        var rb = fish.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}
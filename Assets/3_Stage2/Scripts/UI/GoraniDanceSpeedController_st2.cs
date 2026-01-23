using System.Collections.Generic;
using UnityEngine;
using static GameState_st2;

public class GoraniDanceSpeedByJudge_st2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Stage2EventHub_st2 hub;
    [SerializeField] private Transform goraniRoot;
    [SerializeField] private Animator[] goraniAnimators;

    [Header("Speed Range")]
    [SerializeField, Range(0f, 20f)] private float minSpeed = 0.4f;
    [SerializeField, Range(0f, 20f)] private float maxSpeed = 20.0f;
    [SerializeField, Range(0f, 20f)] private float startSpeed = 1.0f;

    [Header("Smoothing")]
    [SerializeField, Range(0f, 30f)] private float smooth = 8f;

    [Header("Success Gain (Add only)")]
    [SerializeField] private float goodBaseGain = 0.18f;
    [SerializeField] private float perfectBaseGain = 0.28f;

    [Tooltip("연속 성공 가중치(선형 더하기). 예: 3연속이면 base + bonus*(3-1)")]
    [SerializeField] private float goodStreakBonus = 0.07f;
    [SerializeField] private float perfectStreakBonus = 0.10f;

    [Header("Miss Penalty (Add only)")]
    [SerializeField] private float missBaseLoss = 0.22f;

    [Tooltip("연속 미스 가중치(선형 더하기). 예: 4연속이면 base + bonus*(4-1)")]
    [SerializeField] private float missStreakBonus = 0.12f;

    [Header("Miss Source")]
    [Tooltip("GroundMiss/Timeout도 미스로 속도 하락에 반영할지")]
    [SerializeField] private bool countGroundMissAsMiss = true;

    [Tooltip("성공한 fish는 이 시간 동안 GroundMiss/Timeout로 미스 처리하지 않음(중복 방지 핵심)")]
    [SerializeField] private float ignoreGroundMissAfterSuccessSeconds = 10f;

    [Header("Duplicate Guard")]
    [SerializeField] private float judgedCacheSeconds = 0.8f;

    [Header("Apply")]
    [Tooltip("다른 스크립트가 speed=1 덮어써도 이기려면 TRUE")]
    [SerializeField] private bool forceApplyInLateUpdate = true;

    [Header("Debug (Read Only)")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float targetSpeed;
    [SerializeField] private int successStreak;
    [SerializeField] private int missStreak;
    [SerializeField] private int animatorCount;

    // fish id -> last judge time(중복 방지)
    private readonly Dictionary<int, float> _recentJudged = new();
    // fish id -> last success time(성공 후 GroundMiss 무시)
    private readonly Dictionary<int, float> _recentSuccess = new();
    private readonly List<int> _removeKeys = new();

    private void Reset()
    {
        if (!hub) hub = GetComponent<Stage2EventHub_st2>();
        AutoCollectAnimators();
    }

    private void Awake()
    {
        if (!hub) hub = GetComponent<Stage2EventHub_st2>();
        AutoCollectAnimators();

        currentSpeed = ClampSpeed(startSpeed);
        targetSpeed = currentSpeed;
        ApplySpeed(currentSpeed);
    }

    private void OnEnable()
    {
        if (hub == null) return;
        hub.JudgeResolved += OnJudgeResolved;
        hub.FishConsumeRequested += OnFishConsumeRequested;
    }

    private void OnDisable()
    {
        if (hub == null) return;
        hub.JudgeResolved -= OnJudgeResolved;
        hub.FishConsumeRequested -= OnFishConsumeRequested;
    }

    private void Update()
    {
        CleanupDict(_recentJudged, judgedCacheSeconds);
        CleanupDict(_recentSuccess, ignoreGroundMissAfterSuccessSeconds);

        // ✅ 속도는 targetSpeed가 “유지”되고, currentSpeed가 부드럽게 따라감
        float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, k);

        if (!forceApplyInLateUpdate)
            ApplySpeed(currentSpeed);

        animatorCount = (goraniAnimators == null) ? 0 : goraniAnimators.Length;
    }

    private void LateUpdate()
    {
        if (forceApplyInLateUpdate)
            ApplySpeed(currentSpeed);
    }

    private void OnJudgeResolved(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        MarkRecent(_recentJudged, fish);

        if (result == JudgeResult_st2.Perfect)
        {
            MarkRecent(_recentSuccess, fish);
            ApplySuccess(isPerfect: true);
        }
        else if (result == JudgeResult_st2.Good)
        {
            MarkRecent(_recentSuccess, fish);
            ApplySuccess(isPerfect: false);
        }
        else if (result == JudgeResult_st2.Miss)
        {
            ApplyMiss();
        }
    }

    private void OnFishConsumeRequested(FishCatchToken_st2 fish, FishConsumeReason_st2 reason)
    {
        if (!countGroundMissAsMiss) return;

        if (reason != FishConsumeReason_st2.GroundMiss &&
            reason != FishConsumeReason_st2.Timeout)
            return;

        // ✅ 방금 성공한 fish면 GroundMiss/Timeout은 무시
        if (IsRecent(_recentSuccess, fish, ignoreGroundMissAfterSuccessSeconds))
            return;

        // ✅ JudgeResolved(Miss)와 겹치는 케이스 방지
        if (IsRecent(_recentJudged, fish, judgedCacheSeconds))
            return;

        ApplyMiss();
    }

    // =========================
    // ✅ Add-only 핵심 로직
    // =========================
    private void ApplySuccess(bool isPerfect)
    {
        successStreak++;
        missStreak = 0;

        int s = Mathf.Max(1, successStreak);
        float baseGain = isPerfect ? perfectBaseGain : goodBaseGain;
        float bonus = isPerfect ? perfectStreakBonus : goodStreakBonus;

        // ✅ 선형 더하기: base + bonus*(streak-1)
        float gain = baseGain + bonus * (s - 1);

        targetSpeed = ClampSpeed(targetSpeed + gain);
    }

    private void ApplyMiss()
    {
        missStreak++;
        successStreak = 0;

        int m = Mathf.Max(1, missStreak);

        // ✅ 선형 더하기: base + bonus*(streak-1)
        float loss = missBaseLoss + missStreakBonus * (m - 1);

        targetSpeed = ClampSpeed(targetSpeed - loss);
    }

    private float ClampSpeed(float s)
    {
        s = Mathf.Max(0f, s);
        return Mathf.Clamp(s, minSpeed, maxSpeed);
    }

    private void AutoCollectAnimators()
    {
        if (goraniAnimators != null && goraniAnimators.Length > 0) return;
        if (goraniRoot == null) return;

        goraniAnimators = goraniRoot.GetComponentsInChildren<Animator>(true);
    }

    private void ApplySpeed(float speed)
    {
        if (goraniAnimators == null || goraniAnimators.Length == 0) return;

        float clamped = ClampSpeed(speed);
        for (int i = 0; i < goraniAnimators.Length; i++)
        {
            var a = goraniAnimators[i];
            if (!a) continue;

            // ✅ 너가 준 예시처럼 Animator.speed 직접 세팅
            a.speed = clamped;
        }
    }

    private static void MarkRecent(Dictionary<int, float> dict, FishCatchToken_st2 fish)
    {
        if (!fish) return;
        dict[fish.GetInstanceID()] = Time.time;
    }

    private static bool IsRecent(Dictionary<int, float> dict, FishCatchToken_st2 fish, float window)
    {
        if (!fish) return false;
        int id = fish.GetInstanceID();
        return dict.TryGetValue(id, out float t) && (Time.time - t) <= window;
    }

    private void CleanupDict(Dictionary<int, float> dict, float window)
    {
        if (dict.Count == 0) return;

        float now = Time.time;
        _removeKeys.Clear();

        foreach (var kv in dict)
        {
            if (now - kv.Value > window)
                _removeKeys.Add(kv.Key);
        }

        for (int i = 0; i < _removeKeys.Count; i++)
            dict.Remove(_removeKeys[i]);
    }
}

// Assets/3_Stage2/Scripts/Catch/CatchHaptics_st2.cs
using System.Collections;
using UnityEngine;
using static GameState_st2;

/// <summary>
/// 메인 전용 햅틱: JudgeResolved를 구독해서
/// - Good / Perfect 일 때만
/// - 손(Left/Right) 별로 서로 다른 햅틱 프로필 적용
/// </summary>
public class CatchHaptics_st2 : MonoBehaviour
{
    [System.Serializable]
    public struct HapticProfile
    {
        [Range(0f, 1f)] public float frequency;   // 진동 주파수 느낌(0~1)
        [Range(0f, 1f)] public float amplitude;   // 진동 세기(0~1)
        [Min(0f)] public float duration;          // 지속시간(초)
    }

    [Header("Event Hub")]
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Per-hand Profiles (Good)")]
    public HapticProfile leftGood = new HapticProfile { frequency = 0.6f, amplitude = 0.6f, duration = 0.10f };
    public HapticProfile rightGood = new HapticProfile { frequency = 0.6f, amplitude = 0.6f, duration = 0.10f };

    [Header("Per-hand Profiles (Perfect)")]
    public HapticProfile leftPerfect = new HapticProfile { frequency = 0.8f, amplitude = 0.9f, duration = 0.14f };
    public HapticProfile rightPerfect = new HapticProfile { frequency = 0.8f, amplitude = 0.9f, duration = 0.14f };

    [Header("Anti-spam")]
    [Tooltip("같은 손에 대해 너무 연속으로 울리지 않게 제한(초)")]
    [Min(0f)] public float perHandCooldown = 0.05f;

    private bool _subscribed;
    private double _lastLeftDsp = -999;
    private double _lastRightDsp = -999;

    private Coroutine _leftRoutine;
    private Coroutine _rightRoutine;

    public void Initialize(Stage2EventHub_st2 eventHub)
    {
        hub = eventHub;
        TryBind();
    }

    private void OnEnable() => TryBind();

    private void OnDisable()
    {
        TryUnbind();
        StopAllHaptics();
    }

    private void TryBind()
    {
        if (_subscribed) return;
        if (hub == null) return;

        hub.JudgeResolved += OnJudgeResolved;
        _subscribed = true;
    }

    private void TryUnbind()
    {
        if (!_subscribed) return;

        if (hub != null)
            hub.JudgeResolved -= OnJudgeResolved;

        _subscribed = false;
    }

    private void OnJudgeResolved(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        // ✅ Good/Perfect만
        if (result != JudgeResult_st2.Good && result != JudgeResult_st2.Perfect) return;

        // ✅ 손 판별
        bool isLeft = hand == OVRInput.Controller.LTouch;
        bool isRight = hand == OVRInput.Controller.RTouch;
        if (!isLeft && !isRight) return;

        // ✅ 쿨다운(손별)
        double now = AudioSettings.dspTime;
        if (isLeft)
        {
            if (now - _lastLeftDsp < perHandCooldown) return;
            _lastLeftDsp = now;
        }
        else
        {
            if (now - _lastRightDsp < perHandCooldown) return;
            _lastRightDsp = now;
        }

        // ✅ 프로필 선택(손/판정별)
        HapticProfile p;
        if (result == JudgeResult_st2.Perfect)
            p = isLeft ? leftPerfect : rightPerfect;
        else
            p = isLeft ? leftGood : rightGood;

        Play(hand, p);
    }

    private void Play(OVRInput.Controller hand, HapticProfile p)
    {
        float freq = Mathf.Clamp01(p.frequency);
        float amp = Mathf.Clamp01(p.amplitude);
        float dur = Mathf.Max(0f, p.duration);

        if (dur <= 0f || amp <= 0f)
            return;

        if (hand == OVRInput.Controller.LTouch)
        {
            if (_leftRoutine != null) StopCoroutine(_leftRoutine);
            _leftRoutine = StartCoroutine(VibrateRoutine(hand, freq, amp, dur));
        }
        else if (hand == OVRInput.Controller.RTouch)
        {
            if (_rightRoutine != null) StopCoroutine(_rightRoutine);
            _rightRoutine = StartCoroutine(VibrateRoutine(hand, freq, amp, dur));
        }
    }

    private IEnumerator VibrateRoutine(OVRInput.Controller hand, float freq, float amp, float dur)
    {
        OVRInput.SetControllerVibration(freq, amp, hand);

        // Time.timeScale 영향 안 받게 realtime 사용
        float end = Time.realtimeSinceStartup + dur;
        while (Time.realtimeSinceStartup < end)
            yield return null;

        OVRInput.SetControllerVibration(0f, 0f, hand);

        if (hand == OVRInput.Controller.LTouch) _leftRoutine = null;
        if (hand == OVRInput.Controller.RTouch) _rightRoutine = null;
    }

    private void StopAllHaptics()
    {
        if (_leftRoutine != null) StopCoroutine(_leftRoutine);
        if (_rightRoutine != null) StopCoroutine(_rightRoutine);
        _leftRoutine = null;
        _rightRoutine = null;

        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
}

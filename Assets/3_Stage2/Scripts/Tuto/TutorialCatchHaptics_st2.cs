using System.Collections;
using UnityEngine;
using static GameState_st2;

/// <summary>
/// 튜토리얼 전용 햅틱:
/// - 튜토리얼 판정이 확정되는 순간(=Good/Perfect)만 진동
/// - 손(Left/Right) 별로 프로필 분리
/// - 외부(튜토 판정/잡기 성공 처리)에서 TriggerForResult()만 호출하면 됨
/// </summary>
public class TutorialCatchHaptics_st2 : MonoBehaviour
{
    [System.Serializable]
    public struct HapticProfile
    {
        [Range(0f, 1f)] public float frequency;
        [Range(0f, 1f)] public float amplitude;
        [Min(0f)] public float duration;
    }

    [Header("Good Profiles (Per-hand)")]
    public HapticProfile leftGood = new HapticProfile { frequency = 0.6f, amplitude = 0.6f, duration = 0.10f };
    public HapticProfile rightGood = new HapticProfile { frequency = 0.6f, amplitude = 0.6f, duration = 0.10f };

    [Header("Perfect Profiles (Per-hand)")]
    public HapticProfile leftPerfect = new HapticProfile { frequency = 0.8f, amplitude = 0.9f, duration = 0.14f };
    public HapticProfile rightPerfect = new HapticProfile { frequency = 0.8f, amplitude = 0.9f, duration = 0.14f };

    [Header("Anti-spam")]
    [Min(0f)] public float perHandCooldown = 0.05f;

    private double _lastLeftDsp = -999;
    private double _lastRightDsp = -999;

    private Coroutine _leftRoutine;
    private Coroutine _rightRoutine;

    /// <summary>
    /// ✅ 튜토리얼 판정 확정 시점에 호출
    /// Good/Perfect일 때만 진동이 나감
    /// </summary>
    public void TriggerForResult(JudgeResult_st2 result, OVRInput.Controller hand)
    {
        if (result != JudgeResult_st2.Good && result != JudgeResult_st2.Perfect)
            return;

        bool isLeft = hand == OVRInput.Controller.LTouch;
        bool isRight = hand == OVRInput.Controller.RTouch;
        if (!isLeft && !isRight)
            return;

        // 손별 쿨다운
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

        // 프로필 선택
        HapticProfile p = (result == JudgeResult_st2.Perfect)
            ? (isLeft ? leftPerfect : rightPerfect)
            : (isLeft ? leftGood : rightGood);

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

        float end = Time.realtimeSinceStartup + dur;
        while (Time.realtimeSinceStartup < end)
            yield return null;

        OVRInput.SetControllerVibration(0f, 0f, hand);

        if (hand == OVRInput.Controller.LTouch) _leftRoutine = null;
        if (hand == OVRInput.Controller.RTouch) _rightRoutine = null;
    }

    private void OnDisable()
    {
        // 튜토 비활성화 시 진동 잔상 방지
        if (_leftRoutine != null) StopCoroutine(_leftRoutine);
        if (_rightRoutine != null) StopCoroutine(_rightRoutine);
        _leftRoutine = null;
        _rightRoutine = null;

        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
}

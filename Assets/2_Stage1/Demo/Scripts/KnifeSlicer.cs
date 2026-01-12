using UnityEngine;

public class KnifeSlicer : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public KnifeVelocityEstimator velocityEstimator;
    public KnifeVisualResistance visualResistance;

    [Header("Hand")]
    public bool rightHand = true;

    [Header("Audio")]
    public AudioSource sfxSource;

    [Header("Slice Prevention (중복 방지)")]
    [Tooltip("자르기 후 다음 자르기까지 최소 대기 시간 (초)")]
    public float sliceCooldown = 0.15f;

    [Tooltip("연속으로 같은 김밥을 자를 수 있는 최소 간격 (초)")]
    public float sameKimbapCooldown = 0.1f;

    // attempt lock
    bool _canSlice = true;
    float _contactMs;
    float _lastSliceTime = -999f;

    KimbapController _currentKimbap;
    KimbapController _lastSlicedKimbap;
    float _lastKimbapSliceTime = -999f;

    int _sliceIndexThisTrigger;

    // 🔥 실패 피드백 쿨다운
    float _lastFailFeedbackTime = -999f;
    const float FAIL_FEEDBACK_COOLDOWN = 0.2f;

    void Awake()
    {
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // Reset per-trigger index when trigger changes
        if (!conductor) return;

        // If not judging, allow slice again but don't keep old target
        if (!conductor.IsJudgingWindow())
        {
            _canSlice = true;
            _contactMs = 0f;
            _currentKimbap = null;
            _sliceIndexThisTrigger = 0;
            return;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var target = other.GetComponentInParent<KimbapSliceTarget>();
        if (!target || !target.controller) return;

        _currentKimbap = target.controller;
        _contactMs = 0f;
    }

    void OnTriggerStay(Collider other)
    {
        if (!conductor || !conductor.IsJudgingWindow()) return;

        // 🔥 쿨다운 체크 (중복 방지)
        if (Time.time - _lastSliceTime < sliceCooldown)
        {
            return;
        }

        if (!_canSlice) return;

        var trigger = conductor.GetCurrentTrigger();
        if (trigger == null) return;

        var target = other.GetComponentInParent<KimbapSliceTarget>();
        if (!target || !target.controller) return;

        // 🔥 같은 김밥 연속 자르기 방지
        if (target.controller == _lastSlicedKimbap)
        {
            if (Time.time - _lastKimbapSliceTime < sameKimbapCooldown)
            {
                return;
            }
        }

        // Must be sliceable state
        if (!target.controller.sliceable) return;

        // Contact time
        _contactMs += Time.deltaTime * 1000f;
        if (_contactMs < trigger.minContactMs)
        {
            // 🔥 접촉 시간 부족 - 피드백 없이 그냥 리턴
            return;
        }

        // 🔥 Speed check (핵심!)
        float speed = velocityEstimator ? velocityEstimator.speed : 0f;

        if (speed < trigger.minKnifeSpeed)
        {
            // 🔥 속도 부족 - 실패 피드백 (spam 방지)
            if (Time.time - _lastFailFeedbackTime >= FAIL_FEEDBACK_COOLDOWN)
            {
                _lastFailFeedbackTime = Time.time;

                // 디버그 로그
                UnityEngine.Debug.Log($"[KnifeSlicer] Speed too low: {speed:F2} < {trigger.minKnifeSpeed:F2}");

                if (conductor)
                {
                    conductor.NotifySliceFail(transform.position, "SpeedTooLow");
                }
            }
            return;
        }

        // 🔥 속도 충분! - VALID SLICE!
        UnityEngine.Debug.Log($"[KnifeSlicer] Valid slice! Speed: {speed:F2} >= {trigger.minKnifeSpeed:F2}");

        _canSlice = false;
        _lastSliceTime = Time.time;
        _lastSlicedKimbap = target.controller;
        _lastKimbapSliceTime = Time.time;

        // 1) register count
        conductor.RegisterValidSlice();

        // 2) feedback
        float amp = Mathf.Lerp(trigger.hapticHitBase, trigger.hapticHitMax,
            Mathf.Clamp01(speed / (trigger.minKnifeSpeed * 2f)));
        XRHaptics.SendHaptic(rightHand, amp, trigger.hapticDurationMs / 1000f);
        // second tap
        XRHaptics.SendHaptic(rightHand, amp * 0.85f, Mathf.Max(0.01f, trigger.hapticDurationMs / 1600f));

        if (trigger.impactSound) sfxSource.PlayOneShot(trigger.impactSound);

        if (visualResistance)
            visualResistance.Play(trigger.visualResistanceMs, trigger.visualResistanceStrength);

        if (trigger.cutVfxPrefab)
        {
            // spawn at contact approx (knife trigger position)
            var vfx = GameObject.Instantiate(trigger.cutVfxPrefab, transform.position, Quaternion.identity);
            GameObject.Destroy(vfx, 1.5f);
        }

        // 3) RightThin slice (EzySlice)
        if (_currentKimbap)
        {
            int idx0 = _sliceIndexThisTrigger;
            _sliceIndexThisTrigger++;
            _currentKimbap.ExecuteRightThinSlice(idx0);
        }

        // 🔥 4) 성공 이벤트 발행
        if (conductor)
        {
            // 법선은 위쪽(김밥 단면)으로 가정
            conductor.NotifySliceSuccess(transform.position, Vector3.up, speed);
        }

        // Unlock after short cooldown (prevents multi-count in same swing)
        Invoke(nameof(UnlockSlice), sliceCooldown);
    }

    void OnTriggerExit(Collider other)
    {
        var target = other.GetComponentInParent<KimbapSliceTarget>();
        if (!target) return;

        _contactMs = 0f;

        // exit allows next slice sooner (but still respects cooldown)
        if (conductor && conductor.IsJudgingWindow())
        {
            // 쿨다운이 지났으면 바로 허용
            if (Time.time - _lastSliceTime >= sliceCooldown)
            {
                _canSlice = true;
            }
        }
    }

    void UnlockSlice()
    {
        _contactMs = 0f;
        _canSlice = true;
    }
}
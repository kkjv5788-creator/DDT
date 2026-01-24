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
    [Header("Slice Prevention")]
    public float sliceCooldown = 0.15f;
    public float sameKimbapCooldown = 0.1f;

    bool _canSlice = true;
    float _contactMs;
    float _lastSliceTime = -999f;

    KimbapController _currentKimbap;
    KimbapController _lastSlicedKimbap;
    float _lastKimbapSliceTime = -999f;
    int _sliceIndexThisTrigger;
    float _lastFailFeedbackTime = -999f;
    const float FAIL_FEEDBACK_COOLDOWN = 0.2f;

    void Awake()
    {
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
        
        // ▼▼▼ [추가] 칼이 Conductor를 모르면 스스로 찾습니다.
        if (!conductor)
        {
            conductor = FindObjectOfType<RhythmConductor>();
        }
    }

    void Update()
    {
        if (!conductor) return;
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
        if (!conductor) return; // 안전장치
        
        if (!conductor.IsJudgingWindow()) return;
        
        if (Time.time - _lastSliceTime < sliceCooldown) return;
        if (!_canSlice) return;

        var trigger = conductor.GetCurrentTrigger();
        if (trigger == null) return;
        
        var target = other.GetComponentInParent<KimbapSliceTarget>();
        if (!target || !target.controller) return;

        if (target.controller == _lastSlicedKimbap)
        {
            if (Time.time - _lastKimbapSliceTime < sameKimbapCooldown) return;
        }

        if (!target.controller.sliceable) return;
        
        _contactMs += Time.deltaTime * 1000f;
        if (_contactMs < trigger.minContactMs) return;

        float speed = velocityEstimator ? velocityEstimator.speed : 0f;

        if (speed < trigger.minKnifeSpeed)
        {
            if (Time.time - _lastFailFeedbackTime >= FAIL_FEEDBACK_COOLDOWN)
            {
                _lastFailFeedbackTime = Time.time;
                if (conductor) conductor.NotifySliceFail(transform.position, "SpeedTooLow");
            }
            return;
        }

        _canSlice = false;
        _lastSliceTime = Time.time;
        _lastSlicedKimbap = target.controller;
        _lastKimbapSliceTime = Time.time;
        
        conductor.RegisterValidSlice();
        
        float amp = Mathf.Lerp(trigger.hapticHitBase, trigger.hapticHitMax, Mathf.Clamp01(speed / (trigger.minKnifeSpeed * 2f)));
        XRHaptics.SendHaptic(rightHand, amp, trigger.hapticDurationMs / 1000f);
        XRHaptics.SendHaptic(rightHand, amp * 0.85f, Mathf.Max(0.01f, trigger.hapticDurationMs / 1600f));
        if (trigger.impactSound) sfxSource.PlayOneShot(trigger.impactSound);

        if (visualResistance) visualResistance.Play(trigger.visualResistanceMs, trigger.visualResistanceStrength);
        if (trigger.cutVfxPrefab)
        {
            var vfx = GameObject.Instantiate(trigger.cutVfxPrefab, transform.position, Quaternion.identity);
            GameObject.Destroy(vfx, 1.5f);
        }

        if (_currentKimbap)
        {
            int idx0 = _sliceIndexThisTrigger;
            _sliceIndexThisTrigger++;
            _currentKimbap.ExecuteRightThinSlice(idx0);
        }

        // 성공 알림
        if (conductor)
        {
            conductor.NotifySliceSuccess(transform.position, Vector3.up, speed);
        }

        Invoke(nameof(UnlockSlice), sliceCooldown);
    }

    void OnTriggerExit(Collider other)
    {
        var target = other.GetComponentInParent<KimbapSliceTarget>();
        if (!target) return;
        _contactMs = 0f;
        if (conductor && conductor.IsJudgingWindow())
        {
            if (Time.time - _lastSliceTime >= sliceCooldown) _canSlice = true;
        }
    }

    void UnlockSlice()
    {
        _contactMs = 0f;
        _canSlice = true;
    }
}
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

    // attempt lock
    bool _canSlice = true;
    float _contactMs;

    KimbapController _currentKimbap;
    int _sliceIndexThisTrigger;

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
        if (!_canSlice) return;

        var trigger = conductor.GetCurrentTrigger();
        if (trigger == null) return;

        var target = other.GetComponentInParent<KimbapSliceTarget>();
        if (!target || !target.controller) return;

        // Must be sliceable state
        if (!target.controller.sliceable) return;

        // Contact time
        _contactMs += Time.deltaTime * 1000f;
        if (_contactMs < trigger.minContactMs) return;

        // Speed check
        float speed = velocityEstimator ? velocityEstimator.speed : 0f;
        if (speed < trigger.minKnifeSpeed)
        {
            // Optional: very soft negative feedback (no penalty)
            return;
        }

        // VALID SLICE!
        _canSlice = false;

        // 1) register count
        conductor.RegisterValidSlice();

        // 2) feedback
        float amp = Mathf.Lerp(trigger.hapticHitBase, trigger.hapticHitMax, Mathf.Clamp01(speed / (trigger.minKnifeSpeed * 2f)));
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

        // Unlock after short cooldown (prevents multi-count in same swing)
        Invoke(nameof(UnlockSlice), 0.12f);
    }

    void OnTriggerExit(Collider other)
    {
        var target = other.GetComponentInParent<KimbapSliceTarget>();
        if (!target) return;

        _contactMs = 0f;

        // exit allows next slice sooner
        if (conductor && conductor.IsJudgingWindow())
            _canSlice = true;
    }

    void UnlockSlice()
    {
        _contactMs = 0f;
        _canSlice = true;
    }
}

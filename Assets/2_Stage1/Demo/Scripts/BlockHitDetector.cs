using UnityEngine;

[DisallowMultipleComponent]
public class BlockHitDetector : MonoBehaviour
{
    public RhythmConductor conductor;
    public DebugHUD hud;

    TriggerProbe _probe;
    KnifeVisualResistance _resist;

    void Awake()
    {
        _probe = GetComponent<TriggerProbe>();
        if (!_probe) _probe = GetComponentInChildren<TriggerProbe>(true);

        _resist = GetComponent<KnifeVisualResistance>();
        if (!_resist) _resist = GetComponentInChildren<KnifeVisualResistance>(true);
    }

    void OnEnable()
    {
        if (_probe != null) _probe.Blocked += OnBlocked;
    }

    void OnDisable()
    {
        if (_probe != null) _probe.Blocked -= OnBlocked;
    }

    void OnBlocked(Collider c)
    {
        // 비주얼 저항 연출
        if (_resist) _resist.SetResistance01(1f);

        if (conductor) conductor.ReportBlockedHit();
        if (hud) hud.NotifyBlocked(c);
    }
}

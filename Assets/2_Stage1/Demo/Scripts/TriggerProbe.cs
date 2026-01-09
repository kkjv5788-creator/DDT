using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggerProbe : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public Collider knifeTrigger;

    [Header("Filters")]
    public string kimbapTag = "Kimbap";
    public LayerMask sliceableLayer;
    public LayerMask blockedLayer;

    [Header("Runtime")]
    public bool canSlice = true;

    public event Action<Collider> Blocked;
    public event Action<SliceableKimbap, SliceResult> Sliced;

    KnifeVelocityEstimator _vel;

    void Awake()
    {
        if (!knifeTrigger) knifeTrigger = GetComponentInChildren<Collider>(true);
        _vel = GetComponentInParent<KnifeVelocityEstimator>();
        if (!_vel) _vel = GetComponent<KnifeVelocityEstimator>();
    }

    bool InLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    void OnTriggerEnter(Collider other) => HandleTrigger(other);
    void OnTriggerStay(Collider other) => HandleTrigger(other);

    void HandleTrigger(Collider other)
    {
        if (!enabled || !canSlice || other == null) return;

        int layer = other.gameObject.layer;

        // 1) Blocked 우선 처리
        if (InLayerMask(layer, blockedLayer))
        {
            Blocked?.Invoke(other);
            if (conductor) conductor.ReportBlockedHit();
            return;
        }

        // 2) Sliceable 판정
        if (!InLayerMask(layer, sliceableLayer)) return;

        // 태그까지 쓰고 싶으면 유지, 싫으면 kimbapTag 비우면 됨
        if (!string.IsNullOrEmpty(kimbapTag) && !other.CompareTag(kimbapTag))
        {
            // Tag 미일치면 그냥 통과 (프로젝트에서 Tag를 안 쓰는 경우 대비)
        }

        var sliceable = other.GetComponentInParent<SliceableKimbap>();
        if (!sliceable) return;

        float speed = _vel ? _vel.Speed : 0f;

        // 리듬/판정 윈도우 체크
        if (conductor && !conductor.CanSliceNow(speed, out var window))
            return;

        // 실제 슬라이스 시도
        var result = sliceable.TrySlice(speed, conductor ? conductor.SongTime : 0f);
        if (result.didSlice)
        {
            if (conductor) conductor.RegisterSlice(result);
            Sliced?.Invoke(sliceable, result);
        }
    }
}

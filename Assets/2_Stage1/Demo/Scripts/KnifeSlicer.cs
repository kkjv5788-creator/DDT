using UnityEngine;

public class KnifeSlicer : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;

    [Header("Knife")]
    public Collider knifeTrigger;         // 칼날 트리거 콜라이더(자기 자신이어도 됨)

    [Header("Filter")]
    public string kimbapTag = "Kimbap";   // 필요하면 사용
    public LayerMask sliceableLayer;      // KimbapSliceable
    public LayerMask blockedLayer;        // KimbapBlocked (WrongCut용, 별도 Collision이면 다른 스크립트로)

    [Header("Rearm")]
    public bool canSlice = true;

    void Awake()
    {
        if (!knifeTrigger) knifeTrigger = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        // SliceTrigger에 들어왔을 때만 처리(1회)
        if (!canSlice) return;
        if (!IsInLayerMask(other.gameObject.layer, sliceableLayer)) return;

        var trig = conductor ? conductor.GetCurrentTriggerOrNull() : null;
        if (conductor == null || trig == null) return;

        // Judging 아니면 WrongCut(시간 외)
        if (!conductor.IsJudging)
        {
            conductor.RegisterWrongCut(trig, other.ClosestPoint(transform.position));
            return;
        }

        // 현재 활성 KimbapController 찾기
        var kc = other.GetComponentInParent<KimbapController>();
        if (!kc || !kc.sliceable) return;

        // EzySlice 실행
        int sliceIndex0 = Mathf.Max(0, conductor.sliceCount);
        bool ok = kc.ExecuteRightThinSlice(sliceIndex0);

        if (ok)
        {
            conductor.RegisterValidSlice(trig);
            canSlice = false; // Exit 하기 전까진 추가 카운트 금지
        }
        else
        {
            // Judging 내 슬라이스 실패도 피드백(원하면 별도 SFX/VFX 추가)
            conductor.RegisterWrongCut(trig, other.ClosestPoint(transform.position));
        }
    }

    void OnTriggerExit(Collider other)
    {
        // SliceTrigger를 빠져나오면 재무장
        if (IsInLayerMask(other.gameObject.layer, sliceableLayer))
        {
            canSlice = true;
        }
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class GroundMissReporter_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;
    [SerializeField] private JudgeSystem_st2 judge;

    [Header("Filter (Fish ROOT Layer)")]
    [Tooltip("FishCatchToken_st2가 붙어있는 '루트 GameObject'의 Layer를 포함하세요.")]
    public LayerMask fishLayer;

    [Header("Options")]
    [Tooltip("타임아웃 등으로 이미 isResolved=true 인 물고기는 Miss 재카운트(ResolveMissFromGround) 하지 않음")]
    public bool avoidDoubleResolve = true;

    [Tooltip("동일 물고기 중복 호출 방지(초)")]
    public float duplicateCooldown = 0.05f;

    private readonly Dictionary<int, float> _lastHitTime = new Dictionary<int, float>(256);

    public void Construct(Stage2EventHub_st2 eventHub, JudgeSystem_st2 judgeSystem)
    {
        hub = eventHub;
        judge = judgeSystem;
    }

    private void Awake()
    {
        // Construct/인스펙터 누락 안전망 (메인/튜토 허브 분리라면 메인에만 Stage2EventHub가 있어야 함)
        if (!hub) hub = FindObjectOfType<Stage2EventHub_st2>(true);
        if (!judge) judge = FindObjectOfType<JudgeSystem_st2>(true);
    }

    // Trigger 바닥(권장: 감지용 박스 트리거)일 때
    private void OnTriggerEnter(Collider other) => HandleHit(other);

    // 일반 바닥(Collider)일 때도 작동하게
    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider hit)
    {
        if (hit == null) return;

        // collider가 자식에 있어도 부모에서 fish 토큰 찾기
        var fish = hit.GetComponentInParent<FishCatchToken_st2>();
        if (fish == null) return;
        if (!fish.gameObject.activeInHierarchy) return;

        // ⚠️ 필터는 "hit.layer"가 아니라 "fish 루트 layer"로 봐야 안전함
        if (((1 << fish.gameObject.layer) & fishLayer) == 0) return;

        // 중복 호출 억제 (접촉점/다중 콜라이더)
        int id = fish.GetInstanceID();
        float now = Time.time;
        if (_lastHitTime.TryGetValue(id, out float last) && (now - last) < duplicateCooldown) return;
        _lastHitTime[id] = now;

        // 통계용 MISS 확정은 "아직 판정 전"일 때만(중복 방지)
        if (judge != null)
        {
            if (!avoidDoubleResolve || !fish.isResolved)
                judge.ResolveMissFromGround(fish);
        }

        // ✅ 바닥 닿으면 "무조건" 소비(풀 반환)
        if (hub != null)
        {
            hub.PublishFishConsumeRequested(fish, FishConsumeReason_st2.GroundMiss);
        }
        else
        {
            // 허브 없으면 최소 비활성
            fish.gameObject.SetActive(false);
        }
    }
}

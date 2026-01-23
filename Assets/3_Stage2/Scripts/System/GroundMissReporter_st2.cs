using System.Collections.Generic;
using UnityEngine;

public class GroundMissReporter_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Filter (Fish ROOT Layer)")]
    [Tooltip("FishCatchToken_st2가 붙은 '루트 오브젝트'의 Layer를 포함하세요.")]
    public LayerMask fishLayer;

    [Header("Options")]
    [Tooltip("접촉점/다중 콜라이더로 인한 중복 호출 억제(초)")]
    public float duplicateCooldown = 0.05f;

    private readonly Dictionary<int, float> _lastHitTime = new Dictionary<int, float>(256);
    private readonly HashSet<int> _groundRequested = new HashSet<int>(256);

    public void Construct(Stage2EventHub_st2 eventHub)
    {
        hub = eventHub;
    }

    private void Awake()
    {
        if (!hub) hub = FindObjectOfType<Stage2EventHub_st2>(true);
    }

    private void OnTriggerEnter(Collider other) => HandleHit(other);

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider hit)
    {
        if (hit == null) return;

        // 1) 토큰 탐색 (Parent / Self / Rigidbody)
        FishCatchToken_st2 fish = hit.GetComponentInParent<FishCatchToken_st2>();
        if (fish == null) fish = hit.GetComponent<FishCatchToken_st2>();
        if (fish == null && hit.attachedRigidbody != null)
            fish = hit.attachedRigidbody.GetComponent<FishCatchToken_st2>();

        if (fish == null) return;
        if (!fish.gameObject.activeInHierarchy) return;

        // 2) 레이어 필터는 fish "루트" 기준
        var fishRoot = fish.transform.root.gameObject;
        if (((1 << fishRoot.layer) & fishLayer) == 0) return;

        int id = fish.GetInstanceID();
        float now = Time.time;

        // 3) 짧은 시간 중복 억제
        if (_lastHitTime.TryGetValue(id, out float last) && (now - last) < duplicateCooldown) return;
        _lastHitTime[id] = now;

        // 4) 바닥 미스 요청은 1회만
        if (_groundRequested.Contains(id)) return;
        _groundRequested.Add(id);

        // ✅ 판정은 여기서 하지 않음!
        // ✅ "풀 되는 순간" 미스 확정은 FishConsumer_st2가 담당

        if (hub != null)
        {
            hub.PublishFishConsumeRequested(fish, FishConsumeReason_st2.GroundMiss);
        }
        else
        {
            fish.gameObject.SetActive(false);
        }
    }
}

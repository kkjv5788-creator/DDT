using UnityEngine;

public class FishConsumer_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;

    public void Construct(Stage2EventHub_st2 eventHub)
    {
        hub = eventHub;
    }

    private void OnEnable()
    {
        if (hub != null) hub.FishConsumeRequested += OnConsume;
    }

    private void OnDisable()
    {
        if (hub != null) hub.FishConsumeRequested -= OnConsume;
    }

    private void OnConsume(FishCatchToken_st2 fish, FishConsumeReason_st2 reason)
    {
        if (fish == null) return;

        // ✅ Version B: 실물 fish는 풀로 반환
        // Timeout/미스는 “바닥 닿은 뒤 반환” 규칙을 fish 내부 상태로 유지하고,
        // 여기선 reason에 따라 즉시/지연 반환을 선택하면 됨.
        fish.ConsumeToPool(reason);
    }
}

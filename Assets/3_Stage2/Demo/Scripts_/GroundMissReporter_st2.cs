using UnityEngine;

public class GroundMissReporter_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;
    [SerializeField] private JudgeSystem_st2 judge;

    [Header("Filter")]
    [SerializeField] private LayerMask fishLayer;

    public void Construct(Stage2EventHub_st2 eventHub, JudgeSystem_st2 judgeSystem)
    {
        hub = eventHub;
        judge = judgeSystem;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & fishLayer) == 0) return;

        var fish = other.GetComponentInParent<FishCatchToken_st2>();
        if (fish == null) return;

        // ✅ “토큰이 중앙을 모르게” 만들기
        // 기존 ResolveMissFromGround 그대로 사용(통계/패널티 유지)
        if (judge != null)
            judge.ResolveMissFromGround(fish);

        if (hub != null)
            hub.PublishFishConsumeRequested(fish, FishConsumeReason_st2.GroundMiss);
    }
}

using UnityEngine;

public class GroundMissReporter_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;
    [SerializeField] private JudgeSystem_st2 judge;

    [Header("Fish Layer Filter")]
    public LayerMask fishLayer;

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

        // ✅ 기존 ResolveMissFromGround 로직 유지
        if (judge != null)
            judge.ResolveMissFromGround(fish);

        if (hub != null)
            hub.PublishFishConsumeRequested(fish, FishConsumeReason_st2.GroundMiss);
    }
}

using UnityEngine;

public class JudgeBridge_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;
    [SerializeField] private JudgeSystem_st2 judge;

    public void Construct(Stage2EventHub_st2 eventHub, JudgeSystem_st2 judgeSystem)
    {
        hub = eventHub;
        judge = judgeSystem;
    }

    private void OnEnable()
    {
        if (hub != null) hub.CatchAttempted += OnCatchAttempted;
    }

    private void OnDisable()
    {
        if (hub != null) hub.CatchAttempted -= OnCatchAttempted;
    }

    private void OnCatchAttempted(FishCatchToken_st2 fish, OVRInput.Controller hand, double catchDspTime)
    {
        if (judge == null || fish == null) return;

        // ? 기존 PerformJudge 로직 그대로 사용
        JudgeResult_st2 result = judge.PerformJudge(fish, catchDspTime);

        // delta 계산이 기존 로직(예: fish.popDspTime) 기반이면 그걸 그대로
        float delta = fish.TryGetDeltaFromPop((float)catchDspTime, out var d) ? d : 0f;

        hub.PublishJudgeResolved(fish, result, hand, delta);
    }
}

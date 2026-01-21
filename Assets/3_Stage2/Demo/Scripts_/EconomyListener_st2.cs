using UnityEngine;

public class EconomyListener_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;
    [SerializeField] private EconomySystem_st2 economy;

    public void Construct(Stage2EventHub_st2 eventHub, EconomySystem_st2 eco)
    {
        hub = eventHub;
        economy = eco;
    }

    private void OnEnable()
    {
        if (hub != null) hub.JudgeResolved += OnJudgeResolved;
    }

    private void OnDisable()
    {
        if (hub != null) hub.JudgeResolved -= OnJudgeResolved;
    }

    private void OnJudgeResolved(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        if (economy == null) return;
        economy.ApplyJudgeResult(result); // ✅ 기존 함수 그대로
    }
}

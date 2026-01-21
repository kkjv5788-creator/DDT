using System;

public class Stage2EventHub_st2 : MonoBehaviour
{
    public event Action<FishCatchToken_st2, OVRInput.Controller, double> OnCatchAttempted;
    public event Action<FishCatchToken_st2, JudgeResult_st2, OVRInput.Controller, float> OnJudgeResolved;
    public event Action<FishCatchToken_st2, string> OnFishConsumeRequested; // reason: Success/GroundMiss/Timeout

    public void PublishCatchAttempt(FishCatchToken_st2 fish, OVRInput.Controller hand, double dspTime)
        => OnCatchAttempted?.Invoke(fish, hand, dspTime);

    public void PublishJudgeResolved(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
        => OnJudgeResolved?.Invoke(fish, result, hand, delta);

    public void PublishFishConsume(FishCatchToken_st2 fish, string reason)
        => OnFishConsumeRequested?.Invoke(fish, reason);
}
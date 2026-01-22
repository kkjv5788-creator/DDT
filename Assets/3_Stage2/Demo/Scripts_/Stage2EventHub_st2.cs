using System;
using UnityEngine;
using static GameState_st2;

public enum FishConsumeReason_st2
{
    Success,
    GroundMiss,
    Timeout,
    Despawn
}

public class Stage2EventHub_st2 : MonoBehaviour
{
    public event Action<FishCatchToken_st2, OVRInput.Controller, double> CatchAttempted;
    public event Action<FishCatchToken_st2, JudgeResult_st2, OVRInput.Controller, float> JudgeResolved;
    public event Action<FishCatchToken_st2, FishConsumeReason_st2> FishConsumeRequested;

    public void PublishCatchAttempted(FishCatchToken_st2 fish, OVRInput.Controller hand, double dspTime)
        => CatchAttempted?.Invoke(fish, hand, dspTime);

    public void PublishJudgeResolved(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
        => JudgeResolved?.Invoke(fish, result, hand, delta);

    public void PublishFishConsumeRequested(FishCatchToken_st2 fish, FishConsumeReason_st2 reason)
        => FishConsumeRequested?.Invoke(fish, reason);
}

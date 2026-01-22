using System;
using System.Reflection;
using UnityEngine;
using static GameState_st2;

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
        if (hub == null || judge == null || fish == null) return;

        // PerformJudge(fish, catchDspTime) 호출 (시그니처가 다를 수 있어 reflection 사용)
        JudgeResult_st2 result = JudgeResult_st2.Miss;

        try
        {
            var mi = judge.GetType().GetMethod("PerformJudge",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (mi != null)
            {
                object r = mi.GetParameters().Length switch
                {
                    2 => mi.Invoke(judge, new object[] { fish, catchDspTime }),
                    1 => mi.Invoke(judge, new object[] { fish }),
                    _ => mi.Invoke(judge, null)
                };

                if (r is JudgeResult_st2 jr) result = jr;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[JudgeBridge_st2] PerformJudge invoke failed: {e.Message}");
            result = JudgeResult_st2.Miss;
        }

        float delta = ComputeDeltaFromFishPopTime(fish, catchDspTime);
        hub.PublishJudgeResolved(fish, result, hand, delta);
    }

    private float ComputeDeltaFromFishPopTime(FishCatchToken_st2 fish, double catchDspTime)
    {
        // popTime/popDspTime 등의 필드가 있으면 delta 계산, 없으면 0
        try
        {
            var t = fish.GetType();
            var f1 = t.GetField("popDspTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f1 != null && f1.GetValue(fish) is double popDsp)
                return (float)(catchDspTime - popDsp);

            var f2 = t.GetField("popTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f2 != null && f2.GetValue(fish) is double popTime)
                return (float)(catchDspTime - popTime);

            var p1 = t.GetProperty("popDspTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p1 != null && p1.GetValue(fish) is double popDsp2)
                return (float)(catchDspTime - popDsp2);

            var p2 = t.GetProperty("popTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p2 != null && p2.GetValue(fish) is double popTime2)
                return (float)(catchDspTime - popTime2);
        }
        catch { /* ignore */ }

        return 0f;
    }
}

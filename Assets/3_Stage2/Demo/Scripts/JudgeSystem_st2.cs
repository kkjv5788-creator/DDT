using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameState_st2;

public class JudgeSystem_st2 : MonoBehaviour
{
    [Header("판정 윈도우")]
    public float perfectHalfWindow = 0.04f;
    public float goodHalfWindow = 0.075f;
    public float fishCatchableDuration = 1.0f;

    [Header("통계")]
    public int perfectCount = 0;
    public int goodCount = 0;
    public int missCount = 0;

    // 이벤트
    public event Action<JudgeResult_st2, float> OnJudgeResult;

    private HashSet<FishCatchToken_st2> activeFish = new HashSet<FishCatchToken_st2>();

    public void RegisterFish(FishCatchToken_st2 fish)
    {
        activeFish.Add(fish);
    }

    public void UnregisterFish(FishCatchToken_st2 fish)
    {
        activeFish.Remove(fish);
    }

    public JudgeResult_st2 PerformJudge(FishCatchToken_st2 fish, double catchTime)
    {
        if (fish.isResolved) return JudgeResult_st2.Miss;

        float delta = (float)(catchTime - fish.popTime);
        float absDelta = Mathf.Abs(delta);

        JudgeResult_st2 result;

        if (absDelta <= perfectHalfWindow)
        {
            result = JudgeResult_st2.Perfect;
            perfectCount++;
        }
        else if (absDelta <= goodHalfWindow)
        {
            result = JudgeResult_st2.Good;
            goodCount++;
        }
        else
        {
            result = JudgeResult_st2.Miss;
            missCount++;
        }

        fish.isResolved = true;
        fish.isCaught = (result != JudgeResult_st2.Miss);

        activeFish.Remove(fish);

        OnJudgeResult?.Invoke(result, delta);

        return result;
    }

    // ✅ 수정: Timeout Miss는 즉시 삭제하지 않고 낙하 유지
    void ProcessTimeoutMiss(FishCatchToken_st2 fish)
    {
        if (fish.isResolved) return;

        // ✅ Miss 확정만 하고 물리 낙하는 유지
        fish.isResolved = true;
        fish.isCaught = false;
        missCount++;

        OnJudgeResult?.Invoke(JudgeResult_st2.Miss, 0f);

        // ✅ 즉시 ReleaseFish 호출 제거 - 바닥에서 제거됨
        // 단, activeFish에서는 제거
        activeFish.Remove(fish);
    }

    public void ResetStats()
    {
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
        activeFish.Clear();
    }

    public void ForceResolveAll()
    {
        var fishList = new List<FishCatchToken_st2>(activeFish);

        foreach (var fish in fishList)
        {
            if (fish != null && !fish.isResolved)
            {
                ProcessTimeoutMiss(fish);
            }
        }

        activeFish.Clear();
    }
}
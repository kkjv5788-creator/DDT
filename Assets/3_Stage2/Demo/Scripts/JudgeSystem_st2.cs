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

    // ✅ 수정: HashSet으로 변경 (O(1) 제거 성능)
    private HashSet<FishCatchToken_st2> activeFish = new HashSet<FishCatchToken_st2>();

    public void RegisterFish(FishCatchToken_st2 fish)
    {
        activeFish.Add(fish);
    }

    // ✅ 추가: 명시적 제거 메서드
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

        // ✅ 추가: 판정 확정 즉시 제거
        activeFish.Remove(fish);

        OnJudgeResult?.Invoke(result, delta);

        return result;
    }

    void ProcessTimeoutMiss(FishCatchToken_st2 fish)
    {
        if (fish.isResolved) return;

        fish.isResolved = true;
        fish.isCaught = false;
        missCount++;

        OnJudgeResult?.Invoke(JudgeResult_st2.Miss, 0f);

        // ✅ ownerMold로 릴리즈
        if (fish.ownerMold != null)
        {
            fish.ownerMold.ReleaseFish(fish);
        }
    }

    public void ResetStats()
    {
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
        activeFish.Clear();
    }

    // ✅ 추가: 곡 종료 시 남은 fish 강제 Miss
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
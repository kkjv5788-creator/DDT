using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameState_st2;

public class EconomySystem_st2 : MonoBehaviour
{
    [Header("일급 설정")]
    public int baseWage = 10000;
    public int perfectBonus = 100;
    public int missPenalty = 50;
    public int maxWage = 30000;

    [Header("현재 상태")]
    public int currentWage;

    public event Action<int> OnWageChanged;

    void Start()
    {
        currentWage = baseWage;
    }

    public void ApplyJudgeResult(JudgeResult_st2 result)
    {
        switch (result)
        {
            case JudgeResult_st2.Perfect:
                currentWage = Mathf.Min(currentWage + perfectBonus, maxWage);
                break;
            case JudgeResult_st2.Miss:
                currentWage = Mathf.Max(currentWage - missPenalty, 0);
                break;
        }

        OnWageChanged?.Invoke(currentWage);
    }

    public void Reset()
    {
        currentWage = baseWage;
        OnWageChanged?.Invoke(currentWage);
    }
}

using System;
using UnityEngine;
using static GameState_st2;

public class EconomySystem_st2 : MonoBehaviour
{
    [Header("일급 설정")]
    public int baseWage = 10000;

    [Header("보너스/패널티")]
    public int perfectBonus = 100;
    public int goodBonus = 50;     // ✅ 추가: Good 보너스
    public int missPenalty = 50;

    [Header("옵션")]
    public bool penalizeOnMiss = false; // ✅ 미스 감봉 여부(요구사항: 기본 false)

    [Header("최대 임금")]
    public int maxWage = 30000;

    [Header("현재 상태")]
    public int currentWage;

    public event Action<int> OnWageChanged;

    private void Awake()
    {
        // Start 순서 이슈 방지
        if (currentWage <= 0) currentWage = baseWage;
    }

    private void Start()
    {
        currentWage = baseWage;
        OnWageChanged?.Invoke(currentWage);
    }

    public void ApplyJudgeResult(JudgeResult_st2 result)
    {
        bool changed = false;

        switch (result)
        {
            case JudgeResult_st2.Perfect:
                currentWage = Mathf.Min(currentWage + perfectBonus, maxWage);
                changed = true;
                break;

            case JudgeResult_st2.Good:
                currentWage = Mathf.Min(currentWage + goodBonus, maxWage); // ✅ Good도 돈 벌림
                changed = true;
                break;

            case JudgeResult_st2.Miss:
                if (penalizeOnMiss) // ✅ 기본 false라 감봉 안 됨
                {
                    currentWage = Mathf.Max(currentWage - missPenalty, 0);
                    changed = true;
                }
                break;
        }

        if (changed)
            OnWageChanged?.Invoke(currentWage);
    }

    public void Reset()
    {
        currentWage = baseWage;
        OnWageChanged?.Invoke(currentWage);
    }
}

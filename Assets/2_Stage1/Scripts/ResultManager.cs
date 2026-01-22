using UnityEngine;

public class ResultManager : MonoBehaviour
{
    [Header("Refs")]
    public StageUIManager stageUIManager;   
    public RhythmConductor conductor; // [중요] 이벤트를 받기 위해 연결 필요

    [Header("Settings")]
    public int pricePerSuccess = 5000;

    // 내부 변수
    private int _currentTotalSales = 0;

    void OnEnable()
    {
        // 컨덕터의 성공 이벤트를 구독
        if (conductor != null)
        {
            conductor.OnRoundResult.AddListener(OnRoundResult);
        }
    }

    void OnDisable()
    {
        if (conductor != null)
        {
            conductor.OnRoundResult.RemoveListener(OnRoundResult);
        }
    }

    // 성공/실패 결과가 나오면 호출됨
    public void OnRoundResult(bool isSuccess)
    {
        if (isSuccess)
        {
            // 돈 더하기
            _currentTotalSales += pricePerSuccess;

            // UI 매니저에게 표시하라고 시킴
            if (stageUIManager)
            {
                stageUIManager.UpdateSalesUI(_currentTotalSales, pricePerSuccess, true);
            }
        }
    }

    // 게임 재시작용
    public void ResetResults()
    {
        _currentTotalSales = 0;
        if (stageUIManager) stageUIManager.UpdateSalesUI(0, 0, false);
    }
}
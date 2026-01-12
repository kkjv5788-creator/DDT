using UnityEngine;
using UnityEngine.Events;

public class ResultManager : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public GameFlowManager gameFlowManager;

    [Header("Result Panels (3개 중 1개만 활성화)")]
    public GameObject panel100k; // 100% ~ 80%
    public GameObject panel50k;  // 80% ~ 50%
    public GameObject panel0;    // 50% ~ 0%

    [Header("Settings")]
    public float endDelaySeconds = 3f; // 마지막 트리거 완료 후 대기 시간

    [Header("Events")]
    public UnityEvent<int, int, float> OnProgressUpdate; // successCount, totalTriggers, percentage

    // 런타임 데이터
    int _totalTriggers;
    int _successCount;
    bool _gameEnded;

    void OnEnable()
    {
        if (conductor)
        {
            conductor.OnRoundResult.AddListener(HandleRoundResult);
        }
    }

    void OnDisable()
    {
        if (conductor)
        {
            conductor.OnRoundResult.RemoveListener(HandleRoundResult);
        }
    }

    public void StartTracking(int totalTriggers)
    {
        _totalTriggers = totalTriggers;
        _successCount = 0;
        _gameEnded = false;

        Debug.Log($"[ResultManager] Tracking started: {totalTriggers} triggers");

        // 초기 게이지 업데이트
        OnProgressUpdate?.Invoke(0, _totalTriggers, 0f);
    }

    void HandleRoundResult(bool success)
    {
        if (_gameEnded) return;
        if (!gameFlowManager || gameFlowManager.CurrentState != GameState.PlayingMain) return;

        if (success)
        {
            _successCount++;
            Debug.Log($"[ResultManager] Success! ({_successCount}/{_totalTriggers})");
        }
        else
        {
            Debug.Log($"[ResultManager] Failed. ({_successCount}/{_totalTriggers})");
        }

        // 진행률 계산
        float percentage = (_successCount / (float)_totalTriggers) * 100f;
        OnProgressUpdate?.Invoke(_successCount, _totalTriggers, percentage);

        // 마지막 트리거 완료 확인
        if (conductor.CurrentTriggerIndex >= _totalTriggers - 1)
        {
            Debug.Log("[ResultManager] Last trigger completed. Starting end delay...");
            Invoke(nameof(EndGame), endDelaySeconds);
        }
    }

    void EndGame()
    {
        if (_gameEnded) return;
        _gameEnded = true;

        Debug.Log("[ResultManager] Game ended. Showing result panel...");

        // 성공률 계산
        float successRate = (_successCount / (float)_totalTriggers) * 100f;

        // BGM 정지
        if (conductor && conductor.bgmSource)
        {
            conductor.bgmSource.Stop();
        }

        // 상태 전환
        if (gameFlowManager)
        {
            gameFlowManager.EnterFinalResult(successRate);
        }

        // 패널 활성화
        ShowResultPanel(successRate);
    }

    void ShowResultPanel(float successRate)
    {
        // 모든 패널 비활성화
        if (panel100k) panel100k.SetActive(false);
        if (panel50k) panel50k.SetActive(false);
        if (panel0) panel0.SetActive(false);

        // 조건에 맞는 패널만 활성화
        if (successRate >= 80f)
        {
            if (panel100k) panel100k.SetActive(true);
            Debug.Log($"[ResultManager] Result: 100k (Success Rate: {successRate:F1}%)");
        }
        else if (successRate >= 50f)
        {
            if (panel50k) panel50k.SetActive(true);
            Debug.Log($"[ResultManager] Result: 50k (Success Rate: {successRate:F1}%)");
        }
        else
        {
            if (panel0) panel0.SetActive(true);
            Debug.Log($"[ResultManager] Result: 0 (Success Rate: {successRate:F1}%)");
        }
    }

    // 외부에서 호출 가능
    public int GetSuccessCount() => _successCount;
    public int GetTotalTriggers() => _totalTriggers;
    public float GetSuccessRate() => (_successCount / (float)Mathf.Max(1, _totalTriggers)) * 100f;
}
using UnityEngine;
using UnityEngine.Events;

public class ResultManager : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public GameFlowManager gameFlowManager;

    [Header("Result Panels (3�� �� 1���� Ȱ��ȭ)")]
    public GameObject panel100k; // 100% ~ 80%
    public GameObject panel50k;  // 80% ~ 50%
    public GameObject panel0;    // 50% ~ 0%

    [Header("Settings")]
    public float endDelaySeconds = 3f; // ������ Ʈ���� �Ϸ� �� ��� �ð�

    [Header("Events")]
    public UnityEvent<int, int, float> OnProgressUpdate; // successCount, totalTriggers, percentage

    // ��Ÿ�� ������
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
        // 🔥 이전에 예약된 Invoke 취소 (재시작 시 EndGame이 중복 호출되는 것 방지)
        CancelInvoke(nameof(EndGame));

        _totalTriggers = totalTriggers;
        _successCount = 0;
        _gameEnded = false;

        Debug.Log($"[ResultManager] Tracking started: {totalTriggers} triggers");

        // �ʱ� ������ ������Ʈ
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

        // ����� ���
        float percentage = (_successCount / (float)_totalTriggers) * 100f;
        OnProgressUpdate?.Invoke(_successCount, _totalTriggers, percentage);

        // ������ Ʈ���� �Ϸ� Ȯ��
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

        // ������ ���
        float successRate = (_successCount / (float)_totalTriggers) * 100f;

        // BGM ����
        if (conductor && conductor.bgmSource)
        {
            conductor.bgmSource.Stop();
        }

        // ���� ��ȯ
        if (gameFlowManager)
        {
            gameFlowManager.EnterFinalResult(successRate);
        }

        // �г� Ȱ��ȭ
        ShowResultPanel(successRate);
    }

    void ShowResultPanel(float successRate)
    {
        // ��� �г� ��Ȱ��ȭ
        if (panel100k) panel100k.SetActive(false);
        if (panel50k) panel50k.SetActive(false);
        if (panel0) panel0.SetActive(false);

        // ���ǿ� �´� �гθ� Ȱ��ȭ
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

    // �ܺο��� ȣ�� ����
    public int GetSuccessCount() => _successCount;
    public int GetTotalTriggers() => _totalTriggers;
    public float GetSuccessRate() => (_successCount / (float)Mathf.Max(1, _totalTriggers)) * 100f;
}
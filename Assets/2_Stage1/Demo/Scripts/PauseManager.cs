using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("Refs")]
    public GameFlowManager gameFlowManager;
    public RhythmConductor conductor;
    public GameObject pauseMenuPanel;

    [Header("Audio")]
    public AudioSource[] allAudioSources; // 모든 AudioSource 할당

    [Header("Settings")]
    public float inputCooldown = 0.5f;

    bool _isPaused;
    float _lastInputTime = -999f;

    void Update()
    {
        // FinalResult 상태에서는 Pause 불가
        if (gameFlowManager && gameFlowManager.CurrentState == GameState.FinalResult)
            return;

        // Paused 상태에서는 추가 입력 무시
        if (_isPaused)
            return;

        // 왼손 인덱스 트리거 입력 (1회)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            if (Time.time - _lastInputTime > inputCooldown)
            {
                _lastInputTime = Time.time;
                Pause();
            }
        }
    }

    public void Pause()
    {
        if (_isPaused) return;

        _isPaused = true;
        Debug.Log("[PauseManager] Game paused");

        // 시간 정지
        Time.timeScale = 0f;

        // 오디오 정지
        PauseAllAudio();

        // 패널 활성화
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(true);

        // 상태 전환
        if (gameFlowManager)
            gameFlowManager.EnterPaused();
    }

    public void Resume()
    {
        if (!_isPaused) return;

        _isPaused = false;
        Debug.Log("[PauseManager] Game resumed");

        // 시간 재개
        Time.timeScale = 1f;

        // 오디오 재개
        ResumeAllAudio();

        // 패널 비활성화
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        // 상태 복귀
        if (gameFlowManager)
            gameFlowManager.ExitPaused();
    }

    public void Restart()
    {
        Debug.Log("[PauseManager] Restart requested");

        // 시간 재개 (씬 로드 전에 필수)
        Time.timeScale = 1f;

        if (gameFlowManager)
        {
            gameFlowManager.Restart();
        }
    }

    public void GoToMainMenu()
    {
        Debug.Log("[PauseManager] Going to main menu");

        // 시간 재개
        Time.timeScale = 1f;

        if (gameFlowManager)
        {
            gameFlowManager.GoToMainMenu();
        }
    }

    void PauseAllAudio()
    {
        foreach (var source in allAudioSources)
        {
            if (source && source.isPlaying)
            {
                source.Pause();
            }
        }
    }

    void ResumeAllAudio()
    {
        foreach (var source in allAudioSources)
        {
            if (source)
            {
                source.UnPause();
            }
        }
    }

    public bool IsPaused => _isPaused;
}

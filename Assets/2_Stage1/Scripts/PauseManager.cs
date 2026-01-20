using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Refs")]
    public GameFlowManager gameFlowManager;
    public RhythmConductor conductor;
    public TutorialController tutorialController; // 튜토리얼용 (필요시)
    public GameObject pauseMenuPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Audio")]
    public AudioSource[] allAudioSources; // 멈출 오디오들

    [Header("Settings")]
    public float inputCooldown = 0.5f;

    bool _isPaused = false;
    float _lastPauseInputTime = -999f;

    void Start()
    {
        // 버튼 리스너 연결
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (restartButton) restartButton.onClick.AddListener(Restart);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        // 입력 쿨타임 체크
        if (Time.time - _lastPauseInputTime < inputCooldown) return;

        // B버튼 또는 Y버튼 또는 ESC로 일시정지 토글
        if (OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four) || Input.GetKeyDown(KeyCode.Escape))
        {
            _lastPauseInputTime = Time.time;
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (_isPaused) return;

        // ★ GameFlowManager가 없거나, 이미 인트로/결과창이면 일시정지 불가
        if (gameFlowManager == null) return;
        
        // ★ 여기서 에러가 났던 겁니다. 
        // 전역 GameState를 쓰므로 GameFlowManager.GameState가 아니라 그냥 GameState입니다.
        if (gameFlowManager.CurrentState == GameState.Intro || 
            gameFlowManager.CurrentState == GameState.FinalResult) return;

        _isPaused = true;
        
        // 메뉴 표시
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        
        // 매니저에게 알림
        gameFlowManager.PauseGame();
        
        // 소리 끄기 (선택)
        PauseAllAudio();
    }

    public void Resume()
    {
        if (!_isPaused) return;

        _isPaused = false;

        // 메뉴 숨김
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);

        // 매니저에게 알림
        if (gameFlowManager) gameFlowManager.ResumeGame();

        // 소리 켜기
        ResumeAllAudio();
    }

    public void Restart()
    {
        Debug.Log("[PauseManager] 재시작 요청");
        
        // 일시정지 해제 (시간 흐르게 하기 위해)
        Time.timeScale = 1f; 

        // 현재 모드에 따라 재시작 분기
        if (gameFlowManager)
        {
            // 전역 GameState 사용
            if (gameFlowManager.CurrentState == GameState.Tutorial)
            {
                // 튜토리얼은 씬 재로딩이 깔끔할 수 있음
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                // 실전 모드는 매니저 리셋
                gameFlowManager.RestartMainGameOnly();
            }
        }
        else
        {
            // 예외 상황: 그냥 씬 재시작
             SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (gameFlowManager) gameFlowManager.GoToMainMenu();
        else SceneManager.LoadScene("Main_v4.1"); // 하드코딩 백업
    }

    void PauseAllAudio()
    {
        foreach (var source in allAudioSources)
        {
            if (source && source.isPlaying) source.Pause();
        }
    }

    void ResumeAllAudio()
    {
        foreach (var source in allAudioSources)
        {
            if (source) source.UnPause();
        }
    }
    
    // UI 포인터용 퍼블릭 메서드 (필요시 사용)
    public void ShowPauseMenu() 
    {
         if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
    }
    
    public void HidePauseMenu() 
    {
         if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
    }
}
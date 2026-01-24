using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Refs")]
    public GameFlowManager gameFlowManager;
    public GameObject pauseMenuPanel;

    [Header("Buttons (인스펙터 연결 필수)")]
    public Button btnResume;        // 계속하기
    public Button btnRestart;       // 다시하기
    public Button btnGoToSelect;    // 영업준비
    public Button btnGoToLobby;     // 메인으로
    public Button btnQuit;          // 끝내기

    [Header("Settings")]
    public int mainMenuSceneIndex = 0; 
    public float inputCooldown = 0.5f;

    bool _isPaused = false;
    float _lastPauseInputTime = -999f;

    void Start()
    {
        // 버튼 리스너 자동 연결
        if (btnResume) btnResume.onClick.AddListener(Resume);
        if (btnRestart) btnRestart.onClick.AddListener(Restart);
        if (btnGoToSelect) btnGoToSelect.onClick.AddListener(GoToSelectMode);
        if (btnGoToLobby) btnGoToLobby.onClick.AddListener(GoToMainMenu);
        if (btnQuit) btnQuit.onClick.AddListener(QuitGame);

        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (Time.unscaledTime - _lastPauseInputTime < inputCooldown) return;

        // 🔥 [복구] 오른손 A버튼 (Button.One) + 에디터용 ESC 키
        bool inputPause = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) 
                       || Input.GetKeyDown(KeyCode.Escape);
        
        if (inputPause)
        {
            if (gameFlowManager != null)
            {
                Debug.Log(">>> 일시정지 버튼(A버튼/ESC) 입력됨!");
                _lastPauseInputTime = Time.unscaledTime;
                TogglePause();
            }
            else
            {
                Debug.LogError("PauseManager에 GameFlowManager가 연결되지 않았습니다!");
            }
        }
    }

    public void TogglePause() { if (_isPaused) Resume(); else Pause(); }

    public void Pause() 
    { 
        if (_isPaused) return; 
        _isPaused = true; 
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true); 
        if (gameFlowManager) gameFlowManager.PauseGame(); 
    }

    public void Resume() 
    { 
        if (!_isPaused) return; 
        _isPaused = false; 
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false); 
        if (gameFlowManager) gameFlowManager.ResumeGame(); 
    }

    public void Restart() 
    { 
        Time.timeScale = 1f; 
        _isPaused = false;
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (gameFlowManager) gameFlowManager.ResetAndRestartMainGame(); 
    }

    public void GoToSelectMode()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu() 
    { 
        Time.timeScale = 1f; 
        _isPaused = false;
        
        if (gameFlowManager && gameFlowManager.conductor && gameFlowManager.conductor.bgmSource)
            gameFlowManager.conductor.bgmSource.Stop();

        Debug.Log($"메인 메뉴(Index {mainMenuSceneIndex})로 돌아갑니다.");
        SceneManager.LoadScene(mainMenuSceneIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
    }

    // 에러 방지용 함수들 (GameFlowManager에서 호출)
    public void ShowPauseMenu() { if (pauseMenuPanel) pauseMenuPanel.SetActive(true); }
    public void HidePauseMenu() { if (pauseMenuPanel) pauseMenuPanel.SetActive(false); }
}
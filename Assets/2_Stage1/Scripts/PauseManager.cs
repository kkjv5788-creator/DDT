using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Refs")]
    public GameFlowManager gameFlowManager;
    public GameObject pauseMenuPanel; // Monitor_Canvas/Panel_Pause

    [Header("Buttons")]
    public Button resumeButton;  // Btn_ResumePause
    public Button restartButton; // Btn_RestartPause
    public Button mainMenuButton;// Btn_MainPause

    [Header("Settings")]
    public int mainMenuSceneIndex = 0; 
    public float inputCooldown = 0.5f;

    bool _isPaused = false;
    float _lastPauseInputTime = -999f;

    void Start()
    {
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (restartButton) restartButton.onClick.AddListener(Restart);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (Time.unscaledTime - _lastPauseInputTime < inputCooldown) return;

        // [수정] 오른손 A 버튼(One) 또는 ESC 키
                bool inputPause = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
        if (inputPause && gameFlowManager != null)
        {
            _lastPauseInputTime = Time.unscaledTime;
            TogglePause();
        }
    }

    public void TogglePause() { if (_isPaused) Resume(); else Pause(); }
    public void Pause() { if (_isPaused) return; _isPaused = true; if (pauseMenuPanel) pauseMenuPanel.SetActive(true); if (gameFlowManager) gameFlowManager.PauseGame(); }
    public void Resume() { if (!_isPaused) return; _isPaused = false; if (pauseMenuPanel) pauseMenuPanel.SetActive(false); if (gameFlowManager) gameFlowManager.ResumeGame(); }
        public void Restart() { Time.timeScale = 1f; if (gameFlowManager) gameFlowManager.ResetAndRestartMainGame(); }
        public void GoToMainMenu() { Time.timeScale = 1f; if (gameFlowManager) gameFlowManager.GoToMainMenu(); }
    public void ShowPauseMenu() { if (pauseMenuPanel) pauseMenuPanel.SetActive(true); }
    public void HidePauseMenu() { if (pauseMenuPanel) pauseMenuPanel.SetActive(false); }
}

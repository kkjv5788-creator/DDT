using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Refs")]
    public GameFlowManager gameFlowManager;
    public RhythmConductor conductor;
    public TutorialController tutorialController;
    public GameObject pauseMenuPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("VR Pointer")]
    public Transform rightHandAnchor;
    public float pointerMaxDistance = 10f;
    public LineRenderer pointerLine;
    public LayerMask uiLayer;

    [Header("Audio")]
    public AudioSource[] allAudioSources;

    [Header("Settings")]
    public float inputCooldown = 0.5f;

    bool _isPaused;
    float _lastPauseInputTime = -999f;
    float _lastClickInputTime = -999f;
    Button _currentHoveredButton;

    void Start()
    {
        // 버튼 이벤트 연결
        if (resumeButton)
            resumeButton.onClick.AddListener(Resume);

        if (restartButton)
            restartButton.onClick.AddListener(Restart);

        if (mainMenuButton)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        if (pointerLine)
            pointerLine.enabled = false;
    }

    void Update()
    {
        // FinalResult 상태에서는 Pause 불가
        if (gameFlowManager && gameFlowManager.CurrentState == GameState.FinalResult)
            return;

        if (_isPaused)
        {
            // Pause 중: VR 포인터로 버튼 조작
            HandleVRPointer();
        }
        else
        {
            // 왼손 인덱스 트리거로 Pause 진입
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                if (Time.unscaledTime - _lastPauseInputTime > inputCooldown)
                {
                    _lastPauseInputTime = Time.unscaledTime;
                    Pause();
                }
            }
        }
    }

    void HandleVRPointer()
    {
        if (!rightHandAnchor) return;

        Ray ray = new Ray(rightHandAnchor.position, rightHandAnchor.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pointerMaxDistance, uiLayer))
        {
            if (pointerLine)
            {
                pointerLine.enabled = true;
                pointerLine.SetPosition(0, rightHandAnchor.position);
                pointerLine.SetPosition(1, hit.point);
            }

            Button hitButton = hit.collider.GetComponent<Button>();
            if (!hitButton)
                hitButton = hit.collider.GetComponentInParent<Button>();

            if (hitButton)
            {
                if (_currentHoveredButton != hitButton)
                {
                    if (_currentHoveredButton)
                        ResetButtonColor(_currentHoveredButton);

                    _currentHoveredButton = hitButton;
                    HighlightButton(_currentHoveredButton);
                }

                // 🔥 오른손 인덱스 트리거로 클릭
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    if (Time.unscaledTime - _lastClickInputTime > 0.3f)
                    {
                        _lastClickInputTime = Time.unscaledTime;
                        Debug.Log($"[PauseManager] Button clicked: {hitButton.name}");
                        hitButton.onClick.Invoke();
                    }
                }
            }
            else
            {
                if (_currentHoveredButton)
                {
                    ResetButtonColor(_currentHoveredButton);
                    _currentHoveredButton = null;
                }
            }
        }
        else
        {
            if (pointerLine)
            {
                pointerLine.enabled = true;
                pointerLine.SetPosition(0, rightHandAnchor.position);
                pointerLine.SetPosition(1, rightHandAnchor.position + rightHandAnchor.forward * pointerMaxDistance);
            }

            if (_currentHoveredButton)
            {
                ResetButtonColor(_currentHoveredButton);
                _currentHoveredButton = null;
            }
        }
    }

    void HighlightButton(Button button)
    {
        if (!button) return;

        var colors = button.colors;
        colors.normalColor = Color.yellow;
        button.colors = colors;
    }

    void ResetButtonColor(Button button)
    {
        if (!button) return;

        var colors = button.colors;
        colors.normalColor = Color.white;
        button.colors = colors;
    }

    public void Pause()
    {
        if (_isPaused) return;

        _isPaused = true;
        Debug.Log("[PauseManager] Game paused");

        Time.timeScale = 0f;
        PauseAllAudio();

        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(true);

        if (pointerLine)
            pointerLine.enabled = true;

        if (gameFlowManager)
            gameFlowManager.EnterPaused();
    }

    public void Resume()
    {
        if (!_isPaused) return;

        _isPaused = false;
        Debug.Log("[PauseManager] Game resumed");

        // 🔥 시간 재개
        Time.timeScale = 1f;

        // 🔥 오디오 재개
        ResumeAllAudio();

        // 패널 비활성화
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        if (pointerLine)
            pointerLine.enabled = false;

        if (_currentHoveredButton)
        {
            ResetButtonColor(_currentHoveredButton);
            _currentHoveredButton = null;
        }

        if (gameFlowManager)
            gameFlowManager.ExitPaused();
    }

    public void Restart()
    {
        Debug.Log($"[PauseManager] Restart requested from state: {gameFlowManager.CurrentState}");

        // 🔥 시간 재개 (필수)
        Time.timeScale = 1f;

        // 🔥 패널 비활성화
        _isPaused = false;
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(false);

        if (pointerLine)
            pointerLine.enabled = false;

        if (_currentHoveredButton)
        {
            ResetButtonColor(_currentHoveredButton);
            _currentHoveredButton = null;
        }

        // 🔥 상태별 재시작
        if (gameFlowManager.CurrentState == GameState.Tutorial)
        {
            // 튜토리얼 중: 씬 재시작
            Debug.Log("[PauseManager] Restarting tutorial (reloading scene)");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // 메인 게임: 메인 게임만 재시작
            Debug.Log("[PauseManager] Restarting main game");

            if (gameFlowManager)
            {
                gameFlowManager.RestartMainGameOnly();
            }
        }
    }

    public void GoToMainMenu()
    {
        Debug.Log("[PauseManager] Going to main menu");

        Time.timeScale = 1f;

        if (pointerLine)
            pointerLine.enabled = false;

        if (gameFlowManager)
        {
            gameFlowManager.GoToMainMenu();
        }
    }

    void PauseAllAudio()
    {
        Debug.Log($"[PauseManager] Pausing {allAudioSources.Length} audio sources");

        foreach (var source in allAudioSources)
        {
            if (source && source.isPlaying)
            {
                source.Pause();
                Debug.Log($"[PauseManager] Paused: {source.gameObject.name}");
            }
        }
    }

    void ResumeAllAudio()
    {
        Debug.Log($"[PauseManager] Resuming {allAudioSources.Length} audio sources");

        foreach (var source in allAudioSources)
        {
            if (source)
            {
                source.UnPause();
                Debug.Log($"[PauseManager] Resumed: {source.gameObject.name}");
            }
        }
    }

    public bool IsPaused => _isPaused;
}
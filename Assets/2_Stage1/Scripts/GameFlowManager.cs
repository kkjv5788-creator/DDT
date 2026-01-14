using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public TutorialController tutorialController;
    public RadioClickable radio;
    public PlateController plateController;
    public KimbapSpawner spawner;
    public ResultManager resultManager;
    public PauseManager pauseManager;
    public ProgressGaugeUI progressGaugeUI;

    [Header("Data")]
    public RhythmTriggerListSO mainTriggerList;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";

    public GameState CurrentState { get; private set; } = GameState.Tutorial;
    GameState _stateBeforePause;

    bool _mainGameStarted = false;

    void Start()
    {
        if (conductor && plateController)
            conductor.plateController = plateController;

        if (conductor && spawner)
            conductor.spawner = spawner;

        if (tutorialController && radio)
            tutorialController.radio = radio;

        if (resultManager && progressGaugeUI)
        {
            resultManager.OnProgressUpdate.AddListener(progressGaugeUI.UpdateProgress);
        }

        CurrentState = GameState.Tutorial;
        if (tutorialController)
        {
            tutorialController.StartTutorial();
        }

        if (radio)
        {
            radio.OnRadioClicked.AddListener(StartMainGame);
        }

        if (conductor)
        {
            conductor.OnTutorialCompleted.AddListener(OnTutorialCompleted);
            conductor.OnTutorialSkipped.AddListener(OnTutorialCompleted);
        }
    }

    void OnTutorialCompleted()
    {
        Debug.Log("[GameFlowManager] Tutorial completed - Entering WaitForRadio state");
        CurrentState = GameState.WaitForRadio;

        if (radio)
        {
            radio.SetClickable(true);
        }
    }

    void StartMainGame()
    {
        if (_mainGameStarted)
        {
            Debug.Log("[GameFlowManager] Main game already started.");
            return;
        }

        Debug.Log("[GameFlowManager] Starting main game...");

        _mainGameStarted = true;
        CurrentState = GameState.PlayingMain;

        if (radio)
        {
            radio.SetClickable(false);
        }

        if (spawner)
            spawner.DestroyCurrentKimbap();

        if (plateController)
            plateController.ResetToEmptyPlate();

        conductor.isTutorialMode = false;
        conductor.data = mainTriggerList;
        conductor.StartGame();

        if (resultManager && mainTriggerList)
        {
            resultManager.StartTracking(mainTriggerList.triggers.Length);
        }

        Debug.Log("[GameFlowManager] Main game started!");
    }

    public void EnterPaused()
    {
        _stateBeforePause = CurrentState;
        CurrentState = GameState.Paused;
        Debug.Log($"[GameFlowManager] Entered Paused (from {_stateBeforePause})");
    }

    public void ExitPaused()
    {
        CurrentState = _stateBeforePause;
        Debug.Log($"[GameFlowManager] Exited Paused (back to {CurrentState})");
    }

    public void EnterFinalResult(float successRate)
    {
        CurrentState = GameState.FinalResult;
        Debug.Log($"[GameFlowManager] Entered FinalResult (Success Rate: {successRate:F1}%)");

        if (successRate >= 50f)
        {
            Debug.Log("[GameFlowManager] Next stage unlocked!");
        }
    }

    // 🔥 메인 게임만 재시작 (튜토리얼 스킵)
    public void RestartMainGameOnly()
    {
        Debug.Log("[GameFlowManager] Restarting main game only");

        // 상태 초기화
        _mainGameStarted = false;
        CurrentState = GameState.PlayingMain;

        // BGM 정지
        if (conductor && conductor.bgmSource)
        {
            conductor.bgmSource.Stop();
        }

        // 김밥 제거
        if (spawner)
            spawner.DestroyCurrentKimbap();

        // 접시 초기화
        if (plateController)
            plateController.ResetToEmptyPlate();

        // 결과 패널 숨기기
        if (resultManager)
        {
            if (resultManager.panel100k) resultManager.panel100k.SetActive(false);
            if (resultManager.panel50k) resultManager.panel50k.SetActive(false);
            if (resultManager.panel0) resultManager.panel0.SetActive(false);
        }

        // 게이지 초기화
        if (progressGaugeUI)
        {
            progressGaugeUI.UpdateProgress(0, 1, 0f);
        }

        // 라디오 비활성화 (자동 시작)
        if (radio)
        {
            radio.SetClickable(false);
        }

        // 🔥 메인 게임 즉시 시작 (BGM 포함)
        if (conductor && mainTriggerList)
        {
            conductor.isTutorialMode = false;
            conductor.data = mainTriggerList;
            conductor.StartGame(); // 이 안에서 BGM이 처음부터 재생됨
        }

        // 결과 추적 시작
        if (resultManager && mainTriggerList)
        {
            resultManager.StartTracking(mainTriggerList.triggers.Length);
        }

        Debug.Log("[GameFlowManager] Main game restarted with BGM!");
    }

    public void GoToMainMenu()
    {
        Debug.Log("[GameFlowManager] Going to main menu...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}

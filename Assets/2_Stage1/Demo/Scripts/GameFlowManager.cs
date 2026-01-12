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
    public string mainMenuSceneName = "MainMenu"; // 메인 메뉴 씬 이름

    // 상태 관리
    public GameState CurrentState { get; private set; } = GameState.Tutorial;
    GameState _stateBeforePause; // Pause 이전 상태 저장

    bool _mainGameStarted = false;

    void Start()
    {
        // 컴포넌트 연결
        if (conductor && plateController)
            conductor.plateController = plateController;

        if (conductor && spawner)
            conductor.spawner = spawner;

        if (tutorialController && radio)
            tutorialController.radio = radio;

        // ResultManager 이벤트 연결
        if (resultManager && progressGaugeUI)
        {
            resultManager.OnProgressUpdate.AddListener(progressGaugeUI.UpdateProgress);
        }

        // 튜토리얼 시작
        CurrentState = GameState.Tutorial;
        if (tutorialController)
        {
            tutorialController.StartTutorial();
        }

        // 라디오 클릭 이벤트
        if (radio)
        {
            radio.OnRadioClicked.AddListener(StartMainGame);
        }

        // 튜토리얼 완료 이벤트
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

        // 라디오 비활성화
        if (radio)
        {
            radio.SetClickable(false);
        }

        // 초기화
        if (spawner)
            spawner.DestroyCurrentKimbap();

        if (plateController)
            plateController.ResetToEmptyPlate();

        // 메인 게임 시작
        conductor.isTutorialMode = false;
        conductor.data = mainTriggerList;
        conductor.StartGame();

        // 결과 추적 시작
        if (resultManager && mainTriggerList)
        {
            resultManager.StartTracking(mainTriggerList.triggers.Length);
        }

        Debug.Log("[GameFlowManager] Main game started!");
    }

    // Pause 진입
    public void EnterPaused()
    {
        _stateBeforePause = CurrentState;
        CurrentState = GameState.Paused;
        Debug.Log($"[GameFlowManager] Entered Paused (from {_stateBeforePause})");
    }

    // Pause 해제
    public void ExitPaused()
    {
        CurrentState = _stateBeforePause;
        Debug.Log($"[GameFlowManager] Exited Paused (back to {CurrentState})");
    }

    // 최종 결과창 진입
    public void EnterFinalResult(float successRate)
    {
        CurrentState = GameState.FinalResult;
        Debug.Log($"[GameFlowManager] Entered FinalResult (Success Rate: {successRate:F1}%)");

        // TODO: 스테이지 해금 로직 (5장)
        if (successRate >= 50f)
        {
            Debug.Log("[GameFlowManager] Next stage unlocked!");
            // PlayerPrefs.SetInt("Stage2Unlocked", 1);
        }
    }

    // 재시작 (7.5장 규칙)
    public void Restart()
    {
        Debug.Log("[GameFlowManager] Restarting...");

        if (CurrentState == GameState.Tutorial)
        {
            // 튜토리얼 중: 튜토리얼 재시작
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // 튜토리얼 이후: 메인 게임만 재시작
            _mainGameStarted = false;
            CurrentState = GameState.WaitForRadio;

            // BGM 정지
            if (conductor && conductor.bgmSource)
                conductor.bgmSource.Stop();

            // 라디오 활성화
            if (radio)
                radio.SetClickable(true);

            Debug.Log("[GameFlowManager] Restarting from WaitForRadio");
        }
    }

    // 메인 메뉴로 이동
    public void GoToMainMenu()
    {
        Debug.Log("[GameFlowManager] Going to main menu...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}

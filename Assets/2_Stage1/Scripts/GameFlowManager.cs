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

    [Header("New Managers")]
    public HandToolSwitcher toolSwitcher; // [새로 추가] 칼/컨트롤러 교체
    // public ProgressGaugeUI progressGaugeUI; // [삭제됨] StageUIManager가 대신함

    [Header("Data")]
    public RhythmTriggerListSO mainTriggerList;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";

    public GameState CurrentState { get; private set; } = GameState.Tutorial;
    GameState _stateBeforePause;

    bool _mainGameStarted = false;

    void Start()
    {
        // 1. 기본 연결
        if (conductor && plateController)
            conductor.plateController = plateController;

        if (conductor && spawner)
            conductor.spawner = spawner;

        if (tutorialController && radio)
            tutorialController.radio = radio;

        // 2. [변경] 게이지 업데이트를 StageUIManager로 연결
        if (resultManager)
        {
            // ResultManager 이벤트: (현재점수, 총점수, 퍼센트)
            // StageUIManager 함수: (현재, 최대)
            resultManager.OnProgressUpdate.AddListener((curr, total, pct) => 
            {
                if (StageUIManager.Instance) 
                    StageUIManager.Instance.UpdateGauge(curr, total);
            });
        }

        // 3. 초기 상태 설정
        CurrentState = GameState.Tutorial;
        
        // [추가] 시작할 땐 '칼' 들기 + 라디오 가이드 끄기 + 왼손 팁 켜기
        if (toolSwitcher) toolSwitcher.SwitchToKnife();
        if (StageUIManager.Instance) StageUIManager.Instance.SetGuides(false, true);

        // 4. 튜토리얼 시작
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
            radio.SetTutorialCompleted(true);
            radio.SetClickable(true);
        }

        // [추가] 튜토리얼 끝났으니 라디오 쏘라고 가이드 화살표 띄우기
        if (StageUIManager.Instance) StageUIManager.Instance.SetGuides(true, true);
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

        // [추가] 게임 시작: 라디오 가이드 끄기 + 무조건 칼 들기
        if (StageUIManager.Instance) StageUIManager.Instance.SetGuides(false, true);
        if (toolSwitcher) toolSwitcher.SwitchToKnife();

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
        
        // [추가] 일시정지 시 메뉴 눌러야 하니까 컨트롤러 꺼내기
        if (toolSwitcher) toolSwitcher.SwitchToController(); 

        Debug.Log($"[GameFlowManager] Entered Paused (from {_stateBeforePause})");
    }

    public void ExitPaused()
    {
        CurrentState = _stateBeforePause;
        
        // [추가] 일시정지 해제 시 다시 칼 들기
        if (toolSwitcher) toolSwitcher.SwitchToKnife();

        Debug.Log($"[GameFlowManager] Exited Paused (back to {CurrentState})");
    }

    public void EnterFinalResult(float successRate)
    {
        CurrentState = GameState.FinalResult;
        Debug.Log($"[GameFlowManager] Entered FinalResult (Success Rate: {successRate:F1}%)");
        
        // [추가] 결과창에서는 버튼 눌러야 하니 컨트롤러 꺼내기
        if (toolSwitcher) toolSwitcher.SwitchToController();

        if (successRate >= 50f)
        {
            Debug.Log("[GameFlowManager] Next stage unlocked!");
        }
    }

    // 🔥 메인 게임만 재시작
    public void RestartMainGameOnly()
    {
        Debug.Log("[GameFlowManager] Restarting main game only");

        _mainGameStarted = false;
        CurrentState = GameState.PlayingMain;

        // [추가] 재시작이니까 다시 칼 들기
        if (toolSwitcher) toolSwitcher.SwitchToKnife();

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

        // [변경] 게이지 초기화 (StageUIManager 사용)
        if (StageUIManager.Instance)
        {
            StageUIManager.Instance.UpdateGauge(0, 1);
            StageUIManager.Instance.SetGuides(false, true); // 가이드 끄기
        }

        // 라디오 비활성화
        if (radio)
        {
            radio.SetClickable(false);
        }

        // 게임 시작
        if (conductor && mainTriggerList)
        {
            conductor.isTutorialMode = false;
            conductor.data = mainTriggerList;
            conductor.StartGame();
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
        
        // [추가] 로비로 나가니까 컨트롤러 꺼내기
        if (toolSwitcher) toolSwitcher.SwitchToController();

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
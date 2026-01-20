using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public RadioClickable radio;
    public PlateController plateController;
    public KimbapSpawner spawner;
    public ResultManager resultManager;
    public PauseManager pauseManager;

    [Header("UI System")]
    public MissionBoardUI missionBoard;
    public HandToolSwitcher toolSwitcher;

    [Header("Data")]
    public RhythmTriggerListSO mainTriggerList;

    [Header("Settings")]
    public string mainMenuSceneName = "Main_v4.1";

    // ❌ [삭제됨] 내부 enum 정의 삭제 -> GameState.cs를 따르게 됨
    // public enum GameState { Intro, PlayingMain, Paused, FinalResult } 

    public GameState CurrentState { get; private set; } = GameState.Intro;

    GameState _stateBeforePause;

    void OnEnable()
    {
        Debug.Log("[GameFlowManager] 실전 모드 활성화됨");
        
        // GameState.cs에 Intro를 추가했으므로 정상 작동함
        CurrentState = GameState.Intro; 
        
        Invoke(nameof(StartMainGame), 0.5f);
    }

    void OnDisable()
    {
        Debug.Log("[GameFlowManager] 실전 모드 종료");
    }

    public void StartMainGame()
    {
        Debug.Log("[GameFlowManager] 실전 영업 개시!");
        CurrentState = GameState.PlayingMain;

        // 1. 주문서 갱신
        if (missionBoard)
        {
            missionBoard.InitializeUI();
            missionBoard.UpdateHeader("< 점심 주문 >");
            missionBoard.UpdateText("< 점심 주문 >", "주문이 폭주합니다!\n정신 바짝 차리세요!", "");
            missionBoard.ShowSuccessStamp(false);
        }

        // 2. 하드웨어 세팅
        if (radio) radio.SetClickable(false);
        if (toolSwitcher) toolSwitcher.SwitchToKnife();
        if (plateController) plateController.ResetToEmptyPlate();

        // 3. UI 초기화
        if (StageUIManager.Instance)
        {
            StageUIManager.Instance.UpdateGauge(0, 1);
            StageUIManager.Instance.SetGuides(false, true);
        }

        // 4. 리듬 게임 시작
        if (conductor && mainTriggerList)
        {
            conductor.isTutorialMode = false;
            conductor.data = mainTriggerList;
            conductor.StartGame();
        }

        if (resultManager && mainTriggerList)
        {
            resultManager.StartTracking(mainTriggerList.triggers.Length);
        }
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Paused) return;

        _stateBeforePause = CurrentState;
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        
        if (toolSwitcher) toolSwitcher.SwitchToController();
        if (pauseManager) pauseManager.ShowPauseMenu();
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;

        Time.timeScale = 1f;
        CurrentState = _stateBeforePause;
        
        if (toolSwitcher) toolSwitcher.SwitchToKnife();
        if (pauseManager) pauseManager.HidePauseMenu();
    }

    public void EnterFinalResult(float successRate)
    {
        CurrentState = GameState.FinalResult;
        
        if (toolSwitcher) toolSwitcher.SwitchToController();
        
        if (missionBoard)
        {
            missionBoard.UpdateHeader("< 영 업 종 료 >");
            missionBoard.UpdateText("< 영 업 종 료 >", "오늘 장사 끝!\n수고하셨습니다.", "");
            missionBoard.ShowSuccessStamp(true);
        }
    }

    public void RestartMainGameOnly()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
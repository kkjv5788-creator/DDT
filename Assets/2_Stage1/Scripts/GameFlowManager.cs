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

    public GameState CurrentState { get; private set; } = GameState.Intro;

    GameState _stateBeforePause;

    void OnEnable()
    {
        Debug.Log("[GameFlowManager] 실전 모드 활성화됨");
        
        CurrentState = GameState.Intro; 
        
        // 0.5초 뒤 게임 시작
        Invoke(nameof(StartMainGame), 0.5f);
    }

    void OnDisable()
    {
        Debug.Log("[GameFlowManager] 실전 모드 종료");
    }

    public void StartMainGame()
    {
        if (CurrentState == GameState.PlayingMain) return;

        Debug.Log("[GameFlowManager] StartMainGame!");
        CurrentState = GameState.PlayingMain;

        // 1. 도구 칼로 변경
        if (toolSwitcher) toolSwitcher.SwitchToKnife();

        // 2. 리갈패드 업데이트
        if (missionBoard)
        {
            missionBoard.UpdateHeader("< 영 업 중 >");
            missionBoard.UpdateMission("주문이 밀려옵니다!", 0, 0);
        }

        // 3. 데이터 로드 및 음악 재생
        if (conductor && mainTriggerList)
        {
            conductor.data = mainTriggerList;
            
            // PlayBgm 함수가 있다면 실행, 없으면 AudioSource 직접 재생
            // (RhythmConductor 버전에 따라 다를 수 있어 안전하게 처리)
            if (conductor.bgmSource)
            {
                conductor.bgmSource.clip = mainTriggerList.bgm;
                conductor.bgmSource.Play();
            }
            
            // 컨덕터 시작 (노트 내려오기 시작)
            // conductor.Play(); // 필요 시 주석 해제
        }

        // [삭제됨] resultManager.StartTracking(...) 
        // -> ResultManager를 단순화하면서 이 기능이 빠졌으므로 삭제해야 에러가 안 남.
    }

    // 일시정지 기능
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

    // 게임 종료 처리
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
        // 현재 씬 재로딩 (간단한 재시작 구현)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
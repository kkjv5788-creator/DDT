using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // 텍스트 제어를 위해 추가

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

    // 🔥 [추가] 매출 UI 텍스트를 제어하기 위한 변수들
    [Header("Sales UI Control")]
    public GameObject salesUIGroup; 
    public TextMeshProUGUI txtSalesTitle; // "면접 실습" -> "현재 매출"
    public TextMeshProUGUI txtSalesValue; // "진행 중" -> "0원"

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

        // 2. ★ 리갈패드 업데이트 (전체 개수 파악해서 체크박스 그리기)
        int totalCount = 20; // 기본값
        if (mainTriggerList != null && mainTriggerList.triggers != null)
        {
            totalCount = mainTriggerList.triggers.Length;
        }

        if (missionBoard)
        {
            missionBoard.UpdateHeader("< 영 업 중 >");
            // ★ 기존에 0을 넣던 것을 totalCount로 변경 -> 체크박스 생성됨
            missionBoard.UpdateMission("주문이 밀려옵니다!", 0, totalCount);
        }

        // 3. ★ 모니터 UI 텍스트 변경 ("면접 실습" -> "현재 매출")
        if (salesUIGroup) salesUIGroup.SetActive(true);
        if (txtSalesTitle) txtSalesTitle.text = "현재 매출";
        if (txtSalesValue) txtSalesValue.text = "0원";

        // 4. 데이터 로드 및 음악 재생
        if (conductor && mainTriggerList)
        {
            conductor.data = mainTriggerList;
            if (conductor.bgmSource)
            {
                conductor.bgmSource.clip = mainTriggerList.bgm;
                conductor.bgmSource.Play();
            }
        }
    }

    // (나머지 함수들은 기존 유지)
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
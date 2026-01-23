using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameFlowManager : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public ResultManager resultManager;
    public PauseManager pauseManager;
    public HandToolSwitcher toolSwitcher;
    public MissionBoardUI missionBoard;

    [Header("Monitor Panels")]
    public GameObject panelPlayGame;   
    public GameObject panelDialogue;   // 튜토리얼 대화창 (Panel_Dialogue)
    public GameObject panelResult;     // 결과창 (Panel_ClosingResult)

    [Header("Play Game UI")]
    public Slider songProgressSlider;       
    public TextMeshProUGUI textRemainingTime; 
    
    [Header("Sales UI")]
    public GameObject salesInfoGroup;         
    public TextMeshProUGUI textRealtimeSales; 
    public TextMeshProUGUI textFeedback;      

    [Header("Settings")]
    public int pricePerSlice = 5000; // 성공 시 5,000원 고정
    public int mainMenuSceneIndex = 0; 

    public GameState CurrentState { get; private set; } = GameState.Intro;
    GameState _stateBeforePause;
    int _currentSales = 0;
    bool _isGameEnding = false;
    RhythmConductor.RhythmState _lastConductorState;

    void OnEnable()
    {
        CurrentState = GameState.Intro; 
        if (conductor)
        {
            conductor.OnSliceSuccess.AddListener(OnSliceSuccess);
            conductor.OnSliceFail.AddListener(OnSliceFail);
        }
        StartMainGame();
    }

    void OnDisable()
    {
        if (conductor)
        {
            conductor.OnSliceSuccess.RemoveListener(OnSliceSuccess);
            conductor.OnSliceFail.RemoveListener(OnSliceFail);
        }
    }

    void Update()
    {
        if (CurrentState != GameState.PlayingMain || !conductor) return;

        if (conductor.bgmSource && conductor.bgmSource.clip)
        {
            float currentTime = conductor.bgmSource.time;
            float totalTime = conductor.bgmSource.clip.length;
            
            if (!_isGameEnding && !conductor.bgmSource.loop && currentTime >= totalTime - 0.1f)
            {
                _isGameEnding = true;
                if (resultManager) resultManager.EndGame(); 
            }

            if (songProgressSlider) songProgressSlider.value = currentTime / totalTime;
            if (textRemainingTime)
            {
                float remaining = Mathf.Max(0, totalTime - currentTime);
                int min = Mathf.FloorToInt(remaining / 60);
                int sec = Mathf.FloorToInt(remaining % 60);
                textRemainingTime.text = $"{min:00}:{sec:00}";
            }
        }

        if (_lastConductorState != conductor.State)
        {
            _lastConductorState = conductor.State;
            UpdateLegalPadByState();
        }
    }

    void UpdateLegalPadByState()
    {
        if (!missionBoard) return;
        if (conductor.State == RhythmConductor.RhythmState.Waiting)
            missionBoard.UpdateMission("주문 대기 중...", 0, 0);
        else if (conductor.State == RhythmConductor.RhythmState.Guiding || conductor.State == RhythmConductor.RhythmState.Judging)
            UpdateSliceCountUI();
    }

    void OnSliceSuccess(Vector3 pos, Vector3 normal, float speed)
    {
        _currentSales += pricePerSlice; // 단순 가산 방식
        UpdateSalesUI();
        ShowFeedbackText($"+ {pricePerSlice:N0} 원");
        UpdateSliceCountUI();
    }

    void OnSliceFail(Vector3 pos, string reason) => UpdateSliceCountUI();
    void UpdateSalesUI() { if (textRealtimeSales) textRealtimeSales.text = $"{_currentSales:N0} 원"; }

    void ShowFeedbackText(string msg)
    {
        if (textFeedback)
        {
            textFeedback.text = msg;
            textFeedback.gameObject.SetActive(true);
            CancelInvoke(nameof(HideFeedback));
            Invoke(nameof(HideFeedback), 1.0f);
        }
    }
    void HideFeedback() { if (textFeedback) textFeedback.gameObject.SetActive(false); }

    void UpdateSliceCountUI()
    {
        if (!missionBoard || !conductor || conductor.State == RhythmConductor.RhythmState.Waiting) return;
        missionBoard.UpdateMission($"주문 폭주 중!\n<size=120%>[ <color=red><b>{conductor.SliceCount}</b></color> / {conductor.RequiredSliceCount} ]</size>", conductor.SliceCount, conductor.RequiredSliceCount);
    }

    public int GetCurrentSales() => _currentSales;

    public void StartMainGame()
    {
        CurrentState = GameState.PlayingMain;
        _isGameEnding = false;
        _currentSales = 0;
        UpdateSalesUI();

        if (salesInfoGroup) salesInfoGroup.SetActive(true);
        if (panelPlayGame) panelPlayGame.SetActive(true);
        if (panelDialogue) panelDialogue.SetActive(false);
        if (panelResult) panelResult.SetActive(false);

        if (missionBoard) { missionBoard.InitializeUI(); missionBoard.UpdateHeader("*** 주문서 ***"); }

        if (conductor)
        {
            conductor.isTutorialMode = false;
            conductor.StartGame();
            if (conductor.bgmSource) conductor.bgmSource.loop = false; 
        }
        if (resultManager) resultManager.StartTracking(conductor.data.triggers.Length);
    }

    public void PauseGame() { Time.timeScale = 0f; if (toolSwitcher) toolSwitcher.SwitchToController(); if (pauseManager) pauseManager.ShowPauseMenu(); }
    public void ResumeGame() { Time.timeScale = 1f; if (toolSwitcher) toolSwitcher.SwitchToKnife(); if (pauseManager) pauseManager.HidePauseMenu(); }

    public void EnterFinalResult(float successRate)
    {
        CurrentState = GameState.FinalResult;
        if (toolSwitcher) toolSwitcher.SwitchToController();
        if (panelPlayGame) panelPlayGame.SetActive(false);
        if (panelDialogue) panelDialogue.SetActive(false); // 튜토리얼 UI 강제 종료
        if (panelResult) panelResult.SetActive(true);
        if (missionBoard) { missionBoard.UpdateHeader("< 영 업 종 료 >"); missionBoard.ShowSuccessStamp(true); }
    }

    public void RestartMainGameOnly() { gameObject.SetActive(false); gameObject.SetActive(true); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene(mainMenuSceneIndex); }
}
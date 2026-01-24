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
    public GameObject panelDialogue; 
    public GameObject panelResult;   

    [Header("Play Game UI")]
    public Slider songProgressSlider;       
    public TextMeshProUGUI textRemainingTime;
    
    [Header("Sales UI")]
    public GameObject salesInfoGroup;         
    public TextMeshProUGUI textRealtimeSales; 
    public TextMeshProUGUI textFeedback;
    
    [Header("Settings")]
    public int pricePerLine = 5000; 
    public int mainMenuSceneIndex = 0;
    public AudioClip resultBgmClip; 

    public GameState CurrentState { get; private set; } = GameState.Intro;

    GameState _stateBeforePause;
    int _currentSales = 0;
    bool _isGameEnding = false;
    RhythmConductor.RhythmState _lastConductorState;
    
    void Awake()
    {
        if (!conductor) conductor = FindObjectOfType<RhythmConductor>();
        // UI 자동 찾기
        if (!salesInfoGroup || !textRealtimeSales)
        {
            GameObject canvas = GameObject.Find("Monitor_Canvas");
            if (canvas)
            {
                if (!salesInfoGroup) { Transform t = canvas.transform.Find("Sales_Info_Group"); if (t) salesInfoGroup = t.gameObject; }
                if (!textRealtimeSales) { var texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true); foreach (var tx in texts) if (tx.name == "Text_RealtimeSales") { textRealtimeSales = tx; break; } }
                if (!textFeedback) { var texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true); foreach (var tx in texts) if (tx.name == "Text_Feedback") { textFeedback = tx; break; } }
            }
        }
    }
    
    void OnEnable()
    {
        CurrentState = GameState.Intro;
        if (conductor)
        {
            conductor.OnSliceSuccess.AddListener(OnSliceSuccess);
            conductor.OnSliceFail.AddListener(OnSliceFail);
            conductor.OnRoundResult.AddListener(HandleRoundResult);
        }
    }

    void OnDisable()
    {
        if (conductor)
        {
            conductor.OnSliceSuccess.RemoveListener(OnSliceSuccess);
            conductor.OnSliceFail.RemoveListener(OnSliceFail);
            conductor.OnRoundResult.RemoveListener(HandleRoundResult);
        }
    }

    void Update()
    {
        if (CurrentState != GameState.PlayingMain || !conductor) return;

        // 좀비 튜토리얼 모드 방지
        if (conductor.isTutorialMode)
        {
            conductor.isTutorialMode = false;
            if (conductor.bgmSource && conductor.bgmSource.loop) 
                conductor.bgmSource.loop = false;
        }

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
        {
            missionBoard.UpdateMission("주문 대기 중...", 0, 0);
            missionBoard.ShowSuccessStamp(false);
        }
        else if (conductor.State == RhythmConductor.RhythmState.Guiding || conductor.State == RhythmConductor.RhythmState.Judging)
        {
            UpdateSliceCountUI();
            missionBoard.ShowSuccessStamp(false);
        }
    }

    void OnSliceSuccess(Vector3 pos, Vector3 normal, float speed) { UpdateSliceCountUI(); }

    void HandleRoundResult(bool success)
    {
        if (CurrentState != GameState.PlayingMain) return;
        if (success)
        {
            int earnedMoney = pricePerLine;
            _currentSales += earnedMoney;
            UpdateSalesUI();
            string msg = $"<color=#00FF00>판매 완료</color>\n+ {earnedMoney:N0} 원";
            ShowFeedbackText(msg);
            if (missionBoard) missionBoard.ShowSuccessStamp(true);
        }
        else
        {
            string msg = "<color=#FF0000>판매 실패</color>";
            ShowFeedbackText(msg);
            if (missionBoard) missionBoard.ShowSuccessStamp(false);
        }
    }

    void OnSliceFail(Vector3 pos, string reason) => UpdateSliceCountUI();
    void UpdateSalesUI() { if (textRealtimeSales) textRealtimeSales.text = $"{_currentSales:N0} 원"; }
    void ShowFeedbackText(string msg) { if (textFeedback) { textFeedback.text = msg; textFeedback.gameObject.SetActive(true); CancelInvoke(nameof(HideFeedback)); Invoke(nameof(HideFeedback), 2.0f); } }
    void HideFeedback() { if (textFeedback) textFeedback.gameObject.SetActive(false); }
    void UpdateSliceCountUI() { if (!missionBoard || !conductor || conductor.State == RhythmConductor.RhythmState.Waiting) return; missionBoard.UpdateMission($"주문 폭주 중!\n<size=120%>[ <color=red><b>{conductor.SliceCount}</b></color> / {conductor.RequiredSliceCount} ]</size>", conductor.SliceCount, conductor.RequiredSliceCount); }
    public int GetCurrentSales() => _currentSales;
    public void SetState(GameState state) { CurrentState = state; }
    public void EnterTutorialState() { SetState(GameState.Tutorial); }

    public void StartMainGame()
    {
        Time.timeScale = 1f;

        if (!conductor) conductor = FindObjectOfType<RhythmConductor>();
        if (!conductor || conductor.data == null || conductor.data.triggers == null) { Debug.LogError("Cannot start main game"); return; }
        
        if (toolSwitcher) toolSwitcher.SwitchToKnife();

        CurrentState = GameState.PlayingMain;
        _isGameEnding = false;
        _currentSales = 0;
        
        UpdateSalesUI(); 

        if (salesInfoGroup) salesInfoGroup.SetActive(true);
        if (panelPlayGame) panelPlayGame.SetActive(true);
        if (panelDialogue) panelDialogue.SetActive(false);
        if (panelResult) panelResult.SetActive(false);
        
        if (textFeedback) textFeedback.gameObject.SetActive(false);

        if (missionBoard) 
        { 
            missionBoard.InitializeUI(); 
            missionBoard.UpdateHeader("*** 주문서 ***"); 
            missionBoard.UpdateMission("주문 대기 중...", 0, 0); 
            missionBoard.ShowSuccessStamp(false);
        }

        if (conductor)
        {
            // 🔥 [핵심] 꺼진 컨덕터를 켜줍니다.
            conductor.enabled = true;
            
            conductor.isTutorialMode = false;
            conductor.StartGame();
        }
        if (resultManager) resultManager.StartTracking(conductor.data.triggers.Length);
    }

    public void PauseGame()
    {
        _stateBeforePause = CurrentState;
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        if (conductor && conductor.bgmSource) conductor.bgmSource.Pause();
        if (toolSwitcher) toolSwitcher.SwitchToController();
        if (pauseManager) pauseManager.ShowPauseMenu();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        CurrentState = _stateBeforePause;
        if (conductor && conductor.bgmSource) conductor.bgmSource.UnPause();
        if (toolSwitcher) toolSwitcher.SwitchToKnife();
        if (pauseManager) pauseManager.HidePauseMenu();
    }

    public void EnterFinalResult(float successRate)
    {
        Time.timeScale = 1f;
        CurrentState = GameState.FinalResult;
        if (toolSwitcher) toolSwitcher.SwitchToController();
        if (panelPlayGame) panelPlayGame.SetActive(false);
        if (panelDialogue) panelDialogue.SetActive(false); 
        if (panelResult) panelResult.SetActive(true);
        
        if (missionBoard) { missionBoard.UpdateHeader("< 영 업 종 료 >"); missionBoard.UpdateText("", "", ""); missionBoard.ShowSuccessStamp(false); }
        if (conductor && conductor.bgmSource && resultBgmClip != null) { conductor.bgmSource.Stop(); conductor.bgmSource.clip = resultBgmClip; conductor.bgmSource.loop = true; conductor.bgmSource.Play(); }
    }

    public void ResetAndRestartMainGame()
    {
        Time.timeScale = 1f;
        _currentSales = 0;
        _isGameEnding = false;
        _lastConductorState = default;
        CurrentState = GameState.PlayingMain;
        _stateBeforePause = GameState.PlayingMain;

        if (panelResult) panelResult.SetActive(false);
        if (panelDialogue) panelDialogue.SetActive(false);
        if (panelPlayGame) panelPlayGame.SetActive(true);
        if (textFeedback) textFeedback.gameObject.SetActive(false);
        
        if (songProgressSlider) songProgressSlider.value = 0f;
        if (textRemainingTime) textRemainingTime.text = "00:00";
        UpdateSalesUI();
        if (conductor && conductor.bgmSource) conductor.bgmSource.Stop();
        StartMainGame();
    }

    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene(mainMenuSceneIndex); }
}
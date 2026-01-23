using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameState_st2;

/// <summary>
/// Stage2 UI 통합 컨트롤러 (MenuMonitorUI 대체)
/// - Panel_SelectMode: 튜토리얼/메인 선택
/// - Panel_Dialogue: 튜토리얼 안내 텍스트(선택)
/// - Panel_PlayGame: 음악 진행 게이지
/// - Sales_Info_Group: 매출(임금) 누적 + 이번 판정 증가분 표시
/// - Panel_ClosingResult: 결과 화면
/// - Panel_Pause: 일시정지 메뉴
/// </summary>
public class Stage2UIController_st2 : MonoBehaviour
{
    [Header("Panels")]
    public GameObject Panel_SelectMode;
    public GameObject Panel_Dialogue;
    public GameObject Panel_PlayGame;
    public GameObject Sales_Info_Group;
    public GameObject Panel_ClosingResult;
    public GameObject Panel_Pause;

    [Header("SelectMode Buttons")]
    public Button btnTutorial;   // "신입 교육"
    public Button btnMainGame;   // "실전 장사"

    [Header("Pause Buttons")]
    public Button btnResume;
    public Button btnPauseRestart;
    public Button btnPauseMain;

    [Header("ClosingResult Buttons")]
    public Button btnResultRestart;
    public Button btnResultMain;

    [Header("PlayGame Gauge")]
    public Slider songProgressSlider;   // 권장
    public Image songProgressFill;      // Slider 없을 때 대체(선택)
    public TextMeshProUGUI songTimeText; // 선택(있으면 mm:ss 출력)

    [Header("Sales Info")]
    public TextMeshProUGUI totalSalesText;  // 누적
    public TextMeshProUGUI deltaSalesText;  // 이번 추가(+100 등)

    [Header("Closing Result Texts")]
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultBodyText;
    public TextMeshProUGUI resultTotalText;

    [Header("튜토리얼 UI 연결(선택)")]
    [Tooltip("Panel_Dialogue 내부 텍스트를 튜토리얼 컨트롤러에 연결하고 싶다면 넣어주세요.")]
    public TextMeshProUGUI tutorialInstructionText;
    public TextMeshProUGUI tutorialProgressText;
    public TextMeshProUGUI tutorialSkipText;

    private EconomySystem_st2 economy;
    private BagManager_st2 bag;
    private PatternDirector_st2 pattern;
    private JudgeSystem_st2 judge;

    private float _lastWage = 0f;

    public void Initialize(EconomySystem_st2 economySystem, BagManager_st2 bagManager, PatternDirector_st2 patternDirector, JudgeSystem_st2 judgeSystem)
    {
        economy = economySystem;
        bag = bagManager;
        pattern = patternDirector;
        judge = judgeSystem;

        if (economy != null)
        {
            economy.OnWageChanged -= OnWageChanged;
            economy.OnWageChanged += OnWageChanged;
            _lastWage = economy.currentWage;
        }

        if (judge != null)
        {
            judge.OnJudgeResult -= OnJudgeResult;
            judge.OnJudgeResult += OnJudgeResult;
        }

        BindButtonsOnce();
        UpdateSalesTexts(force: true);
    }

    void OnDestroy()
    {
        if (economy != null) economy.OnWageChanged -= OnWageChanged;
        if (judge != null) judge.OnJudgeResult -= OnJudgeResult;
    }

    void Update()
    {
        // PlayGame 게이지는 Playing일 때만 업데이트
        var g = GameFlowController_st2.Instance;
        if (g == null) return;

        if (g.CurrentState == GameStatest2.Playing)
        {
            if (g.GetSongProgress01(out double elapsed, out double total, out double n01))
            {
                SetGauge((float)n01);

                if (songTimeText != null)
                    songTimeText.text = $"{FormatMMSS((float)elapsed)} / {FormatMMSS((float)total)}";
            }
        }
    }

    public void UpdateState(GameStatest2 state)
    {
        // 패널 노출 규칙
        SetActiveSafe(Panel_SelectMode, state == GameStatest2.MainMenu);
        SetActiveSafe(Panel_Pause, state == GameStatest2.Paused);
        SetActiveSafe(Panel_ClosingResult, state == GameStatest2.Result);

        // 플레이 중 UI
        bool showPlayHUD = (state == GameStatest2.Playing);
        SetActiveSafe(Panel_PlayGame, showPlayHUD);

        // 튜토리얼용 UI
        bool showTutorial = (state == GameStatest2.Tutorial);
        SetActiveSafe(Panel_Dialogue, showTutorial);

        // 매출 UI는 메인/튜토리얼 진행 중에만
        bool showSales = (state == GameStatest2.Playing || state == GameStatest2.Tutorial);
        SetActiveSafe(Sales_Info_Group, showSales);

        // Pause/Result일 때 플레이 HUD 숨기기(상호작용 헷갈림 방지)
        if (state == GameStatest2.Paused || state == GameStatest2.Result)
        {
            SetActiveSafe(Panel_PlayGame, false);
            SetActiveSafe(Panel_Dialogue, false);
            SetActiveSafe(Sales_Info_Group, false);
        }

        // 결과 화면 텍스트 갱신
        if (state == GameStatest2.Result)
            RefreshResultTexts();

        // 튜토리얼 텍스트 연결(선택)
        TryBindTutorialTexts();
    }

    void BindButtonsOnce()
    {
        if (btnTutorial != null)
        {
            btnTutorial.onClick.RemoveAllListeners();
            btnTutorial.onClick.AddListener(() => GameFlowController_st2.Instance?.StartTutorialFromMenu());
        }

        if (btnMainGame != null)
        {
            btnMainGame.onClick.RemoveAllListeners();
            btnMainGame.onClick.AddListener(() => GameFlowController_st2.Instance?.StartMainGameFromMenu());
        }

        if (btnResume != null)
        {
            btnResume.onClick.RemoveAllListeners();
            btnResume.onClick.AddListener(() => GameFlowController_st2.Instance?.ResumeGame());
        }

        if (btnPauseRestart != null)
        {
            btnPauseRestart.onClick.RemoveAllListeners();
            btnPauseRestart.onClick.AddListener(() => GameFlowController_st2.Instance?.RestartToSelectMode());
        }

        if (btnPauseMain != null)
        {
            btnPauseMain.onClick.RemoveAllListeners();
            btnPauseMain.onClick.AddListener(() => GameFlowController_st2.Instance?.BackToSelectMode());
        }

        if (btnResultRestart != null)
        {
            btnResultRestart.onClick.RemoveAllListeners();
            btnResultRestart.onClick.AddListener(() => GameFlowController_st2.Instance?.RestartToSelectMode());
        }

        if (btnResultMain != null)
        {
            btnResultMain.onClick.RemoveAllListeners();
            btnResultMain.onClick.AddListener(() => GameFlowController_st2.Instance?.BackToSelectMode());
        }
    }

    void TryBindTutorialTexts()
    {
        var t = GameFlowController_st2.Instance != null ? GameFlowController_st2.Instance.tutorialController : null;
        if (t == null) return;

        // Panel_Dialogue 텍스트를 튜토리얼 컨트롤러가 직접 쓰도록 연결(선택)
        if (tutorialInstructionText != null) t.instructionText = tutorialInstructionText;
        if (tutorialProgressText != null) t.progressText = tutorialProgressText;
        if (tutorialSkipText != null) t.skipText = tutorialSkipText;

        // tutorialCanvas를 Panel_Dialogue로 쓰고 싶다면(선택)
        if (Panel_Dialogue != null) t.tutorialCanvas = Panel_Dialogue;
    }

    // =========================
    // Sales / Result
    // =========================

    void OnWageChanged(int newWage)
    {
        _lastWage = newWage;
        UpdateSalesTexts(force: true);
    }

    void OnJudgeResult(JudgeResult_st2 result, float delta)
    {
        // delta는 현재 구현에서 "판정 차이 시간"일 수 있어 표시값으로는 result 기반 보너스를 쓰는 게 안전
        // 하지만 EconomySystem이 적용 후 OnWageChanged를 주기 때문에 여기서는 '증가분'만 감성적으로 보여준다.
        int add = 0;
        if (economy != null)
        {
            if (result == JudgeResult_st2.Perfect) add = economy.perfectBonus;
            else if (result == JudgeResult_st2.Good) add = economy.goodBonus;
            else if (result == JudgeResult_st2.Miss) add = (economy.penalizeOnMiss ? -economy.missPenalty : 0);
        }

        if (deltaSalesText != null)
        {
            if (add > 0) deltaSalesText.text = $"+{add:n0}";
            else if (add < 0) deltaSalesText.text = $"{add:n0}";
            else deltaSalesText.text = "";
        }

        UpdateSalesTexts(force: true);
    }

    void UpdateSalesTexts(bool force)
    {
        if (totalSalesText != null && economy != null)
            totalSalesText.text = $"{economy.currentWage:n0}원";
    }

    void RefreshResultTexts()
    {
        int earned = (economy != null) ? economy.currentWage : 0;
        int perfect = (judge != null) ? judge.perfectCount : 0;
        int good = (judge != null) ? judge.goodCount : 0;
        int miss = (judge != null) ? judge.missCount : 0;

        if (resultTotalText != null)
            resultTotalText.text = $"{earned:n0}원";

        if (resultTitleText != null || resultBodyText != null)
        {
            // 임의 멘트(원하면 나중에 쉽게 바꿀 수 있게 구조만)
            string title;
            string body;

            if (earned >= 20000)
            {
                title = "오늘 장사 대박!";
                body = $"Perfect {perfect} / Good {good} / Miss {miss}\n이 정도면 단골 생기겠다 👍";
            }
            else if (earned >= 14000)
            {
                title = "괜찮게 벌었다!";
                body = $"Perfect {perfect} / Good {good} / Miss {miss}\n조금만 더 집중하면 더 올라간다!";
            }
            else
            {
                title = "연습이 더 필요해!";
                body = $"Perfect {perfect} / Good {good} / Miss {miss}\n다시 한 판 가자!";
            }

            if (resultTitleText != null) resultTitleText.text = title;
            if (resultBodyText != null) resultBodyText.text = body;
        }
    }

    // =========================
    // Gauge helpers
    // =========================

    void SetGauge(float normalized01)
    {
        if (songProgressSlider != null)
            songProgressSlider.value = normalized01;

        if (songProgressFill != null)
            songProgressFill.fillAmount = normalized01;
    }

    static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }

    static string FormatMMSS(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}

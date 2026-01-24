using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameState_st2;

/// <summary>
/// Stage2 UI 통합 컨트롤러 (MenuMonitorUI 대체)
/// - Panel_SelectMode: 튜토리얼/메인 선택
/// - Panel_Dialogue: 튜토리얼 대사 패널(튜토리얼 컨트롤러가 직접 텍스트를 관리)
/// - Panel_PlayGame: 음악 진행 게이지
/// - Sales_Info_Group: 매출(임금) 누적 + 이번 판정 증가분 표시 (메인 Playing에서만)
/// - Panel_ClosingResult: 결과 화면
/// - Panel_Pause: 일시정지 메뉴(오버레이)
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
    public Button btnPauseMain;  // 메인 씬 로드
    public Button btnPauseQuit;  // ✅ 게임 종료 버튼 추가

    [Header("ClosingResult Buttons")]
    public Button btnResultRestart;
    public Button btnResultMain; // 메인 씬 로드

    [Header("PlayGame Gauge")]
    public Slider songProgressSlider;    // 권장
    public Image songProgressFill;       // Slider 없을 때 대체(선택)
    public TextMeshProUGUI songTimeText; // 선택(있으면 mm:ss 출력)

    [Header("Sales Info")]
    public TextMeshProUGUI totalSalesText;  // 누적
    public TextMeshProUGUI deltaSalesText;  // 이번 추가(+100 등)

    [Header("Closing Result Texts")]
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultBodyText;
    public TextMeshProUGUI resultTotalText;

    private EconomySystem_st2 economy;
    private BagManager_st2 bag;
    private PatternDirector_st2 pattern;
    private JudgeSystem_st2 judge;

    private int _lastWage = 0;

    public void Initialize(
        EconomySystem_st2 economySystem,
        BagManager_st2 bagManager,
        PatternDirector_st2 patternDirector,
        JudgeSystem_st2 judgeSystem)
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
        // ✅ Pause는 "오버레이": 다른 패널 상태를 건드리지 않고 Pause 패널만 켠다.
        if (state == GameStatest2.Paused)
        {
            SetActiveSafe(Panel_Pause, true);
            return;
        }

        // Pause가 아니면 Pause 패널은 끈다.
        SetActiveSafe(Panel_Pause, false);

        // 결과 화면은 Result에서만
        SetActiveSafe(Panel_ClosingResult, state == GameStatest2.Result);

        // SelectMode는 MainMenu에서만
        SetActiveSafe(Panel_SelectMode, state == GameStatest2.MainMenu);

        // 튜토리얼 대사 패널은 Tutorial에서만
        SetActiveSafe(Panel_Dialogue, state == GameStatest2.Tutorial);

        // Play HUD는 Playing에서만
        SetActiveSafe(Panel_PlayGame, state == GameStatest2.Playing);

        // ✅ 매출 UI는 메인 Playing에서만 (튜토리얼에서는 절대 안 켬)
        SetActiveSafe(Sales_Info_Group, state == GameStatest2.Playing);

        // 결과 텍스트 갱신
        if (state == GameStatest2.Result)
            RefreshResultTexts();
    }

    void BindButtonsOnce()
    {
        // Select Mode
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

        // Pause Menu
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
        if (btnPauseQuit != null)
        {
            btnPauseQuit.onClick.RemoveAllListeners();
            btnPauseQuit.onClick.AddListener(QuitGame);
        }

        // ✅ 메인으로 = 메인 씬 로드
        if (btnPauseMain != null)
        {
            btnPauseMain.onClick.RemoveAllListeners();
            btnPauseMain.onClick.AddListener(() => GameFlowController_st2.Instance?.LoadMainScene());
        }

        // Result Menu
        if (btnResultRestart != null)
        {
            btnResultRestart.onClick.RemoveAllListeners();
            btnResultRestart.onClick.AddListener(() => GameFlowController_st2.Instance?.RestartToSelectMode());
        }

        // ✅ 메인으로 = 메인 씬 로드
        if (btnResultMain != null)
        {
            btnResultMain.onClick.RemoveAllListeners();
            btnResultMain.onClick.AddListener(() => GameFlowController_st2.Instance?.LoadMainScene());
        }
    }

    void QuitGame()
    {
        // 혹시 timeScale=0으로 멈춰둔 상태면 정상 종료 전 복구(선택)
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터: 플레이 종료
#else
        Application.Quit(); // 빌드: 앱 종료(퀘스트면 홈으로 나감)
#endif
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
        // delta는 판정 오차시간일 수 있으니 표시값은 result 기반 보너스로 처리
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

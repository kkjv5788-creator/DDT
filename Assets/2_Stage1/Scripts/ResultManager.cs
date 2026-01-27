using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public GameFlowManager gameFlowManager;

    [Header("Monitor UI Elements")]
    public GameObject panelClosingResult; // Panel_ClosingResult
    public TextMeshProUGUI textTitle;      // Text_Title
    public TextMeshProUGUI textFinalTotal; // Text_FinalTotal
    public TextMeshProUGUI textComment;    // Text_GradeComment

    [Header("Buttons")]
    public Button btnRestart;  // 인스펙터에서 연결
    public Button btnMainMenu; // 인스펙터에서 연결

    bool _gameEnded = false;

    void Start()
    {
        if (btnRestart) btnRestart.onClick.AddListener(RestartGame);
        if (btnMainMenu) btnMainMenu.onClick.AddListener(GoToMain);
        if (panelClosingResult) panelClosingResult.SetActive(false);
    }

    public void StartTracking(int total)
    {
        _gameEnded = false;
        if (panelClosingResult) panelClosingResult.SetActive(false);
    }

    public void EndGame()
    {
        if (_gameEnded) return;
        _gameEnded = true;

        if (conductor && conductor.bgmSource) conductor.bgmSource.Stop();
        ShowResult();
    }

    void ShowResult()
    {
        if (panelClosingResult) panelClosingResult.SetActive(true);

        if (!gameFlowManager)
        {
            Debug.LogError("[ResultManager] gameFlowManager not assigned.");
            return;
        }

        int totalSales = gameFlowManager.GetCurrentSales();

        // ✅ Stage1 최고기록(원) 갱신 (로컬 저장)
        BestWageStore_st2.TryUpdateBest("stage1", totalSales);

        if (textTitle) textTitle.text = "< 영 업 정 산 >";
        if (textFinalTotal) textFinalTotal.text = $"{totalSales:N0} 원";

        if (textComment)
        {
            if (totalSales >= 40000) textComment.text = "자네, 우리 가게의 보배구만!";
            else if (totalSales >= 10000) textComment.text = "나쁘지 않아. 조금 더 분발하게.";
            else textComment.text = "이래서 월세나 내겠나? 다시 해보게!";
        }

        gameFlowManager.EnterFinalResult(0);
    }

    void RestartGame()
    {
        if (!gameFlowManager)
        {
            Debug.LogError("[ResultManager] gameFlowManager not assigned.");
            return;
        }
        Time.timeScale = 1f;
        gameFlowManager.ResetAndRestartMainGame();
    }

    void GoToMain()
    {
        if (!gameFlowManager)
        {
            Debug.LogError("[ResultManager] gameFlowManager not assigned.");
            return;
        }

        Time.timeScale = 1f;

        // ✅ Stage1에서 Main으로 돌아감 → 다음 Main은 Lobby 스폰
        if (MainEntryState_st2.Instance != null)
            MainEntryState_st2.Instance.MarkReturnToMain();

        gameFlowManager.GoToMainMenu();
    }
}

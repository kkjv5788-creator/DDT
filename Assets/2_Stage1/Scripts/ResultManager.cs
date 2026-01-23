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
    public TextMeshProUGUI textFinalTotal;  // Text_FinalTotal
    public TextMeshProUGUI textComment;     // Text_GradeComment

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

    public void StartTracking(int total) { _gameEnded = false; if (panelClosingResult) panelClosingResult.SetActive(false); }

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
        int totalSales = gameFlowManager.GetCurrentSales(); 
        
        if (textTitle) textTitle.text = "< 영 업 정 산 >";
        if (textFinalTotal) textFinalTotal.text = $"{totalSales:N0} 원";
        
        if (textComment)
        {
            if (totalSales >= 50000) textComment.text = "자네, 우리 가게의 보배구만!";
            else if (totalSales >= 10000) textComment.text = "나쁘지 않아. 조금 더 분발하게.";
            else textComment.text = "이래서 월세나 내겠나? 다시 해보게!";
        }
        gameFlowManager.EnterFinalResult(0); 
    }

    void RestartGame() { Time.timeScale = 1f; gameFlowManager.RestartMainGameOnly(); }
    void GoToMain() { Time.timeScale = 1f; gameFlowManager.GoToMainMenu(); }
}
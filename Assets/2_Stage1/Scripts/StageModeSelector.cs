using UnityEngine;

public class StageModeSelector : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject selectionPanel;

    [Header("Game Modes")]
    public GameObject tutorialGroup;
    public GameObject mainGameGroup;

    [Header("Shared UI")]
    public MissionBoardUI missionBoard;

    [Header("Refs")]
    public GameFlowManager gameFlowManager;
    public RhythmConductor conductor;
    public RhythmTriggerListSO mainGameData;

    [Header("Tutorial Refs (one of these)")]
    public TutorialDialogueController tutorialDialogueController;
    public TutorialController tutorialController;

    [Header("Ambience")]
    public AudioSource ambientSource;

    void Start()
    {
        if (selectionPanel) selectionPanel.SetActive(true);
        if (tutorialGroup) tutorialGroup.SetActive(false);
        if (mainGameGroup) mainGameGroup.SetActive(false);

        if (missionBoard)
        {
            missionBoard.InitializeUI();
            missionBoard.UpdateHeader("< 대 기 중 >");
            missionBoard.UpdateText("< 대 기 중 >", "모니터에서\n모드를 선택하세요", "");
            missionBoard.ShowSuccessStamp(false);
        }

        if (ambientSource && !ambientSource.isPlaying)
        {
            ambientSource.loop = true;
            ambientSource.Play();
        }
    }

    public void OnClick_StartTutorial()
    {
        Debug.Log(">> 튜토리얼 모드 시작");
        StopAmbience();

        if (selectionPanel) selectionPanel.SetActive(false);
        if (mainGameGroup) mainGameGroup.SetActive(false);
        if (tutorialGroup) tutorialGroup.SetActive(true);

        // (optional) keep state consistent
        if (gameFlowManager) gameFlowManager.EnterTutorialState();

        // Auto-find if not wired
        if (!tutorialDialogueController && tutorialGroup)
            tutorialDialogueController = tutorialGroup.GetComponentInChildren<TutorialDialogueController>(true);
        if (!tutorialController && tutorialGroup)
            tutorialController = tutorialGroup.GetComponentInChildren<TutorialController>(true);

        // Start dialogue first; if no dialogue controller, start tutorial directly
        if (tutorialDialogueController)
            tutorialDialogueController.BeginTutorialFlow();
        else if (tutorialController)
            tutorialController.StartTutorial();
        else
            Debug.LogError("[StageModeSelector] TutorialDialogueController/TutorialController not assigned or found.");
    }

    public void OnClick_StartGame()
    {
        Debug.Log(">> 실전 게임 시작");
        StopAmbience();

        if (selectionPanel) selectionPanel.SetActive(false);
        if (tutorialGroup) tutorialGroup.SetActive(false);
        if (mainGameGroup) mainGameGroup.SetActive(true);

        if (conductor && mainGameData)
            conductor.data = mainGameData;

        if (!gameFlowManager && mainGameGroup)
            gameFlowManager = mainGameGroup.GetComponentInChildren<GameFlowManager>(true);

        if (gameFlowManager)
            gameFlowManager.StartMainGame();
        else
            Debug.LogError("[StageModeSelector] gameFlowManager not assigned/found.");
    }

    void StopAmbience()
    {
        if (ambientSource) ambientSource.Stop();
    }
}

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
    
    [Header("Tutorial Refs")]
    public TutorialDialogueController tutorialDialogueController;
    public TutorialController tutorialController;
    
    [Header("Audio")]
    public AudioSource ambientSource;
    public AudioClip menuBgmClip; 

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

        if (ambientSource)
        {
            if (menuBgmClip != null) ambientSource.clip = menuBgmClip;
            if (!ambientSource.isPlaying)
            {
                ambientSource.loop = true;
                ambientSource.Play();
            }
        }
    }

    public void OnClick_StartTutorial()
    {
        Debug.Log(">> 튜토리얼 모드 시작");
        StopAmbience();

        if (selectionPanel) selectionPanel.SetActive(false);
        if (mainGameGroup) mainGameGroup.SetActive(false);
        if (tutorialGroup) tutorialGroup.SetActive(true);
        
        if (gameFlowManager) gameFlowManager.EnterTutorialState();
        
        // 자동 찾기
        if (!tutorialDialogueController && tutorialGroup)
            tutorialDialogueController = tutorialGroup.GetComponentInChildren<TutorialDialogueController>(true);
        if (!tutorialController && tutorialGroup)
            tutorialController = tutorialGroup.GetComponentInChildren<TutorialController>(true);
        
        if (tutorialDialogueController)
            tutorialDialogueController.BeginTutorialFlow();
        else if (tutorialController)
            tutorialController.StartTutorial();
        else
            Debug.LogError("[StageModeSelector] Tutorial Controllers not found.");
    }

    // ▼▼▼ [핵심 수정] 순서가 바뀐 StartGame 함수 ▼▼▼
    public void OnClick_StartGame()
    {
        Debug.Log(">> 실전 게임 시작");
        StopAmbience();

        if (selectionPanel) selectionPanel.SetActive(false);
        if (tutorialGroup) tutorialGroup.SetActive(false);
        
        // 1. 매니저 먼저 찾기 (비활성 상태에서도 찾기)
        if (!gameFlowManager && mainGameGroup)
            gameFlowManager = mainGameGroup.GetComponentInChildren<GameFlowManager>(true);

        // 2. [중요] 게임을 켜기 전에 데이터를 먼저 셋팅!
        if (conductor)
        {
            if (gameFlowManager) gameFlowManager.conductor = conductor;
            
            // 🔥 [절대 필수] 튜토리얼 모드 강제 해제
            conductor.isTutorialMode = false; 

            // 데이터 주입
            if (mainGameData)
            {
                conductor.data = mainGameData;
                Debug.Log($"[Selector] 데이터 주입 완료: {mainGameData.name}");
            }
            else
            {
                Debug.LogError("[Selector] MainGameData가 연결되지 않았습니다!");
            }
        }

        // 3. 이제 게임 오브젝트를 켭니다. (데이터가 들어간 상태로 깨어남)
        if (mainGameGroup) mainGameGroup.SetActive(true);

        // 4. 게임 시작
        if (gameFlowManager)
        {
            gameFlowManager.StartMainGame();
        }
        else
        {
            Debug.LogError("[StageModeSelector] gameFlowManager not found.");
        }
    }

    void StopAmbience()
    {
        if (ambientSource) ambientSource.Stop();
    }
}
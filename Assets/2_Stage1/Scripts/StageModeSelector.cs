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

// [수정] Start 대신 OnEnable 사용
    // 이 오브젝트가 켜질 때마다(돌아올 때마다) 실행됨
    void OnEnable()
    {
        // 1. 패널 초기화
        if (selectionPanel) selectionPanel.SetActive(true);
        if (tutorialGroup) tutorialGroup.SetActive(false);
        if (mainGameGroup) mainGameGroup.SetActive(false);

        // 2. 미션 보드 초기화
        if (missionBoard)
        {
            missionBoard.InitializeUI();
            missionBoard.UpdateHeader("< 대 기 중 >");
            missionBoard.UpdateText("< 대 기 중 >", "모니터에서\n모드를 선택하세요", "");
            missionBoard.ShowSuccessStamp(false);
        }

        // 3. [핵심] BGM 다시 재생하기
        if (ambientSource)
        {
            if (menuBgmClip != null) ambientSource.clip = menuBgmClip;
            
            // 이미 재생 중이 아니라면 재생!
            if (!ambientSource.isPlaying)
            {
                ambientSource.Play();
            }
        }
    }

    // Start는 이제 비워두거나 다른 초기화 로직만 남깁니다.
    void Start()
    {
        // (OnEnable에서 다 처리했으므로 여기는 비워도 됩니다)
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
using System.Diagnostics;
using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public TutorialController tutorialController;
    public RadioClickable radio;
    public PlateController plateController;
    public KimbapSpawner spawner;

    [Header("Data")]
    public RhythmTriggerListSO mainTriggerList;

    bool _mainGameStarted = false; // 🔥 메인 게임 시작 플래그

    void Start()
    {
        // PlateController를 RhythmConductor에 연결
        if (conductor && plateController)
        {
            conductor.plateController = plateController;
        }

        // Spawner도 연결
        if (conductor && spawner)
        {
            conductor.spawner = spawner;
        }

        // TutorialController에 Radio 연결
        if (tutorialController && radio)
        {
            tutorialController.radio = radio;
        }

        // 튜토리얼부터 시작
        if (tutorialController)
        {
            tutorialController.StartTutorial();
        }

        // 라디오 클릭 이벤트 구독
        if (radio)
        {
            radio.OnRadioClicked.AddListener(StartMainGame);
        }
    }

    void StartMainGame()
    {
        // 🔥 이미 메인 게임이 시작되었으면 무시
        if (_mainGameStarted)
        {
            UnityEngine.Debug.Log("[GameFlowManager] Main game already started. Ignoring radio click.");
            return;
        }

        UnityEngine.Debug.Log("[GameFlowManager] Starting main game...");

        if (!conductor || !mainTriggerList)
        {
            UnityEngine.Debug.LogError("[GameFlowManager] Missing conductor or mainTriggerList.");
            return;
        }

        // 🔥 메인 게임 시작 플래그 설정
        _mainGameStarted = true;

        // 🔥 라디오 비활성화 (더 이상 클릭 불가)
        if (radio)
        {
            radio.SetClickable(false);
        }

        // 🔥 1. 기존 김밥 완전히 제거
        if (spawner)
        {
            spawner.DestroyCurrentKimbap();
            UnityEngine.Debug.Log("[GameFlowManager] Destroyed tutorial kimbap");
        }

        // 🔥 2. 접시 초기화 (빈 접시로)
        if (plateController)
        {
            plateController.ResetToEmptyPlate();
            UnityEngine.Debug.Log("[GameFlowManager] Reset plate to empty");
        }

        // 🔥 3. 메인 게임 데이터 설정
        conductor.isTutorialMode = false;
        conductor.data = mainTriggerList;

        // 🔥 4. 메인 게임 시작 (새 김밥 자동 생성됨)
        conductor.StartGame();

        UnityEngine.Debug.Log("[GameFlowManager] Main game started successfully!");
    }
}
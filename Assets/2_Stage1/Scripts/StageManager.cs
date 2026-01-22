using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Game Modes")]
    public GameObject modeTutorial; // MODE_TUTORIAL 오브젝트
    public GameObject modeGame;     // MODE_GAME 오브젝트

    [Header("System")]
    public RhythmConductor conductor; // GameManager에 있는 컨덕터
    public RhythmTriggerListSO mainGameData; // 본 게임용 데이터 (mainTriggerList)
    
    [Header("UI References")]
    public MissionBoardUI missionBoard; // 리갈패드 초기화용

    // 튜토리얼이 끝나고 트리거를 당기면 이 함수가 호출됩니다.
    public void SwitchToMainGame()
    {
        Debug.Log("[StageManager] Switching to Main Game...");

        // 1. 튜토리얼 모드 끄기
        if (modeTutorial != null) modeTutorial.SetActive(false);

        // 2. 본 게임 모드 켜기
        if (modeGame != null) modeGame.SetActive(true);

        // 3. 리갈패드(주문서) 초기화 (합격 도장 지우기)
        if (missionBoard != null)
        {
            missionBoard.InitializeUI();
            missionBoard.UpdateHeader("< 영업 개시 >");
            missionBoard.UpdateMission("손님들이 몰려옵니다!\n정확하게 서빙하세요.", 0, 0);
        }

        // 4. 리듬 컨덕터 설정 변경 (튜토리얼 -> 본 게임)
        if (conductor != null)
        {
            conductor.isTutorialMode = false; // 튜토리얼 모드 해제
            conductor.data = mainGameData;    // 데이터 교체 (Tutorial -> Main)
            conductor.StartGame();            // 게임 시작!
        }
    }
}
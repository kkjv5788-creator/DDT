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

        // 2. 본 게임 모드 켜기 (GameFlowManager가 깨어남)
        if (modeGame != null) modeGame.SetActive(true);

        // ▼▼▼ [삭제] GameFlowManager가 할 일이므로 StageManager에서는 삭제했습니다. ▼▼▼
        /*
        // 3. 리갈패드(주문서) 초기화
        if (missionBoard != null) { ... }

        // 4. 리듬 컨덕터 설정 변경
        if (conductor != null) { ... }
        */
    }
}
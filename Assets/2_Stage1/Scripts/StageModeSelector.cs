using UnityEngine;

public class StageModeSelector : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject selectionPanel; // Panel_SelectMode

    [Header("Game Modes")]
    public GameObject tutorialGroup;  // MODE_TUTORIAL
    public GameObject mainGameGroup;  // MODE_MAINGAME

    [Header("Shared UI")]
    public MissionBoardUI missionBoard;

    void Start()
    {
        // 1. 초기화: 선택창만 켜고 나머지는 끔
        if (selectionPanel) selectionPanel.SetActive(true);
        if (tutorialGroup) tutorialGroup.SetActive(false);
        if (mainGameGroup) mainGameGroup.SetActive(false);

        // 2. 주문서에 안내 문구 띄우기
        if (missionBoard)
        {
            missionBoard.InitializeUI();
            missionBoard.UpdateHeader("< 대 기 중 >");
            missionBoard.UpdateText("< 대 기 중 >", "모니터에서\n모드를 선택하세요", "");
            missionBoard.ShowSuccessStamp(false);
        }
    }

    // [버튼: 신입 교육]
    public void OnClick_StartTutorial()
    {
        Debug.Log(">> 튜토리얼 모드 시작");
        
        if (selectionPanel) selectionPanel.SetActive(false);
        if (tutorialGroup) tutorialGroup.SetActive(true); // OnEnable() 발동됨
        
        // MissionBoard 텍스트는 TutorialController.OnEnable()이 곧바로 덮어씌움
    }

    // [버튼: 점심 장사]
    public void OnClick_StartGame()
    {
        Debug.Log(">> 실전 게임 시작");

        if (selectionPanel) selectionPanel.SetActive(false);
        if (mainGameGroup) mainGameGroup.SetActive(true); // OnEnable() 발동됨

        // MissionBoard 텍스트는 GameFlowManager.OnEnable()이 곧바로 덮어씌움
    }
}
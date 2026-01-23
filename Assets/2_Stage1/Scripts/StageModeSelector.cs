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

        // 배경음악 재생
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
        if (tutorialGroup) tutorialGroup.SetActive(true); 
    }

    public void OnClick_StartGame()
    {
        Debug.Log(">> 실전 게임 시작");
        StopAmbience();
        if (selectionPanel) selectionPanel.SetActive(false);
        if (mainGameGroup) mainGameGroup.SetActive(true); 
    }

    void StopAmbience()
    {
        if (ambientSource) ambientSource.Stop();
    }
}
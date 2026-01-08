using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("이동 설정")]
    public Transform playerRig;     // 플레이어 (OVRCameraRig)
    public Transform lobbyPos;      // 도착할 위치 (LobbyPosition)

    [Header("UI 그룹")]
    public GameObject titleGroup;   // 타이틀 화면 (꺼질 녀석)
    // public GameObject selectGroup; // 곡 선택 화면 (나중에 켤 녀석 - 지금은 주석처리 하거나 비워도 됨)

    void Start()
    {
        // 시작하면 타이틀은 켜져 있어야 함
        if (titleGroup != null) titleGroup.SetActive(true);
    }

    // [시작하기] 버튼 누르면 실행될 함수
    public void GoToLobby()
    {
        // 1. 플레이어 순간이동 (위치와 회전 복사)
        if (playerRig != null && lobbyPos != null)
        {
            playerRig.position = lobbyPos.position;
            playerRig.rotation = lobbyPos.rotation;
        }

        // 2. 타이틀 화면 끄기 (이제 안 봐도 되니까)
        if (titleGroup != null) titleGroup.SetActive(false);
        
        Debug.Log("문방구 앞으로 이동 완료!");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GoToStage1()
    {
        Debug.Log("게임 시작! Stage1으로 이동합니다.");
        SceneManager.LoadScene("Stage1");
    }
}
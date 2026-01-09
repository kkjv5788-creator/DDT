using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("이동 설정")]
    public Transform playerRig;     // 플레이어 (OVRCameraRig)
    public Transform lobbyPos;      // 도착할 위치 (LobbyPosition)

    [Header("UI 그룹")]
    public GameObject titleGroup;   // 타이틀 화면 (꺼질 녀석)
    // public GameObject selectGroup; // 곡 선택 화면 (나중에 켤 녀석)

    void Start()
    {
        // 1. 시작하면 타이틀 UI 켜기
        if (titleGroup != null) titleGroup.SetActive(true);

        // 2. [추가됨] 플레이어 위치를 강제로 타이틀 공간(지하 500m)으로 이동
        // (개발하다가 실수로 로비에 카메라를 둬도, 시작하면 무조건 타이틀로 옵니다)
        if (playerRig != null)
        {
            playerRig.position = new Vector3(0, -500, 0);
            playerRig.rotation = Quaternion.identity; // 정면 보기
        }
    }

    // [시작하기] 버튼 누르면 실행될 함수
    public void GoToLobby()
    {
        // 1. 플레이어 순간이동 (로비 위치로)
        if (playerRig != null && lobbyPos != null)
        {
            playerRig.position = lobbyPos.position;
            playerRig.rotation = lobbyPos.rotation;
        }

        // 2. 타이틀 화면 끄기
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
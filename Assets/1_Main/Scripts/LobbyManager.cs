using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Settings")]
    public int stage1SceneIndex = 1;
    public float fadeDuration = 1.0f;

    [Header("Locations (Drag & Drop)")]
    public Transform playerRig;    // OVRCameraRig
    public Transform titlePoint;   // 타이틀 화면 위치
    public Transform lobbyPoint;   // 로비(Door 앞) 위치

    [Header("UI Panels (Optional)")]
    public GameObject titleCanvas; // 타이틀 UI
    public GameObject guideUI;     // 문 위 안내 UI

    [Header("Refs")]
    public OVRScreenFade fader;

    private void Start()
    {
        Time.timeScale = 1f;

        // 시작하면 타이틀 위치로 강제 이동 & 안내 UI 숨기기
        if (playerRig && titlePoint)
        {
            playerRig.position = titlePoint.position;
            playerRig.rotation = titlePoint.rotation;
        }
        
        if (titleCanvas) titleCanvas.SetActive(true);
        if (guideUI) guideUI.SetActive(false);
    }

    // 🔥 [수정] 버튼은 이 함수를 불러야 합니다! (Wrapper 함수 추가)
    public void MoveToLobby()
    {
        StartCoroutine(RoutineMoveToLobby());
    }

    // 내부 로직 (버튼이 직접 못 부름)
    IEnumerator RoutineMoveToLobby()
    {
        // 1. 화면 어둡게
        if (fader) fader.FadeOut();
        
        // 2. 대기
        yield return new WaitForSeconds(fadeDuration);

        // 3. 플레이어 이동
        if (playerRig && lobbyPoint)
        {
            playerRig.position = lobbyPoint.position;
            playerRig.rotation = lobbyPoint.rotation;
        }

        // 4. UI 교체
        if (titleCanvas) titleCanvas.SetActive(false); 
        if (guideUI) guideUI.SetActive(true);         

        // 5. 화면 다시 밝게 (Fade In)
        if (fader) fader.FadeIn(); 
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void LoadStage1()
    {
        StartCoroutine(RoutineLoadStage());
    }

    IEnumerator RoutineLoadStage()
    {
        if (fader) fader.FadeOut();
        yield return new WaitForSeconds(fadeDuration);
        SceneManager.LoadScene(stage1SceneIndex);
    }
}
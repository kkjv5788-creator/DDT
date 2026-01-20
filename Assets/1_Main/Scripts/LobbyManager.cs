using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Settings")]
    public int stage1SceneIndex = 1;
    public int stage2SceneIndex = 2; // 🔥 [추가] 스테이지 2 인덱스 (기본값 2)
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

        if (playerRig && titlePoint)
        {
            playerRig.position = titlePoint.position;
            playerRig.rotation = titlePoint.rotation;
        }
        
        if (titleCanvas) titleCanvas.SetActive(true);
        if (guideUI) guideUI.SetActive(false);
    }

    public void MoveToLobby()
    {
        StartCoroutine(RoutineMoveToLobby());
    }

    IEnumerator RoutineMoveToLobby()
    {
        if (fader) fader.FadeOut();
        yield return new WaitForSeconds(fadeDuration);

        if (playerRig && lobbyPoint)
        {
            playerRig.position = lobbyPoint.position;
            playerRig.rotation = lobbyPoint.rotation;
        }

        if (titleCanvas) titleCanvas.SetActive(false); 
        if (guideUI) guideUI.SetActive(true);         

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

    // --- 스테이지 로드 관련 함수들 ---

    public void LoadStage1()
    {
        StartCoroutine(RoutineLoadStage(stage1SceneIndex));
    }

    // 🔥 [추가] 스테이지 2 로드 함수
    public void LoadStage2()
    {
        StartCoroutine(RoutineLoadStage(stage2SceneIndex));
    }

    // [개선] 코드 중복을 피하기 위해 통합된 루틴 사용
    IEnumerator RoutineLoadStage(int sceneIndex)
    {
        if (fader) fader.FadeOut();
        yield return new WaitForSeconds(fadeDuration);
        SceneManager.LoadScene(sceneIndex);
    }
}
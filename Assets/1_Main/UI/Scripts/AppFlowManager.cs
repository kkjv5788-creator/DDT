using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AppFlowManager : MonoBehaviour
{
    [Header("1. 필수 연결 (References)")]
    public Transform playerRig;       
    public Transform lobbyPoint;      
    
    [Header("2. 기능 연결 (Components)")]
    public SmartHeightManager heightManager; 

    [Header("3. 설정 (Settings)")]
    public string stageSceneName = "Stage1"; 
    public float fadeTime = 1.0f;            

    // --- [UI] 시작 버튼 함수 ---
    public void OnClickStartButton()
    {
        StartCoroutine(ProcessToLobby());
    }

    // --- [UI] 종료 버튼 함수 (추가됨!) ---
    public void OnClickQuitButton()
    {
        // 에디터에서도 멈추고, 실제 게임에서도 꺼지게 설정
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // --- [3D] 문(Door) 함수 ---
    public void OnClickDoor()
    {
        StartCoroutine(ProcessToStage());
    }

    // 로직 1: 타이틀 -> 로비
    private IEnumerator ProcessToLobby()
    {
        OVRScreenFade fader = Camera.main.GetComponent<OVRScreenFade>();
        if (fader != null) fader.FadeOut();
        
        yield return new WaitForSeconds(fadeTime);

        if (playerRig != null && lobbyPoint != null)
        {
            playerRig.position = lobbyPoint.position;
            playerRig.rotation = lobbyPoint.rotation;
        }

        if (heightManager != null) heightManager.SwitchToGameHeight(); 

        if (fader != null) fader.FadeIn();
    }

    // 로직 2: 로비 -> 스테이지
    private IEnumerator ProcessToStage()
    {
        OVRScreenFade fader = Camera.main.GetComponent<OVRScreenFade>();
        if (fader != null) fader.FadeOut();

        yield return new WaitForSeconds(fadeTime);

        SceneManager.LoadScene(stageSceneName);
    }
}
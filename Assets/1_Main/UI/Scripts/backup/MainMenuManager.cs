using UnityEngine;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("연결할 것들")]
    public Transform playerRig;       
    public GameObject titleGroup;     
    public OVRScreenFade screenFader; 

    // [추가] 높이 관리자 연결 슬롯
    public SmartHeightManager heightManager; 

    [Header("설정")]
    public Vector3 lobbyPosition = new Vector3(0, 0, 0); 

    public void OnClickStart()
    {
        StartCoroutine(TeleportSequence());
    }

    public void OnClickQuit()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

IEnumerator TeleportSequence()
    {
        if (screenFader != null) screenFader.FadeOut();
        
        float waitTime = screenFader != null ? screenFader.fadeTime : 2.0f;
        yield return new WaitForSeconds(waitTime);

        // --- 이동 시점 ---

        playerRig.position = lobbyPosition;
        playerRig.rotation = Quaternion.identity;

        // [추가] 여기서 키를 1.7m로 변경하라고 명령!
        if (heightManager != null)
        {
            heightManager.SwitchToGameHeight();
        }
        // 혹시 연결 안 했을까봐 비상용으로 직접 GetComponent 시도
        else 
        {
            var manager = playerRig.GetComponent<SmartHeightManager>();
            if(manager != null) manager.SwitchToGameHeight();
        }

        if (titleGroup != null) titleGroup.SetActive(false);

        if (screenFader != null) screenFader.FadeIn();
    }
}
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

    [Header("BGM 설정")]
    public AudioSource bgmAudioSource; // BGM용 AudioSource 연결 (Inspector에서 설정)
    public AudioClip newBGMClip;       // "Dudungtak! Neo Street-2.mp3" 연결

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

    /// <summary>
    /// BGM을 변경하는 메서드 (play_btn의 On Click에서 호출)
    /// </summary>
    public void ChangeBGM()
    {
        if (bgmAudioSource != null && newBGMClip != null)
        {
            // 현재 재생 중인 BGM 정지
            bgmAudioSource.Stop();
            
            // 새 BGM 클립으로 변경
            bgmAudioSource.clip = newBGMClip;
            
            // 재생
            bgmAudioSource.Play();
            
            Debug.Log($"BGM 변경: {newBGMClip.name}");
        }
        else
        {
            Debug.LogWarning("BGM AudioSource 또는 새 클립이 설정되지 않았습니다. Inspector에서 확인하세요.");
        }
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
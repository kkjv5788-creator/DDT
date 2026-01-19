using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Settings")]
    public float fadeDuration = 1.0f;

    [Header("References")]
    public Transform playerRig;   // Player_Core (OVRCameraRig)
    public Transform startPoint;  // 플레이어가 서 있을 위치 (Point_Start)
    public OVRScreenFade fader;   // 페이드 효과 (CenterEyeAnchor)

    private void Start()
    {
        // 1. 시간 정상화
        Time.timeScale = 1f;

        // 2. 플레이어 위치 이동 (핵심!)
        if (playerRig != null && startPoint != null)
        {
            playerRig.position = startPoint.position;
            playerRig.rotation = startPoint.rotation;
        }

        // 3. 화면 밝히기 (Fade In)
        StartCoroutine(RoutineFadeIn());
    }

    IEnumerator RoutineFadeIn()
    {
        yield return new WaitForSeconds(0.5f);
        if (fader != null) fader.FadeIn();
    }
}
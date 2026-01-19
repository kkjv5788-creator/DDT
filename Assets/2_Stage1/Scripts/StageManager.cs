using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Settings")]
    public float fadeDuration = 1.0f;

    [Header("References")]
    public OVRScreenFade fader;   // 페이드 효과 (CenterEyeAnchor)

    IEnumerator Start()
    {
        // 1. 시간 정상화
        Time.timeScale = 1f;

        // 2. 화면 밝히기 (Fade In) 
        // (위치 이동 코드는 삭제했습니다. 이제 배치한 곳에서 바로 시작합니다.)
        yield return new WaitForSeconds(0.5f);
        
        if (fader != null) 
        {
            fader.FadeIn();
        }
    }
}
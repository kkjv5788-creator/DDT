using UnityEngine;
using System.Collections;

public class ButtonFeedback : MonoBehaviour
{
    [Header("설정")]
    public float clickScale = 0.8f; // 눌렸을 때 크기 (80%)
    public AudioClip clickSound;    // 효과음 파일
    private Vector3 originalScale;
    private AudioSource audioSource;

    void Start()
    {
        originalScale = transform.localScale;
        // 오디오 소스 자동 추가
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // 이 함수를 버튼의 OnClick 이벤트에 연결하세요
    public void PlayFeedback()
    {
        StartCoroutine(AnimateButton());
        if (clickSound != null) audioSource.PlayOneShot(clickSound);
    }

    IEnumerator AnimateButton()
    {
        // 1. 작아지기
        transform.localScale = originalScale * clickScale;
        yield return new WaitForSeconds(0.1f); // 0.1초 대기
        // 2. 원래대로 복구
        transform.localScale = originalScale;
    }
}
using UnityEngine;
using UnityEngine.EventSystems; // 마우스/레이저 감지용 필수
using System.Collections;

// 인터페이스 추가: 마우스가 들어오고(Enter) 나가는(Exit) 것을 감지함
public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("1. 애니메이션 설정")]
    public float clickScale = 0.8f;      // 눌렀을 때 크기
    public float hoverScale = 1.1f;      // [추가] 마우스 올렸을 때 크기
    public float animationSpeed = 0.1f;  // 애니메이션 속도

    [Header("2. 사운드 & 진동")]
    public AudioClip clickSound;         // 클릭 소리
    public AudioClip hoverSound;         // [추가] 마우스 올렸을 때 소리 (틱!)
    [Range(0, 1)] public float vibrationStrength = 0.5f; // 진동 세기

    private Vector3 originalScale;
    private AudioSource audioSource;

    void Start()
    {
        originalScale = transform.localScale;
        
        // 오디오 소스 설정
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // --- [1] 호버 기능 (레이저가 버튼 위에 올라감) ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. 커지기 (호버 애니메이션)
        StopAllCoroutines();
        StartCoroutine(AnimateToScale(originalScale * hoverScale));

        // 2. 가벼운 소리 ("틱")
        if (hoverSound != null) audioSource.PlayOneShot(hoverSound);

        // 3. 아주 살짝 진동 ("징")
        StartCoroutine(VibrateController(0.05f, 0.1f)); 
    }

    // --- [2] 호버 해제 (레이저가 벗어남) ---
    public void OnPointerExit(PointerEventData eventData)
    {
        // 원래 크기로 복귀
        StopAllCoroutines();
        StartCoroutine(AnimateToScale(originalScale));
    }

    // --- [3] 클릭 기능 (버튼 누름) ---
    // (이 함수는 인스펙터 OnClick 이벤트에 연결되어 있어야 함)
    public void PlayFeedback()
    {
        // 1. 눌리는 애니메이션
        StartCoroutine(ClickAnimationRoutine());

        // 2. 클릭 소리 ("딸깍")
        if (clickSound != null) audioSource.PlayOneShot(clickSound);

        // 3. 강한 진동 ("지잉!")
        StartCoroutine(VibrateController(0.15f, vibrationStrength));
    }

    // -- 내부 로직: 크기 부드럽게 변경 --
    IEnumerator AnimateToScale(Vector3 targetScale)
    {
        float t = 0;
        Vector3 startScale = transform.localScale;

        while (t < 1)
        {
            t += Time.deltaTime / animationSpeed;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    // -- 내부 로직: 클릭 시 쫀득한 움직임 --
    IEnumerator ClickAnimationRoutine()
    {
        // 작아졌다가
        yield return StartCoroutine(AnimateToScale(originalScale * clickScale));
        // 다시 (호버 상태 크기 or 원래 크기로) 복귀
        yield return StartCoroutine(AnimateToScale(originalScale));
    }

    // -- 내부 로직: 컨트롤러 진동 --
    IEnumerator VibrateController(float duration, float strength)
    {
        // 현재 활성화된 컨트롤러(오른손 or 왼손)에 진동을 줌
        OVRInput.SetControllerVibration(strength, strength, OVRInput.Controller.Active);
        
        // 시간만큼 대기
        yield return new WaitForSeconds(duration);

        // 진동 끄기 (안 끄면 계속 징- 거림)
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.Active);
    }
}
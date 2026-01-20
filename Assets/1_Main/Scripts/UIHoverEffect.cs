using UnityEngine;
using UnityEngine.EventSystems;

// 이 스크립트는 버튼이나 문에 붙이세요.
// 레이저가 올라오면 크기가 커지는 "애니메이션"만 담당합니다.
public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Animation Settings")]
    public float hoverScale = 1.1f;      // 커질 크기 (1.1배)
    public float animationSpeed = 0.15f; // 반응 속도

    private Vector3 originalScale;

    void Start()
    {
        // 원래 크기 기억
        originalScale = transform.localScale;
    }

    // 레이저가 들어왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateToScale(originalScale * hoverScale));
    }

    // 레이저가 나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateToScale(originalScale));
    }

    // 부드럽게 크기 변경하는 로직
    System.Collections.IEnumerator AnimateToScale(Vector3 targetScale)
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
}
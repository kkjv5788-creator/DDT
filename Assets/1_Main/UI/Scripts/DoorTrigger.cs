using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems; // VR 인터랙션 필수

public class DoorTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("설정")]
    public MeshRenderer doorRenderer; // 빛나게 할 문(자기 자신)
    public UnityEvent onClick;        // 클릭 시 실행할 동작

    void Start()
    {
        // 시작할 때는 문을 안 보이게 끕니다.
        if (doorRenderer != null) doorRenderer.enabled = false;
    }

    // 1. 레이저가 문에 닿았을 때 (빛남)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorRenderer != null) doorRenderer.enabled = true;
    }

    // 2. 레이저가 문에서 떨어졌을 때 (꺼짐)
    public void OnPointerExit(PointerEventData eventData)
    {
        if (doorRenderer != null) doorRenderer.enabled = false;
    }

    // 3. 문을 클릭했을 때 (이동)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("문 클릭! 주방으로 이동합니다.");
        onClick.Invoke(); // 연결된 함수 실행
    }
}
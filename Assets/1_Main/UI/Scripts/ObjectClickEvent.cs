using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems; // 레이저와 대화하기 위한 필수 부품

public class ObjectClickEvent : MonoBehaviour, IPointerClickHandler
{
    [Header("클릭하면 실행할 기능")]
    public UnityEvent onClick;

    // 레이저가 "너 클릭됐어!"라고 신호를 보내면 이게 실행됨
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("문이 클릭되었습니다!"); // 확인용 로그
        onClick.Invoke();
    }
}
using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public void OnPointerEnter()
    {
        // 하이라이트 ON
        Debug.Log("Hover Enter: " + name);
    }

    public void OnPointerExit()
    {
        // 하이라이트 OFF
        Debug.Log("Hover Exit: " + name);
    }

    public void OnPointerClick()
    {
        Debug.Log("Clicked: " + name);
        // 여기서 씬 전환 / 문 열기 호출
    }
}

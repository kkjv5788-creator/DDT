using UnityEngine;
using UnityEngine.Events;

public class RadioClickable : MonoBehaviour
{
    public UnityEvent onSingleClick;
    public UnityEvent onDoubleClick;

    [Header("Double Click")]
    public float doubleClickWindow = 0.45f;

    float _lastClickTime = -999f;
    bool _pendingSingle;

    void Update()
    {
        // 단일 클릭 확정(더블클릭 윈도우 지나면)
        if (_pendingSingle && Time.time - _lastClickTime > doubleClickWindow)
        {
            _pendingSingle = false;
            onSingleClick?.Invoke();
        }
    }

    void OnMouseDown()
    {
        InvokeClick();
    }

    public void InvokeClick()
    {
        float now = Time.time;

        if (now - _lastClickTime <= doubleClickWindow)
        {
            _pendingSingle = false;
            _lastClickTime = -999f;
            onDoubleClick?.Invoke();
            return;
        }

        _lastClickTime = now;
        _pendingSingle = true;
    }
}

using UnityEngine;
using UnityEngine.Events;

public class RadioRayAction : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public UnityEvent onRadioClick;

    public void Invoke()
    {
        onRadioClick?.Invoke();
    }
}

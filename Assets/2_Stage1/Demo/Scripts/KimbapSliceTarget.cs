using UnityEngine;

public class KimbapSliceTarget : MonoBehaviour
{
    public KimbapController controller;

    void Awake()
    {
        if (!controller) controller = GetComponentInParent<KimbapController>();
    }
}

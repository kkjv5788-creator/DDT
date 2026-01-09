using UnityEngine;

[DisallowMultipleComponent]
public class KnifeVisualResistance : MonoBehaviour
{
    public Transform knifeVisual;
    public Vector3 localBackAxis = new Vector3(0, 0, -1);
    public float maxOffset = 0.015f;

    float _resistance01;
    Vector3 _initialLocalPos;
    bool _inited;

    void Start()
    {
        if (knifeVisual)
        {
            _initialLocalPos = knifeVisual.localPosition;
            _inited = true;
        }
    }

    public void SetResistance01(float value01)
    {
        _resistance01 = Mathf.Clamp01(value01);
    }

    void LateUpdate()
    {
        if (!knifeVisual || !_inited) return;

        Vector3 axis = localBackAxis.sqrMagnitude < 1e-6f ? Vector3.back : localBackAxis.normalized;
        Vector3 offset = axis * (maxOffset * _resistance01);
        knifeVisual.localPosition = _initialLocalPos + offset;

        // 자연스럽게 풀리게
        _resistance01 = Mathf.MoveTowards(_resistance01, 0f, Time.deltaTime * 6f);
    }
}

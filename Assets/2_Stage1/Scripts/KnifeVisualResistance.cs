using UnityEngine;

public class KnifeVisualResistance : MonoBehaviour
{
    public Transform knifeVisual; // assign model root
    public Vector3 localBackAxis = Vector3.back; // visual moves slightly backward on hit
    public float maxOffset = 0.015f;

    float _timer;
    float _dur;
    float _strength;
    Vector3 _baseLocalPos;

    void Awake()
    {
        if (!knifeVisual) knifeVisual = transform;
        _baseLocalPos = knifeVisual.localPosition;
    }

    public void Play(int milliseconds, float strength01)
    {
        _dur = Mathf.Clamp(milliseconds / 1000f, 0.03f, 0.25f);
        _timer = _dur;
        _strength = Mathf.Clamp01(strength01);
    }

    void LateUpdate()
    {
        if (_timer <= 0f)
        {
            knifeVisual.localPosition = Vector3.Lerp(knifeVisual.localPosition, _baseLocalPos, 0.35f);
            return;
        }

        _timer -= Time.deltaTime;
        float u = 1f - Mathf.Clamp01(_timer / _dur); // 0->1
        // ease in/out
        float k = Mathf.Sin(u * Mathf.PI);
        Vector3 offset = localBackAxis.normalized * (maxOffset * _strength * k);
        knifeVisual.localPosition = _baseLocalPos + offset;
    }
}

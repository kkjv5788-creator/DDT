using UnityEngine;

[DisallowMultipleComponent]
public class KnifeVelocityEstimator : MonoBehaviour
{
    public Transform samplePoint;

    public Vector3 Velocity { get; private set; }
    public float Speed => Velocity.magnitude;

    Vector3 _prevPos;
    bool _hasPrev;

    void Start()
    {
        if (!samplePoint) samplePoint = transform;
        _prevPos = samplePoint.position;
        _hasPrev = true;
    }

    void FixedUpdate()
    {
        if (!samplePoint) return;
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;

        Vector3 p = samplePoint.position;
        if (!_hasPrev)
        {
            _prevPos = p;
            _hasPrev = true;
            Velocity = Vector3.zero;
            return;
        }

        Velocity = (p - _prevPos) / dt;
        _prevPos = p;
    }
}

using UnityEngine;

public class KnifeVelocityEstimator : MonoBehaviour
{
    public Transform samplePoint; // blade tip or knife root
    public float speed { get; private set; }

    Vector3 _prevPos;
    bool _hasPrev;

    void Start()
    {
        if (!samplePoint) samplePoint = transform;
    }

    void Update()
    {
        Vector3 p = samplePoint.position;
        if (_hasPrev)
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            speed = Vector3.Distance(p, _prevPos) / dt;
        }
        _prevPos = p;
        _hasPrev = true;
    }
}

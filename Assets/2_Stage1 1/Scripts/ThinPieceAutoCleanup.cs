using UnityEngine;

public class ThinPieceAutoCleanup : MonoBehaviour
{
    public KimbapController owner;
    public Vector3 flyDirection = Vector3.right;

    public float flySeconds = 0.2f;
    public float lifeSeconds = 1.2f;

    Vector3 _startPos;
    Quaternion _startRot;
    float _t;

    void Start()
    {
        _startPos = transform.position;
        _startRot = transform.rotation;

        // Remove heavy physics by default
        var rb = GetComponent<Rigidbody>();
        if (rb) Destroy(rb);

        // You can add a light collider if you want, but prototype ok without.
        // Disable colliders to avoid unexpected physics
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    void Update()
    {
        _t += Time.deltaTime;

        // Fly animation
        if (_t <= flySeconds && flySeconds > 0f)
        {
            float u = _t / flySeconds;
            float ease = 1f - Mathf.Pow(1f - u, 3f);
            transform.position = _startPos + flyDirection.normalized * (0.06f * ease) + Vector3.up * (0.02f * ease);
            transform.rotation = _startRot * Quaternion.Euler(0f, 0f, 18f * ease);
        }

        if (_t >= lifeSeconds)
        {
            if (owner) owner.NotifyThinPieceDespawned();
            Destroy(gameObject);
        }
    }
}

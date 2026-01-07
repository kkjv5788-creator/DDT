using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KnifePhysicsFollowerOVR : MonoBehaviour
{
    public Transform target;
    public Rigidbody rb;

    [Range(0.01f, 1f)] public float positionLerp = 0.5f;
    [Range(0.01f, 1f)] public float rotationLerp = 0.35f;
    public float maxMoveSpeed = 12f;
    public float maxTurnSpeed = 480f;

    Vector3 _targetPos;
    Quaternion _targetRot;
    bool _hasTargetPose;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!target) return;
        _targetPos = target.position;
        _targetRot = target.rotation;
        _hasTargetPose = true;
    }

    void FixedUpdate()
    {
        if (!rb || !_hasTargetPose) return;

        // --- Position ---
        Vector3 delta = _targetPos - rb.position;
        float maxStep = maxMoveSpeed * Time.fixedDeltaTime;
        Vector3 step = Vector3.ClampMagnitude(delta, maxStep);
        Vector3 nextPos = rb.position + step;
        nextPos = Vector3.Lerp(rb.position, nextPos, positionLerp);
        rb.MovePosition(nextPos);

        // --- Rotation ---
        Quaternion desiredRot = _targetRot;

        Quaternion dq = desiredRot * Quaternion.Inverse(rb.rotation);
        dq.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        float maxAngleStep = maxTurnSpeed * Time.fixedDeltaTime;
        float clamped = Mathf.Clamp(angle, -maxAngleStep, maxAngleStep);
        Quaternion stepRot = Quaternion.AngleAxis(clamped, axis);

        Quaternion nextRot = stepRot * rb.rotation;
        nextRot = Quaternion.Slerp(rb.rotation, nextRot, rotationLerp);
        rb.MoveRotation(nextRot);
    }
}

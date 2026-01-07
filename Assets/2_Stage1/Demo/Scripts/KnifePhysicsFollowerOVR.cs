using UnityEngine;

/// <summary>
/// Rigidbody(칼 물리 프록시)가 목표 Transform(OVRCameraRig의 KnifeGrip_R)을 따라가도록 MovePosition/MoveRotation.
/// 충돌 시에는 물리가 막아주므로 "월드 관통 방지" 효과가 생김.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class KnifePhysicsFollowerOVR : MonoBehaviour
{
    public Transform target;
    public Rigidbody rb;

    [Range(0.01f, 1f)] public float positionLerp = 0.6f;
    [Range(0.01f, 1f)] public float rotationLerp = 0.5f;
    public float maxMoveSpeed = 18f;   // m/s
    public float maxTurnSpeed = 720f;  // deg/s

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!target || !rb) return;

        // Position follow (clamped)
        Vector3 desiredPos = target.position;
        Vector3 delta = desiredPos - rb.position;

        float maxStep = maxMoveSpeed * Time.fixedDeltaTime;
        Vector3 step = Vector3.ClampMagnitude(delta, maxStep);
        Vector3 nextPos = rb.position + step;

        // Smooth
        nextPos = Vector3.Lerp(rb.position, nextPos, positionLerp);
        rb.MovePosition(nextPos);

        // Rotation follow (clamped)
        Quaternion desiredRot = target.rotation;
        float angle;
        Vector3 axis;
        Quaternion dq = desiredRot * Quaternion.Inverse(rb.rotation);
        dq.ToAngleAxis(out angle, out axis);
        if (angle > 180f) angle -= 360f;

        float maxAngleStep = maxTurnSpeed * Time.fixedDeltaTime;
        float clampedAngle = Mathf.Clamp(angle, -maxAngleStep, maxAngleStep);
        Quaternion stepRot = Quaternion.AngleAxis(clampedAngle, axis);

        Quaternion nextRot = stepRot * rb.rotation;
        nextRot = Quaternion.Slerp(rb.rotation, nextRot, rotationLerp);
        rb.MoveRotation(nextRot);
    }
}

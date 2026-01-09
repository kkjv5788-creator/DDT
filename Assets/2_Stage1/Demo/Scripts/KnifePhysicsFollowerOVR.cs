using UnityEngine;

[DisallowMultipleComponent]
public class KnifePhysicsFollowerOVR : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;
    public Rigidbody rb;

    [Header("Tuning")]
    [Range(0f, 1f)] public float positionLerp = 0.512f;
    [Range(0f, 1f)] public float rotationLerp = 0.45f;
    public float maxMoveSpeed = 14f;
    public float maxTurnSpeed = 540f;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!target || !rb) return;

        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;

        // Position follow
        Vector3 toTarget = (target.position - rb.position);
        Vector3 desiredVel = toTarget / dt;
        desiredVel = Vector3.ClampMagnitude(desiredVel, maxMoveSpeed);

        // Blend velocity
        Vector3 vel = Vector3.Lerp(rb.velocity, desiredVel, positionLerp);
        rb.velocity = vel;

        // Rotation follow
        Quaternion current = rb.rotation;
        Quaternion desired = target.rotation;

        Quaternion delta = desired * Quaternion.Inverse(current);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (float.IsNaN(axis.x)) axis = Vector3.up;

        if (angle > 180f) angle -= 360f;

        float desiredAngVelDeg = angle / dt;
        desiredAngVelDeg = Mathf.Clamp(desiredAngVelDeg, -maxTurnSpeed, maxTurnSpeed);

        Vector3 desiredAngVel = axis.normalized * desiredAngVelDeg * Mathf.Deg2Rad;
        Vector3 angVel = Vector3.Lerp(rb.angularVelocity, desiredAngVel, rotationLerp);
        rb.angularVelocity = angVel;
    }
}

using UnityEngine;

namespace Project.Gameplay.Knife
{
    public class KnifePhysicsFollowerOVR : MonoBehaviour
    {
        [Header("Target")]
        public Transform followTarget; // RightControllerAnchor 추천

        [Header("Follow")]
        public float followSharpness = 140f;
        public bool followTargetRotation = true;
        public Vector3 rotationOffsetEuler = Vector3.zero;

        [Header("Collision Blocking")]
        public LayerMask blockMask = ~0;              // 막힘 레이어
        public float skin = 0.0025f;                  // 표면에 살짝 띄우기
        public bool ignoreTriggers = true;

        Rigidbody rb;
        Collider solidCol;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (!rb) rb = gameObject.AddComponent<Rigidbody>();

            rb.useGravity = false;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            solidCol = GetComponent<Collider>();
            if (!solidCol)
                Debug.LogWarning("KnifeRoot에 Solid Collider(Trigger OFF)가 필요합니다.");
        }

        void FixedUpdate()
        {
            if (!followTarget) return;

            Vector3 cur = rb.position;
            Vector3 target = followTarget.position;

            // 부드러운 추종 목표점
            Vector3 desired = Vector3.Lerp(cur, target, 1f - Mathf.Exp(-followSharpness * Time.fixedDeltaTime));
            Vector3 delta = desired - cur;

            // 충돌 차단: 이동 방향으로 SweepTest 후 거리 제한
            if (solidCol && delta.sqrMagnitude > 0.0000001f)
            {
                Vector3 dir = delta.normalized;
                float dist = delta.magnitude;

                RaycastHit hit;
                var qti = ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide;

                // Rigidbody.SweepTest는 붙어있는 Collider 기준으로 스윕해줌
                if (rb.SweepTest(dir, out hit, dist + skin, qti))
                {
                    // blockMask 필터
                    if (((1 << hit.collider.gameObject.layer) & blockMask) != 0)
                    {
                        float allowed = Mathf.Max(0f, hit.distance - skin);
                        desired = cur + dir * allowed;
                    }
                }
            }

            rb.MovePosition(desired);

            if (followTargetRotation)
            {
                Quaternion targetRot = followTarget.rotation * Quaternion.Euler(rotationOffsetEuler);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 1f - Mathf.Exp(-followSharpness * Time.fixedDeltaTime)));
            }
        }
    }
}

using UnityEngine;

namespace Project.Gameplay.Knife
{
    public class KnifeVelocityEstimator : MonoBehaviour
    {
        public float speedSmoothing = 0.25f; // 0~1

        Vector3 lastPos;
        float lastTime;
        float smoothedSpeed;

        public float CurrentSpeed => smoothedSpeed;

        void OnEnable()
        {
            lastPos = transform.position;
            lastTime = Time.time;
            smoothedSpeed = 0f;
        }

        void Update()
        {
            float t = Time.time;
            float dt = Mathf.Max(0.0001f, t - lastTime);
            float inst = Vector3.Distance(transform.position, lastPos) / dt;

            smoothedSpeed = Mathf.Lerp(smoothedSpeed, inst, 1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, speedSmoothing)));

            lastPos = transform.position;
            lastTime = t;
        }
    }
}

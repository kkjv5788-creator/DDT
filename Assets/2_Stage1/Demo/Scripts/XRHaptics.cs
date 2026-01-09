using UnityEngine;

namespace Project.Gameplay
{
    public class XRHaptics : MonoBehaviour
    {
        [Header("Right hand haptics (Quest)")]
        public float minAmp = 0.15f;
        public float maxAmp = 0.8f;

        public void PlayTwoStepImpact(float knifeSpeed)
        {
            float a = Mathf.InverseLerp(0.5f, 2.5f, knifeSpeed);
            float amp = Mathf.Lerp(minAmp, maxAmp, a);

#if UNITY_ANDROID || UNITY_EDITOR
            // ¡°Å¹¡±
            OVRInput.SetControllerVibration(0.05f, amp, OVRInput.Controller.RTouch);
            // stop then ¡°¶Ç°¢¡±
            Invoke(nameof(SecondTap), 0.06f);
#endif
        }

        void SecondTap()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            OVRInput.SetControllerVibration(0.08f, 0.35f, OVRInput.Controller.RTouch);
            Invoke(nameof(StopAll), 0.10f);
#endif
        }

        public void PlayWeakBuzz()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            OVRInput.SetControllerVibration(0.04f, 0.15f, OVRInput.Controller.RTouch);
            Invoke(nameof(StopAll), 0.06f);
#endif
        }

        public void PlayWarningTap()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            OVRInput.SetControllerVibration(0.06f, 0.25f, OVRInput.Controller.RTouch);
            Invoke(nameof(StopAll), 0.08f);
#endif
        }

        void StopAll()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
#endif
        }
    }
}

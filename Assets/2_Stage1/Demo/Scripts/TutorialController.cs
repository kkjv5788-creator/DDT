using UnityEngine;
using Project.Core;
using Project.Data;

namespace Project.Core
{
    public class TutorialController : MonoBehaviour
    {
        [Header("Config (optional fallback)")]
        public FeedbackSetSO fallbackFeedbackSet;

        float lastSkipTime = -999f;
        float skipCooldown = 0.5f;

        bool isTutorialMode;

        void OnEnable()
        {
            GameEvents.ModeChanged += OnModeChanged;
            GameEvents.FeedbackSetChanged += OnFeedbackSetChanged;
        }

        void OnDisable()
        {
            GameEvents.ModeChanged -= OnModeChanged;
            GameEvents.FeedbackSetChanged -= OnFeedbackSetChanged;
        }

        void OnModeChanged(bool isTutorial)
        {
            isTutorialMode = isTutorial;
        }

        void OnFeedbackSetChanged(FeedbackSetSO set)
        {
            var use = set ? set : fallbackFeedbackSet;
            if (use) skipCooldown = Mathf.Max(0.05f, use.skipInputCooldown);
        }

        void Update()
        {
            if (!isTutorialMode) return;

            // Meta Quest A button
            bool aDown = false;
#if UNITY_ANDROID || UNITY_EDITOR
            // Oculus Integration required
            aDown = OVRInput.GetDown(OVRInput.Button.One);
#endif
            if (!aDown) return;

            if (Time.time - lastSkipTime < skipCooldown) return;
            lastSkipTime = Time.time;

            GameEvents.RaiseTutorialSkipped();
        }
    }
}

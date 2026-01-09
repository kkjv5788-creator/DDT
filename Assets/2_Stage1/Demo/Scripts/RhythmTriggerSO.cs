using UnityEngine;

namespace Project.Data
{
    [CreateAssetMenu(menuName = "Project/Rhythm/RhythmTriggerSO")]
    public class RhythmTriggerSO : ScriptableObject
    {
        public string id = "T01";

        [Header("Guide")]
        public AudioClip guideSfx;
        public float guideLeadTime = 0.45f;

        [Header("Judging")]
        public int targetSlices = 3;
        public float judgeTimeSeconds = 2.0f;

        [Header("Speed / Attempt")]
        public float minKnifeSpeed = 1.2f;
        public float sliceCooldownSeconds = 0.12f;

        [Header("Optional Contact Time")]
        public bool useContactTime = false;
        public int minContactMs = 50;

        [Header("Tutorial Assist")]
        public bool allowAssistOverrides = true;
    }
}

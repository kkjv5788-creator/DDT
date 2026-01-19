using UnityEngine;

[CreateAssetMenu(menuName = "KimbapRhythm/Rhythm Trigger List", fileName = "RhythmTriggerList")]
public class RhythmTriggerListSO : ScriptableObject
{
    public AudioClip bgm;
    public float timingOffsetMs = 0f;

    public Trigger[] triggers;

    [System.Serializable]
    public class Trigger
    {
        [Header("Time")]
        public float triggerTime;      // seconds on BGM timeline
        public float guideDuration = 0.4f;
        public float judgeDuration = 0.6f;
        public AudioClip guideBeatSound;

        [Header("Counts")]
        public int requiredSliceCount = 5;

        [Header("Judgement (simple)")]
        public float minKnifeSpeed = 1.0f; // m/s-ish (depends on scale)
        public float minContactMs = 10f;

        [Header("RightThin Slice (fixed)")]
        public float thinSliceThicknessNorm = 0.08f;
        public AnimationCurve thinThicknessCurve; // optional
        public float minThinThicknessWorld = 0.01f;
        public int maxActiveThinPieces = 6;

        [Header("Feedback")]
        [Range(0f, 1f)] public float hapticHitBase = 0.25f;
        [Range(0f, 1f)] public float hapticHitMax = 0.75f;
        public int hapticDurationMs = 25;
        public AudioClip impactSound;
        public AudioClip swishSound;
        public GameObject cutVfxPrefab;
        [Range(0f, 1f)] public float visualResistanceStrength = 0.6f;
        public int visualResistanceMs = 80;
    }
}

using System;
using UnityEngine;

[CreateAssetMenu(menuName = "KimbapRhythm/Rhythm Trigger List", fileName = "RhythmTriggerListSO")]
public class RhythmTriggerListSO : ScriptableObject
{
    public AudioClip bgm;
    public bool loopBgm = false;                 // Tutorial에서 true 권장
    public float loopLengthOverride = 0f;        // 0이면 clip.length 사용
    public int timingOffsetMs = 0;

    public Trigger[] triggers;

    [Serializable]
    public class Trigger
    {
        public float triggerTime = 1.0f;
        public int requiredSliceCount = 3;

        [Header("Guide / Judge")]
        public AudioClip guideBeatSound;
        public float guideDuration = 0.35f;
        public float judgeDuration = 2.5f;

        [Header("EzySlice Thin params")]
        public float thinSliceThicknessNorm = 0.12f;
        public AnimationCurve thinThicknessCurve;
        public float minThinThicknessWorld = 0.01f;
        public int maxActiveThinPieces = 8;

        [Header("Feedback (Optional)")]
        public AudioClip hitSfx;
        public AudioClip wrongCutSfx;
        public AudioClip successSfx;
        public AudioClip failSfx;

        public GameObject cutVfxPrefab;
        public GameObject wrongCutVfxPrefab;
        public GameObject successVfxPrefab;
        public GameObject failVfxPrefab;
    }

    public float GetLoopLength()
    {
        if (loopLengthOverride > 0f) return loopLengthOverride;
        return bgm ? bgm.length : 0f;
    }
}

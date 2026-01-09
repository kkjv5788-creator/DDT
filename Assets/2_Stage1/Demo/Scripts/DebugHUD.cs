using UnityEngine;
using Project.Core;
using Project.Gameplay.Knife;

namespace Project.UI
{
    public class DebugHUD : MonoBehaviour
    {
        public RhythmConductor conductor;
        public KnifeVelocityEstimator knifeVel;
        public AudioSource bgmSource;

        RhythmState state;
        bool isTutorial;

        void OnEnable()
        {
            GameEvents.StateChanged += s => state = s;
            GameEvents.ModeChanged += t => isTutorial = t;
        }

        void OnDisable()
        {
            GameEvents.StateChanged -= s => state = s; // (이벤트 람다는 해제가 안 되니 실제론 안 써도 됨)
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 520, 320), GUI.skin.box);

            GUILayout.Label($"Mode: {(isTutorial ? "Tutorial" : "Main/Idle")}");
            GUILayout.Label($"State: {state}");

            // Knife runtime params
            GUILayout.Label($"Judging Runtime: {KnifeSlicerRuntime.IsJudging}");
            GUILayout.Label($"minSpeed: {KnifeSlicerRuntime.MinKnifeSpeed:F2}");
            GUILayout.Label($"cooldown: {KnifeSlicerRuntime.SliceCooldownSeconds:F2}");
            GUILayout.Label($"contactTime: {(KnifeSlicerRuntime.UseContactTime ? $"ON ({KnifeSlicerRuntime.MinContactMs}ms)" : "OFF")}");

            // Knife speed
            float spd = knifeVel ? knifeVel.CurrentSpeed : -1f;
            GUILayout.Label($"Knife Speed: {(spd < 0 ? "NOT SET" : spd.ToString("F2"))}");

            // Conductor info
            if (conductor)
            {
                GUILayout.Label($"Success: {conductor.SuccessCount} / {conductor.TargetSlices}");
                GUILayout.Label($"JudgeTimeLeft: {conductor.JudgeTimeLeft:F2}s");
            }
            else GUILayout.Label("Conductor: NOT SET");

            // BGM runtime
            if (bgmSource && bgmSource.clip)
            {
                GUILayout.Label($"BGM: {bgmSource.clip.name}");
                GUILayout.Label($"BGM Time: {bgmSource.time:F2} / {bgmSource.clip.length:F2}");
                GUILayout.Label($"BGM Playing: {bgmSource.isPlaying}");
            }
            else GUILayout.Label("BGM: NOT SET");

            GUILayout.EndArea();
        }
    }
}

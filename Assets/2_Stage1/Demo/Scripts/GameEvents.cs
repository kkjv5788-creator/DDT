using System;
using UnityEngine;
using Project.Data;
using Project.Gameplay.Kimbap;

namespace Project.Core
{
    public enum RhythmState { Waiting, Guiding, Judging, Result, Cleanup }

    public enum SliceFailReason { SpeedTooLow, ContactTooShort, NotJudging, NoTarget }

    public static class GameEvents
    {
        // Mode / flow
        public static event Action<bool> ModeChanged; // isTutorial
        public static event Action<RhythmState> StateChanged;
        public static event Action<RhythmTriggerSO> TriggerStarted;
        public static event Action<bool> RoundResult; // success

        // Slice attempt & results
        public static event Action<KimbapSliceTarget> SliceAttemptStarted;
        public static event Action<Vector3, Vector3, float> SliceSuccess; // hitPos, hitNormal, knifeSpeed
        public static event Action<Vector3, SliceFailReason> SliceFail;   // pos, reason
        public static event Action<Vector3> WrongCut;

        // Tutorial / radio
        public static event Action TutorialSkipped;
        public static event Action TutorialCompleted;
        public static event Action MainStartRequested;

        // Feedback set
        public static event Action<FeedbackSetSO> FeedbackSetChanged;

        // Raise helpers
        public static void RaiseModeChanged(bool isTutorial) => ModeChanged?.Invoke(isTutorial);
        public static void RaiseStateChanged(RhythmState s) => StateChanged?.Invoke(s);
        public static void RaiseTriggerStarted(RhythmTriggerSO t) => TriggerStarted?.Invoke(t);
        public static void RaiseRoundResult(bool success) => RoundResult?.Invoke(success);

        public static void RaiseSliceAttemptStarted(KimbapSliceTarget t) => SliceAttemptStarted?.Invoke(t);
        public static void RaiseSliceSuccess(Vector3 p, Vector3 n, float v) => SliceSuccess?.Invoke(p, n, v);
        public static void RaiseSliceFail(Vector3 p, SliceFailReason r) => SliceFail?.Invoke(p, r);
        public static void RaiseWrongCut(Vector3 p) => WrongCut?.Invoke(p);

        public static void RaiseTutorialSkipped() => TutorialSkipped?.Invoke();
        public static void RaiseTutorialCompleted() => TutorialCompleted?.Invoke();
        public static void RaiseMainStartRequested() => MainStartRequested?.Invoke();

        public static void RaiseFeedbackSetChanged(FeedbackSetSO set) => FeedbackSetChanged?.Invoke(set);
    }
}

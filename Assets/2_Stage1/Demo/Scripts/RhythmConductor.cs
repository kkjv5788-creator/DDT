using System.Diagnostics;
using UnityEngine;

public class RhythmConductor : MonoBehaviour
{
    public enum RhythmState { Waiting, Guiding, Judging, Result }

    [Header("Data")]
    public RhythmTriggerListSO data;

    [Header("Refs")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public KimbapSpawner spawner;
    public DebugHUD hud;

    public RhythmState State { get; private set; } = RhythmState.Waiting;
    public int CurrentTriggerIndex { get; private set; } = -1;

    public float BgmTime { get; private set; } // seconds, with offset applied
    public int SliceCount { get; private set; }
    public int RequiredSliceCount { get; private set; }

    float _offsetSec;
    float _stateEndTime; // in BgmTime domain
    bool _bgmStarted;

    void Awake()
    {
        if (!bgmSource) bgmSource = gameObject.AddComponent<AudioSource>();
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
        _offsetSec = (data ? data.timingOffsetMs : 0f) / 1000f;

        if (hud) hud.Bind(this);
    }

    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        if (!data || !data.bgm)
        {
            UnityEngine.Debug.LogError("[RhythmConductor] Missing data/bgm.");
            return;
        }

        bgmSource.clip = data.bgm;
        bgmSource.Play();
        _bgmStarted = true;

        State = RhythmState.Waiting;
        CurrentTriggerIndex = -1;
        SliceCount = 0;
        RequiredSliceCount = 0;

        // Spawn first Kimbap
        if (spawner) spawner.EnsureKimbapExists();
    }

    void Update()
    {
        if (!_bgmStarted) return;

        // Use AudioSource.time for simplicity in editor/simulator.
        // (Later you can upgrade to DSP time if needed.)
        BgmTime = bgmSource.time + _offsetSec;

        AdvanceTriggerIfNeeded();
        TickStateMachine();
    }

    void AdvanceTriggerIfNeeded()
    {
        if (data.triggers == null || data.triggers.Length == 0) return;

        int nextIndex = CurrentTriggerIndex + 1;
        if (nextIndex >= data.triggers.Length) return;

        if (BgmTime >= data.triggers[nextIndex].triggerTime)
        {
            EnterTrigger(nextIndex);
        }
    }

    void EnterTrigger(int idx)
    {
        CurrentTriggerIndex = idx;
        var t = data.triggers[idx];

        SliceCount = 0;
        RequiredSliceCount = Mathf.Max(1, t.requiredSliceCount);

        // Prepare Kimbap for judging
        if (spawner && spawner.CurrentKimbap)
        {
            spawner.CurrentKimbap.BeginTrigger(t);
        }

        // Guiding state
        State = RhythmState.Guiding;
        _stateEndTime = BgmTime + Mathf.Max(0.01f, t.guideDuration);

        if (t.guideBeatSound && sfxSource) sfxSource.PlayOneShot(t.guideBeatSound);

        if (hud) hud.Log($"Enter Trigger #{idx} (req={RequiredSliceCount})");
    }

    void TickStateMachine()
    {
        if (CurrentTriggerIndex < 0) return;
        var t = data.triggers[CurrentTriggerIndex];

        if (State == RhythmState.Guiding && BgmTime >= _stateEndTime)
        {
            State = RhythmState.Judging;
            _stateEndTime = BgmTime + Mathf.Max(0.01f, t.judgeDuration);

            if (spawner && spawner.CurrentKimbap)
                spawner.CurrentKimbap.SetSliceable(true);

            if (hud) hud.Log("State -> Judging");
        }
        else if (State == RhythmState.Judging && BgmTime >= _stateEndTime)
        {
            State = RhythmState.Result;

            if (spawner && spawner.CurrentKimbap)
                spawner.CurrentKimbap.SetSliceable(false);

            bool success = (SliceCount == RequiredSliceCount);
            if (spawner && spawner.CurrentKimbap)
                spawner.CurrentKimbap.OnTriggerResult(success);

            if (hud) hud.Log($"State -> Result (success={success})");

            // Immediately go back to Waiting for next trigger (keeps flow)
            State = RhythmState.Waiting;
        }
    }

    public bool IsJudgingWindow()
    {
        return State == RhythmState.Judging && CurrentTriggerIndex >= 0;
    }

    public RhythmTriggerListSO.Trigger GetCurrentTrigger()
    {
        if (!data || data.triggers == null) return null;
        if (CurrentTriggerIndex < 0 || CurrentTriggerIndex >= data.triggers.Length) return null;
        return data.triggers[CurrentTriggerIndex];
    }

    // Called by Knife slicer on valid hit
    public void RegisterValidSlice()
    {
        var t = GetCurrentTrigger();
        if (t == null) return;

        SliceCount++;
        if (hud) hud.Log($"ValidSlice! ({SliceCount}/{RequiredSliceCount})");
    }
}

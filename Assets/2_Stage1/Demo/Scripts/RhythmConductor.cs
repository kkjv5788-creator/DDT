using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;

public class RhythmConductor : MonoBehaviour
{
    public enum RhythmState { Waiting, Guiding, Judging, Result, Cleanup }

    [Header("Data")]
    public RhythmTriggerListSO data;

    [Header("Refs")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public KimbapSpawner spawner;
    public DebugHUD hud;
    public PlateController plateController;

    [Header("Events")]
    public UnityEvent<Vector3, Vector3, float> OnSliceSuccess;
    public UnityEvent<Vector3, string> OnSliceFail;
    public UnityEvent<bool> OnRoundResult;
    public UnityEvent<Vector3> OnWrongCut;
    public UnityEvent OnTutorialSkipped;
    public UnityEvent OnTutorialCompleted;

    [Header("Settings")]
    public float resultDisplayDuration = 0.5f;

    [Header("Tutorial Mode")]
    public bool isTutorialMode = false;

    public RhythmState State { get; private set; } = RhythmState.Waiting;
    public int CurrentTriggerIndex { get; private set; } = -1;

    public float BgmTime { get; private set; }
    public int SliceCount { get; private set; }
    public int RequiredSliceCount { get; private set; }

    float _offsetSec;
    float _stateEndTime;
    bool _bgmStarted;
    bool _waitingForManualAdvance = false;

    void Awake()
    {
        if (!bgmSource) bgmSource = gameObject.AddComponent<AudioSource>();
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
        _offsetSec = (data ? data.timingOffsetMs : 0f) / 1000f;

        if (hud) hud.Bind(this);
    }

    void Start()
    {
        // GameFlowManager에서 호출하도록 변경
        // StartGame();
    }

    public void StartGame()
    {
        if (!data)
        {
            UnityEngine.Debug.LogError("[RhythmConductor] Missing data.");
            return;
        }

        // 🔥 BGM 재생 (튜토리얼/메인 모두)
        if (data.bgm)
        {
            bgmSource.clip = data.bgm;
            bgmSource.loop = true; // 튜토리얼은 반복 재생
            bgmSource.Play();
            _bgmStarted = true;
            UnityEngine.Debug.Log($"[RhythmConductor] BGM started - Mode: {(isTutorialMode ? "Tutorial" : "Main")}");
        }
        else
        {
            UnityEngine.Debug.LogWarning("[RhythmConductor] No BGM assigned!");
            _bgmStarted = false;
        }

        State = RhythmState.Waiting;
        CurrentTriggerIndex = -1;
        SliceCount = 0;
        RequiredSliceCount = 0;
        _waitingForManualAdvance = false;

        if (spawner) spawner.EnsureKimbapExists();
        if (plateController) plateController.ResetToEmptyPlate();

        if (hud) hud.Log($"Game Started - Mode: {(isTutorialMode ? "Tutorial" : "Main")}");
    }

    void Update()
    {
        // 🔥 튜토리얼 모드에서는 시간 기반 진행 완전 차단
        if (isTutorialMode)
        {
            // BGM 시간 업데이트만 (디버그용)
            if (_bgmStarted && bgmSource.isPlaying)
            {
                BgmTime = bgmSource.time + _offsetSec;
            }

            // 상태 머신만 작동 (트리거 자동 진행 X)
            TickStateMachine();
            return;
        }

        // 메인 게임 모드: 기존 로직
        if (!_bgmStarted) return;

        BgmTime = bgmSource.time + _offsetSec;

        AdvanceTriggerIfNeeded();
        TickStateMachine();
    }

    void AdvanceTriggerIfNeeded()
    {
        // 🔥 튜토리얼 모드에서는 절대 호출되지 않음
        if (isTutorialMode) return;

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

        // 빈 접시로 초기화
        if (plateController) plateController.ResetToEmptyPlate();

        if (spawner && spawner.CurrentKimbap)
        {
            spawner.CurrentKimbap.BeginTrigger(t);
        }

        State = RhythmState.Guiding;
        _stateEndTime = BgmTime + Mathf.Max(0.01f, t.guideDuration);

        if (t.guideBeatSound && sfxSource) sfxSource.PlayOneShot(t.guideBeatSound);

        if (hud) hud.Log($"Enter Trigger #{idx} (req={RequiredSliceCount}) - State: Guiding");
    }

    void TickStateMachine()
    {
        if (CurrentTriggerIndex < 0) return;
        var t = data.triggers[CurrentTriggerIndex];

        // 🔥 튜토리얼 모드: 수동 진행 대기 중이면 Result 이후 Cleanup만 차단
        // Guiding -> Judging 전환은 정상 작동해야 함!

        // 🔥 튜토리얼 모드: Time.time 기준으로 상태 전환
        float currentTime = isTutorialMode ? Time.time : BgmTime;

        if (State == RhythmState.Guiding && currentTime >= _stateEndTime)
        {
            State = RhythmState.Judging;
            _stateEndTime = currentTime + Mathf.Max(0.01f, t.judgeDuration);

            if (spawner && spawner.CurrentKimbap)
                spawner.CurrentKimbap.SetSliceable(true);

            if (hud) hud.Log("State -> Judging");
        }
        else if (State == RhythmState.Judging && currentTime >= _stateEndTime)
        {
            State = RhythmState.Result;

            if (spawner && spawner.CurrentKimbap)
                spawner.CurrentKimbap.SetSliceable(false);

            bool success = (SliceCount == RequiredSliceCount);
            if (spawner && spawner.CurrentKimbap)
                spawner.CurrentKimbap.OnTriggerResult(success);

            if (hud) hud.Log($"State -> Result (success={success})");

            OnRoundResult?.Invoke(success);

            // 튜토리얼 모드: 결과 후 수동 진행 대기
            if (isTutorialMode)
            {
                _waitingForManualAdvance = true;
            }

            _stateEndTime = currentTime + resultDisplayDuration;
        }
        else if (State == RhythmState.Result && currentTime >= _stateEndTime)
        {
            // 🔥 튜토리얼 모드: Result 이후 즉시 Cleanup 실행 (김밥 교체)
            State = RhythmState.Cleanup;
            if (hud) hud.Log("State -> Cleanup");

            if (spawner)
            {
                spawner.DestroyCurrentKimbap();
                spawner.EnsureKimbapExists();
                if (hud) hud.Log("New kimbap spawned after Result");
            }

            // 🔥 튜토리얼 모드: Cleanup 이후 수동 진행 대기
            if (isTutorialMode)
            {
                State = RhythmState.Waiting;
                // _waitingForManualAdvance는 이미 Result에서 true로 설정됨
                return;
            }

            // 메인 게임: 바로 다음 Waiting으로
            State = RhythmState.Waiting;
        }
    }

    public void RetryCurrentTrigger()
    {
        if (!isTutorialMode)
        {
            UnityEngine.Debug.LogWarning("[RhythmConductor] RetryCurrentTrigger called but not in tutorial mode!");
            return;
        }

        if (CurrentTriggerIndex < 0) return;

        UnityEngine.Debug.Log($"[RhythmConductor] Retrying trigger #{CurrentTriggerIndex}");

        _waitingForManualAdvance = false;

        // 🔥 김밥은 이미 Result -> Cleanup에서 교체되었으므로 재생성 불필요
        // 대신 현재 김밥이 없으면 생성
        if (spawner && !spawner.CurrentKimbap)
        {
            spawner.EnsureKimbapExists();
        }

        // 🔥 Time.time 기준으로 상태 시간 설정
        var t = data.triggers[CurrentTriggerIndex];

        SliceCount = 0;
        RequiredSliceCount = Mathf.Max(1, t.requiredSliceCount);

        if (plateController) plateController.ResetToEmptyPlate();

        if (spawner && spawner.CurrentKimbap)
        {
            spawner.CurrentKimbap.BeginTrigger(t);
        }

        State = RhythmState.Guiding;
        _stateEndTime = Time.time + Mathf.Max(0.01f, t.guideDuration);

        if (t.guideBeatSound && sfxSource) sfxSource.PlayOneShot(t.guideBeatSound);

        if (hud) hud.Log($"Retry Trigger #{CurrentTriggerIndex} - State: Guiding");
    }

    public void AdvanceToNextTrigger()
    {
        if (!isTutorialMode)
        {
            UnityEngine.Debug.LogWarning("[RhythmConductor] AdvanceToNextTrigger called but not in tutorial mode!");
            return;
        }

        _waitingForManualAdvance = false;

        int nextIndex = CurrentTriggerIndex + 1;

        if (nextIndex >= data.triggers.Length)
        {
            UnityEngine.Debug.Log("[RhythmConductor] Tutorial completed - no more triggers");
            return;
        }

        UnityEngine.Debug.Log($"[RhythmConductor] Advancing to next trigger #{nextIndex}");

        // 🔥 김밥은 이미 Result -> Cleanup에서 교체되었으므로 재생성 불필요
        // 대신 현재 김밥이 없으면 생성
        if (spawner && !spawner.CurrentKimbap)
        {
            spawner.EnsureKimbapExists();
        }

        CurrentTriggerIndex = nextIndex;
        var t = data.triggers[nextIndex];

        SliceCount = 0;
        RequiredSliceCount = Mathf.Max(1, t.requiredSliceCount);

        if (plateController) plateController.ResetToEmptyPlate();

        if (spawner && spawner.CurrentKimbap)
        {
            spawner.CurrentKimbap.BeginTrigger(t);
        }

        State = RhythmState.Guiding;
        _stateEndTime = Time.time + Mathf.Max(0.01f, t.guideDuration);

        if (t.guideBeatSound && sfxSource) sfxSource.PlayOneShot(t.guideBeatSound);

        if (hud) hud.Log($"Enter Trigger #{nextIndex} (req={RequiredSliceCount}) - State: Guiding");
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

    public void RegisterValidSlice()
    {
        var t = GetCurrentTrigger();
        if (t == null) return;

        SliceCount++;
        if (hud) hud.Log($"ValidSlice! ({SliceCount}/{RequiredSliceCount})");
    }

    public void NotifySliceSuccess(Vector3 hitPos, Vector3 hitNormal, float knifeSpeed)
    {
        OnSliceSuccess?.Invoke(hitPos, hitNormal, knifeSpeed);
    }

    public void NotifySliceFail(Vector3 hitPos, string reason)
    {
        OnSliceFail?.Invoke(hitPos, reason);
    }

    public void NotifyWrongCut(Vector3 hitPos)
    {
        OnWrongCut?.Invoke(hitPos);
    }
}
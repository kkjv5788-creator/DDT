using System.Collections;
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
    [Tooltip("가이드 비트 사운드 재생 간격 (초)")]
    public float guideBeatInterval = 0.15f;
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
        // StartGame() is called externally
    }

    public void StartGame()
    {
        if (!data)
        {
            UnityEngine.Debug.LogError("[RhythmConductor] Missing data.");
            return;
        }

        _offsetSec = (data ? data.timingOffsetMs : 0f) / 1000f;
        
        // ▼▼▼ [수정] BGM 재생 로직 분기 ▼▼▼
        if (data.bgm)
        {
            bgmSource.clip = data.bgm;
            bgmSource.loop = isTutorialMode; 

            // 1. 튜토리얼: 대화 흐름을 위해 이미 켜져 있으면 끊지 않음
            if (isTutorialMode)
            {
                if (!bgmSource.isPlaying) bgmSource.Play();
            }
            // 2. 실전(메인 게임): 무조건 처음부터 다시 재생 (박자 싱크 맞춤)
            else
            {
                bgmSource.Stop();
                bgmSource.time = 0f;
                bgmSource.Play();
            }

            _bgmStarted = true;
        }
        else
        {
            _bgmStarted = false;
        }

        State = RhythmState.Waiting;
        CurrentTriggerIndex = -1;
        SliceCount = 0;
        RequiredSliceCount = 0;
        _waitingForManualAdvance = false;

        // 게임 시작 시 소리 정리
        StopAllGuideBeats();

        if (spawner) spawner.EnsureKimbapExists();
        if (plateController) plateController.ResetToEmptyPlate();
    }

    void Update()
    {
        if (isTutorialMode)
        {
            if (_bgmStarted && bgmSource.isPlaying)
            {
                BgmTime = bgmSource.time + _offsetSec;
            }
            TickStateMachine();
            return;
        }

        // 메인 게임
        if (!_bgmStarted) return;
        
        // AudioSettings.dspTime을 쓰면 더 정확하지만 일단 time 사용
        BgmTime = bgmSource.time + _offsetSec;

        AdvanceTriggerIfNeeded();
        TickStateMachine();
    }

    void AdvanceTriggerIfNeeded()
    {
        if (isTutorialMode) return;
        if (data.triggers == null || data.triggers.Length == 0) return;

        int nextIndex = CurrentTriggerIndex + 1;
        if (nextIndex >= data.triggers.Length) return;
        
        // 다음 트리거 시간이 되면 진입
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
        
        if (plateController) plateController.ResetToEmptyPlate();
        if (spawner && spawner.CurrentKimbap)
        {
            spawner.CurrentKimbap.BeginTrigger(t);
        }

        State = RhythmState.Guiding;
        _stateEndTime = BgmTime + Mathf.Max(0.01f, t.guideDuration);
        
        // 가이드 비트 재생
        if (t.guideBeatSound && sfxSource)
        {
            StartCoroutine(PlayGuideBeatSoundRepeatedly(t.guideBeatSound, RequiredSliceCount));
        }
    }

    void TickStateMachine()
    {
        if (CurrentTriggerIndex < 0) return;
        var t = data.triggers[CurrentTriggerIndex];

        float currentTime = isTutorialMode ? Time.time : BgmTime;

        if (State == RhythmState.Guiding && currentTime >= _stateEndTime)
        {
            State = RhythmState.Judging;
            _stateEndTime = currentTime + Mathf.Max(0.01f, t.judgeDuration);

            if (spawner && spawner.CurrentKimbap)
                spawner.CurrentKimbap.SetSliceable(true);
        }
        else if (State == RhythmState.Judging && currentTime >= _stateEndTime)
        {
            State = RhythmState.Result;
            if (spawner && spawner.CurrentKimbap)
                spawner.CurrentKimbap.SetSliceable(false);
            
            bool success = (SliceCount == RequiredSliceCount);
            if (spawner && spawner.CurrentKimbap)
                spawner.CurrentKimbap.OnTriggerResult(success);

            OnRoundResult?.Invoke(success);

            if (isTutorialMode) _waitingForManualAdvance = true;
            _stateEndTime = currentTime + resultDisplayDuration;
        }
        else if (State == RhythmState.Result && currentTime >= _stateEndTime)
        {
            State = RhythmState.Cleanup;
            if (spawner)
            {
                spawner.DestroyCurrentKimbap();
                spawner.EnsureKimbapExists();
            }

            if (isTutorialMode)
            {
                State = RhythmState.Waiting;
                return;
            }

            State = RhythmState.Waiting;
        }
    }

    // 튜토리얼용 수동 조작 함수들
    public void RetryCurrentTrigger()
    {
        if (!isTutorialMode) return;
        if (CurrentTriggerIndex < 0) return;

        _waitingForManualAdvance = false;
        if (spawner && !spawner.CurrentKimbap) spawner.EnsureKimbapExists();

        var t = data.triggers[CurrentTriggerIndex];
        SliceCount = 0;
        RequiredSliceCount = Mathf.Max(1, t.requiredSliceCount);

        if (plateController) plateController.ResetToEmptyPlate();
        if (spawner && spawner.CurrentKimbap) spawner.CurrentKimbap.BeginTrigger(t);

        State = RhythmState.Guiding;
        _stateEndTime = Time.time + Mathf.Max(0.01f, t.guideDuration);
        
        if (t.guideBeatSound && sfxSource)
        {
            StartCoroutine(PlayGuideBeatSoundRepeatedly(t.guideBeatSound, RequiredSliceCount));
        }
    }

    public void AdvanceToNextTrigger()
    {
        if (!isTutorialMode) return;

        _waitingForManualAdvance = false;
        int nextIndex = CurrentTriggerIndex + 1;
        if (nextIndex >= data.triggers.Length) return;

        if (spawner && !spawner.CurrentKimbap) spawner.EnsureKimbapExists();

        CurrentTriggerIndex = nextIndex;
        var t = data.triggers[nextIndex];

        SliceCount = 0;
        RequiredSliceCount = Mathf.Max(1, t.requiredSliceCount);

        if (plateController) plateController.ResetToEmptyPlate();
        if (spawner && spawner.CurrentKimbap) spawner.CurrentKimbap.BeginTrigger(t);

        State = RhythmState.Guiding;
        _stateEndTime = Time.time + Mathf.Max(0.01f, t.guideDuration);
        
        if (t.guideBeatSound && sfxSource)
        {
            StartCoroutine(PlayGuideBeatSoundRepeatedly(t.guideBeatSound, RequiredSliceCount));
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

    public void RegisterValidSlice()
    {
        SliceCount++;
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

    IEnumerator PlayGuideBeatSoundRepeatedly(AudioClip clip, int count)
    {
        if (clip == null || sfxSource == null || count <= 0) yield break;
        for (int i = 0; i < count; i++)
        {
            sfxSource.PlayOneShot(clip);
            if (i < count - 1) yield return new WaitForSeconds(guideBeatInterval);
        }
    }

    public void StopAllGuideBeats()
    {
        StopAllCoroutines();
        if (sfxSource) sfxSource.Stop();
    }
}
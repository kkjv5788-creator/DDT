using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameState_st2;
using UnityEngine.Pool;

public class PatternDirector_st2 : MonoBehaviour
{
    [Header("BPM 설정")]
    public float BPM = 120f;
    public float telegraphLeadTime = 0.4f;
    public float cueLeadTime = 0.35f;
    public float runStep = 0.25f;

    [Header("패턴 간격")]
    public int patternIntervalBeats = 3;
    public float minPatternInterval = 0.5f;
    public float initialDelay = 5.0f;

    [Header("특수 패턴 설정")]
    public int specialCooldownBeats = 4;
    public int maxBeatsWithoutSpecial = 12;
    public float sync2Chance = 30f;
    public float run3Chance = 30f;

    [Header("참조")]
    public MoldController_st2[] molds = new MoldController_st2[3];
    public AudioSource cueAudioSource;
    public AudioClip sync2Cue;
    public AudioClip run3Cue;

    private double songStartDspTime;
    private double patternLockUntilDspTime;
    private double nextPatternTime;
    private double lastPatternTime;
    private int beatsSinceLastSpecial = 0;
    private bool isInCooldown = false;

    private List<AudioSource> scheduledSources = new List<AudioSource>();
    private List<Coroutine> scheduledCoroutines = new List<Coroutine>();

    public event Action<PatternType_st2> OnPatternQueued;

    public void StartPatternSystem(double startDspTime)
    {
        songStartDspTime = startDspTime;
        patternLockUntilDspTime = startDspTime + initialDelay;
        nextPatternTime = startDspTime + initialDelay;
        lastPatternTime = 0;
        beatsSinceLastSpecial = 0;
        isInCooldown = false;
    }

    void Update()
    {
        if (GameFlowController_st2.Instance.CurrentState != GameStatest2.Playing) return;

        double currentDsp = AudioSettings.dspTime;

        if (CanSelectNewPattern(currentDsp))
        {
            SelectAndExecutePattern(currentDsp);
        }
    }

    bool CanSelectNewPattern(double dspTime)
    {
        // 1순위: 락 체크
        if (dspTime < patternLockUntilDspTime) return false;

        // 2순위: 최소 간격
        if (dspTime < lastPatternTime + minPatternInterval) return false;

        // 3순위: 그리드 체크
        if (dspTime < nextPatternTime) return false;

        // 곡 종료 전 5초 제한
        AudioSource bgm = GameFlowController_st2.Instance.bgmSource;
        if (bgm.clip.length - bgm.time < 5.0f) return false;

        return true;
    }

    void SelectAndExecutePattern(double currentDsp)
    {
        PatternType_st2 selectedPattern = SelectPattern();
        ExecutePattern(selectedPattern, currentDsp);

        // 다음 패턴 시간 계산
        float beatDuration = 60f / BPM;
        nextPatternTime = currentDsp + patternIntervalBeats * beatDuration;
        lastPatternTime = currentDsp;

        // 락/그리드 충돌 시 스냅 보정
        if (nextPatternTime < patternLockUntilDspTime + minPatternInterval)
        {
            nextPatternTime = patternLockUntilDspTime + minPatternInterval;
        }

        OnPatternQueued?.Invoke(selectedPattern);
    }

    PatternType_st2 SelectPattern()
    {
        // 1. 쿨다운 체크
        if (isInCooldown)
        {
            beatsSinceLastSpecial++;
            if (beatsSinceLastSpecial >= specialCooldownBeats)
            {
                isInCooldown = false;
                beatsSinceLastSpecial = 0;
            }
            else
            {
                return PatternType_st2.Normal;
            }
        }

        // 2. 보장 체크 (쿨다운보다 우선)
        if (beatsSinceLastSpecial >= maxBeatsWithoutSpecial)
        {
            beatsSinceLastSpecial = 0;
            isInCooldown = true;

            return UnityEngine.Random.value < 0.5f ? PatternType_st2.Sync2 : PatternType_st2.Run3;
        }

        // 3. 확률 판정
        float roll = UnityEngine.Random.Range(0f, 100f);

        if (roll < sync2Chance)
        {
            beatsSinceLastSpecial = 0;
            isInCooldown = true;
            return PatternType_st2.Sync2;
        }
        else if (roll < sync2Chance + run3Chance)
        {
            beatsSinceLastSpecial = 0;
            isInCooldown = true;
            return PatternType_st2.Run3;
        }
        else
        {
            beatsSinceLastSpecial++;
            return PatternType_st2.Normal;
        }
    }

    void ExecutePattern(PatternType_st2 type, double startTime)
    {
        switch (type)
        {
            case PatternType_st2.Normal:
                ExecuteNormalPattern(startTime);
                break;
            case PatternType_st2.Sync2:
                ExecuteSync2Pattern(startTime);
                break;
            case PatternType_st2.Run3:
                ExecuteRun3Pattern(startTime);
                break;
        }
    }

    void ExecuteNormalPattern(double t0)
    {
        int moldId = UnityEngine.Random.Range(0, 3);

        double telegraphTime = t0;
        double popTime = t0 + telegraphLeadTime;

        molds[moldId].ScheduleTelegraph(telegraphTime);
        molds[moldId].SchedulePop(popTime);

        patternLockUntilDspTime = popTime + 1.0 + 0.1;
    }

    void ExecuteSync2Pattern(double t0)
    {
        // 틀 선택: {0,1}, {1,2}, {0,2}
        int[][] pairs = { new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 0, 2 } };
        int[] selectedPair = pairs[UnityEngine.Random.Range(0, 3)];

        double cueTime = t0 - cueLeadTime;
        double telegraphTime = t0;
        double popTime = t0 + telegraphLeadTime;

        // 큐 사운드
        ScheduleCueSound(sync2Cue, cueTime);

        // 두 틀 동시 실행
        foreach (int moldId in selectedPair)
        {
            molds[moldId].ScheduleTelegraph(telegraphTime);
            molds[moldId].SchedulePop(popTime);
        }

        patternLockUntilDspTime = popTime + 1.0 + 0.1;
    }

    void ExecuteRun3Pattern(double t0)
    {
        double cueTime = t0 - cueLeadTime;

        // 큐 사운드
        ScheduleCueSound(run3Cue, cueTime);

        double lastPopTime = 0;

        // 3개 순차 실행
        for (int i = 0; i < 3; i++)
        {
            double telegraphTime = t0 + runStep * i;
            double popTime = t0 + telegraphLeadTime + runStep * i;

            molds[i].ScheduleTelegraph(telegraphTime);
            molds[i].SchedulePop(popTime);

            lastPopTime = popTime;
        }

        patternLockUntilDspTime = lastPopTime + 1.0 + 0.1;
    }

    void ScheduleCueSound(AudioClip clip, double dspTime)
    {
        cueAudioSource.clip = clip;
        cueAudioSource.PlayScheduled(dspTime);
        scheduledSources.Add(cueAudioSource);
    }

    public void ClearAllScheduledEvents()
    {
        // AudioSource 정리
        foreach (var source in scheduledSources)
        {
            if (source != null)
            {
                source.Stop();
            }
        }
        scheduledSources.Clear();

        // Coroutine 정리
        foreach (var routine in scheduledCoroutines)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        scheduledCoroutines.Clear();

        CancelInvoke();
    }
}

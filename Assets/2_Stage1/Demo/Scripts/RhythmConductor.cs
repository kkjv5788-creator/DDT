using System;
using System.Diagnostics;
using UnityEngine;

public class RhythmConductor : MonoBehaviour
{
    public enum RhythmState { Waiting, Guiding, Judging, Result }

    [Header("Data")]
    public RhythmTriggerListSO data;

    [Header("Audio")]
    public AudioSource bgmSource;

    [Header("Refs")]
    public KimbapSpawner spawner;
    public PlateController plate;

    [Header("Tutorial Assist")]
    [Range(1f, 2f)] public float judgeDurationMultiplier = 1f; // Assist에서 올림(튜토리얼만)

    [Header("Runtime")]
    public RhythmState state = RhythmState.Waiting;
    public int triggerIndex = 0;

    public int sliceCount = 0;
    int _required = 0;

    float _stateEnterTime;
    float _prevAudioTime;

    public event Action<int, RhythmTriggerListSO.Trigger> OnEnterTrigger;
    public event Action<int, bool, int, int> OnTriggerResolved; // index, success, sliceCount, required
    public event Action<RhythmState> OnStateChanged;

    // KnifeSlicer가 "지금 Judging 중이냐" 확인할 때 사용
    public bool IsJudging => state == RhythmState.Judging;
    public int CurrentRequired => _required;

    void Awake()
    {
        if (!bgmSource) bgmSource = GetComponent<AudioSource>();
    }

    public void SetData(RhythmTriggerListSO newData)
    {
        data = newData;
        triggerIndex = 0;
        sliceCount = 0;
        _required = 0;
        state = RhythmState.Waiting;
        _stateEnterTime = Time.time;
        _prevAudioTime = 0f;

        if (plate) plate.ResetPlateVisual();
    }

    public void PlayFromStart()
    {
        if (!data || !bgmSource)
        {
            UnityEngine.Debug.LogWarning("[RhythmConductor] Missing data or bgmSource.");
            return;
        }

        bgmSource.clip = data.bgm;
        bgmSource.loop = data.loopBgm;
        bgmSource.time = 0f;
        bgmSource.Play();

        _prevAudioTime = 0f;
        triggerIndex = 0;

        // ✅ 시작 전에 다음 김밥 준비(중복 방지 포함)
        if (spawner) spawner.EnsurePreparedKimbap();

        SetState(RhythmState.Waiting);
    }

    public void Stop()
    {
        if (bgmSource) bgmSource.Stop();
    }

    void Update()
    {
        if (!data || !bgmSource || !bgmSource.isPlaying) return;

        float t = bgmSource.time + (data.timingOffsetMs / 1000f);

        // 루프 감지(튜토리얼용)
        if (data.loopBgm && _prevAudioTime > bgmSource.time)
        {
            triggerIndex = 0;
            // 루프 돌 때도 상태머신은 계속 진행. 필요하면 Waiting으로 강제해도 됨.
        }
        _prevAudioTime = bgmSource.time;

        // 트리거 진입 체크(Waiting 상태에서만)
        if (state == RhythmState.Waiting)
        {
            if (triggerIndex < data.triggers.Length && t >= data.triggers[triggerIndex].triggerTime)
            {
                EnterTrigger(triggerIndex);
            }
        }

        TickStateMachine();
    }

    void EnterTrigger(int idx)
    {
        var trig = data.triggers[idx];

        sliceCount = 0;
        _required = trig.requiredSliceCount;

        // ✅ Guiding 시작 시 Activate 규칙 유지
        // 다만 prepared가 없을 가능성 방지로 Ensure 한번 더
        if (spawner)
        {
            spawner.EnsurePreparedKimbap();
            spawner.ActivatePreparedKimbap();
        }

        if (plate) plate.ResetPlateVisual();

        OnEnterTrigger?.Invoke(idx, trig);
        SetState(RhythmState.Guiding);
    }

    void TickStateMachine()
    {
        if (triggerIndex >= data.triggers.Length) return;
        var trig = data.triggers[triggerIndex];

        switch (state)
        {
            case RhythmState.Guiding:
                // 가이드 사운드
                if (trig.guideBeatSound && Mathf.Abs(Time.time - _stateEnterTime) < 0.05f)
                {
                    // guideBeatSound는 별도 AudioSource를 쓰는 게 더 좋지만, 간단히 OneShot
                    bgmSource.PlayOneShot(trig.guideBeatSound);
                }

                if (Time.time - _stateEnterTime >= trig.guideDuration)
                {
                    SetState(RhythmState.Judging);

                    // 현재 Kimbap을 Sliceable로
                    var kc = spawner ? spawner.GetCurrentKimbapController() : null;
                    if (kc) kc.SetSliceable(true);
                }
                break;

            case RhythmState.Judging:
                // 제한시간(Assist 적용)
                float judgeDur = trig.judgeDuration * Mathf.Max(1f, judgeDurationMultiplier);

                if (Time.time - _stateEnterTime >= judgeDur)
                {
                    // Judging 종료
                    var kc = spawner ? spawner.GetCurrentKimbapController() : null;
                    if (kc) kc.SetSliceable(false);

                    // 성공 판정
                    bool success = (sliceCount == trig.requiredSliceCount);
                    // 초과도 실패 유지: success 조건을 == 로 둠

                    // 결과 VFX/SFX
                    if (success)
                    {
                        if (trig.successSfx) bgmSource.PlayOneShot(trig.successSfx);
                        if (trig.successVfxPrefab) Instantiate(trig.successVfxPrefab, transform.position, Quaternion.identity);
                    }
                    else
                    {
                        if (trig.failSfx) bgmSource.PlayOneShot(trig.failSfx);
                        if (trig.failVfxPrefab) Instantiate(trig.failVfxPrefab, transform.position, Quaternion.identity);
                    }

                    // 접시 결과 연출
                    if (plate) plate.ResolvePlate(success);

                    OnTriggerResolved?.Invoke(triggerIndex, success, sliceCount, trig.requiredSliceCount);

                    SetState(RhythmState.Result);
                }
                break;

            case RhythmState.Result:
                // Result 연출 후 cleanup → Waiting
                // 짧은 결과 시간(필요하면 trig에 별도 resultDuration 필드 추가)
                if (Time.time - _stateEnterTime >= 0.6f)
                {
                    // Result → cleanup → Waiting 진입 시 Prepare (확정 규칙)
                    if (spawner) spawner.PrepareNextKimbap();

                    triggerIndex++;
                    SetState(RhythmState.Waiting);
                }
                break;
        }
    }

    void SetState(RhythmState s)
    {
        state = s;
        _stateEnterTime = Time.time;
        OnStateChanged?.Invoke(s);
    }

    /// <summary>
    /// KnifeSlicer가 ValidSlice를 판정했을 때 호출
    /// </summary>
    public void RegisterValidSlice(RhythmTriggerListSO.Trigger trig)
    {
        sliceCount++;

        // 접시 조각 누적
        if (plate) plate.AddPiece();

        // 즉시 피드백(컷 SFX/VFX)
        if (trig.hitSfx) bgmSource.PlayOneShot(trig.hitSfx);
        if (trig.cutVfxPrefab) Instantiate(trig.cutVfxPrefab, transform.position, Quaternion.identity);
    }

    public void RegisterWrongCut(RhythmTriggerListSO.Trigger trig, Vector3 worldPos)
    {
        if (trig.wrongCutSfx) bgmSource.PlayOneShot(trig.wrongCutSfx);
        if (trig.wrongCutVfxPrefab) Instantiate(trig.wrongCutVfxPrefab, worldPos, Quaternion.identity);
    }

    public RhythmTriggerListSO.Trigger GetCurrentTriggerOrNull()
    {
        if (!data || data.triggers == null) return null;
        if (triggerIndex < 0 || triggerIndex >= data.triggers.Length) return null;
        return data.triggers[triggerIndex];
    }
    public bool IsJudgingWindow()
    {
        return IsJudging;
    }               
    public RhythmState State => state;                        // DebugHUD: conductor.State
public int CurrentTriggerIndex => triggerIndex;           // DebugHUD: conductor.CurrentTriggerIndex
public int SliceCount => sliceCount;                      // DebugHUD: conductor.SliceCount
public int RequiredSliceCount => _required;               // DebugHUD: conductor.RequiredSliceCount

public float BgmTime
{
    get
    {
        if (!bgmSource || !bgmSource.isPlaying) return 0f;
        // HUD 표시용: 타이밍 오프셋 반영한 현재 시간
        return bgmSource.time + (data ? data.timingOffsetMs / 1000f : 0f);
    }
}
}

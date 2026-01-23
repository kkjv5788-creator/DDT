using System;
using System.Collections.Generic;
using UnityEngine;
using static GameState_st2;

public class JudgeSystem_st2 : MonoBehaviour
{
    [Header("판정 윈도우")]
    public float perfectHalfWindow = 0.04f;
    public float goodHalfWindow = 0.075f;

    [Tooltip("팝(popTime) 이후 이 시간(초)이 지나면 자동 Miss 확정")]
    public float fishCatchableDuration = 1.0f;

    [Header("타임아웃 처리 옵션")]
    [Tooltip("true면 타임아웃 Miss 확정과 동시에 소비(풀 반환)도 요청함. (Stage2EventHub + FishConsumer가 있을 때만 의미 있음)")]
    public bool consumeOnTimeout = false;

    [SerializeField] private Stage2EventHub_st2 hub; // optional

    [Header("통계")]
    public int perfectCount = 0;
    public int goodCount = 0;
    public int missCount = 0;

    // 이벤트
    public event Action<JudgeResult_st2, float> OnJudgeResult;

    private readonly HashSet<FishCatchToken_st2> activeFish = new HashSet<FishCatchToken_st2>();
    private readonly List<FishCatchToken_st2> _timeoutBuffer = new List<FishCatchToken_st2>(128);

    private void Awake()
    {
        if (consumeOnTimeout && hub == null)
            hub = FindObjectOfType<Stage2EventHub_st2>(true);
    }

    private void Update()
    {
        // 자동 Miss 확정(타임아웃)
        if (activeFish.Count == 0) return;

        double now = AudioSettings.dspTime;
        _timeoutBuffer.Clear();

        // HashSet은 순회 중 제거 불가 → 버퍼에 모아서 처리
        foreach (var fish in activeFish)
        {
            if (fish == null) { _timeoutBuffer.Add(fish); continue; }
            if (fish.isResolved) { _timeoutBuffer.Add(fish); continue; }

            // popTime 기준으로 catchableDuration 초 지나면 Miss 확정
            if (now - fish.popTime >= fishCatchableDuration)
            {
                _timeoutBuffer.Add(fish);
            }
        }

        for (int i = 0; i < _timeoutBuffer.Count; i++)
        {
            var fish = _timeoutBuffer[i];
            if (fish == null)
            {
                // null은 그냥 active에서 제거
                activeFish.Remove(fish);
                continue;
            }

            // 이미 다른 루트(잡기/바닥 등)에서 해결됐으면 제거만
            if (fish.isResolved)
            {
                activeFish.Remove(fish);
                continue;
            }

            // ✅ 타임아웃 Miss 확정
            ProcessTimeoutMiss(fish);

            // ✅ 원하면 여기서 바로 소비(풀 반환)까지 요청
            if (consumeOnTimeout && hub != null)
            {
                hub.PublishFishConsumeRequested(fish, FishConsumeReason_st2.Timeout);
            }
        }
    }

    public void RegisterFish(FishCatchToken_st2 fish)
    {
        if (fish == null) return;
        activeFish.Add(fish);
    }

    public void UnregisterFish(FishCatchToken_st2 fish)
    {
        if (fish == null) return;
        activeFish.Remove(fish);
    }

    public JudgeResult_st2 PerformJudge(FishCatchToken_st2 fish, double catchTime)
    {
        if (fish == null) return JudgeResult_st2.Miss;
        if (fish.isResolved) return JudgeResult_st2.Miss;

        float delta = (float)(catchTime - fish.popTime);
        float absDelta = Mathf.Abs(delta);

        JudgeResult_st2 result;

        if (absDelta <= perfectHalfWindow)
        {
            result = JudgeResult_st2.Perfect;
            perfectCount++;
        }
        else if (absDelta <= goodHalfWindow)
        {
            result = JudgeResult_st2.Good;
            goodCount++;
        }
        else
        {
            result = JudgeResult_st2.Miss;
            missCount++;
        }

        fish.isResolved = true;
        fish.isCaught = (result != JudgeResult_st2.Miss);

        activeFish.Remove(fish);
        OnJudgeResult?.Invoke(result, delta);

        return result;
    }

    private void ProcessTimeoutMiss(FishCatchToken_st2 fish)
    {
        if (fish == null) return;
        if (fish.isResolved) return;

        fish.isResolved = true;
        fish.isCaught = false;
        missCount++;

        activeFish.Remove(fish);

        // 타임아웃 미스는 delta 의미 없으니 0
        OnJudgeResult?.Invoke(JudgeResult_st2.Miss, 0f);
    }

    public void ResetStats()
    {
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
        activeFish.Clear();
        _timeoutBuffer.Clear();
    }

    public void ForceResolveAll()
    {
        _timeoutBuffer.Clear();
        foreach (var fish in activeFish)
        {
            if (fish != null && !fish.isResolved)
                _timeoutBuffer.Add(fish);
        }

        for (int i = 0; i < _timeoutBuffer.Count; i++)
        {
            var fish = _timeoutBuffer[i];
            if (fish != null && !fish.isResolved)
                ProcessTimeoutMiss(fish);
        }

        activeFish.Clear();
        _timeoutBuffer.Clear();
    }
}

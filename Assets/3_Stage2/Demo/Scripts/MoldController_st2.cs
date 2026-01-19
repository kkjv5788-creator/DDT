using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class MoldController_st2 : MonoBehaviour
{
    [Header("참조")]
    public GameObject fishPrefab;
    public AudioSource telegraphSource;
    public AudioSource popSource;
    public AudioClip telegraphClip;
    public AudioClip popClip;

    [Header("시각 효과")]
    public Animator moldAnimator;

    private ObjectPool<FishCatchToken_st2> fishPool;
    private JudgeSystem_st2 judgeSystem;

    // ✅ 추가: 이벤트
    public event Action OnTelegraphPlayed;
    public event Action OnPopPlayed;

    // ✅ 추가: 활성 fish 추적
    private List<FishCatchToken_st2> activeFish = new List<FishCatchToken_st2>();

    // ✅ 추가: 예약된 코루틴 추적
    private List<Coroutine> scheduledSpawns = new List<Coroutine>();

    void Awake()
    {
        // ✅ 수정: fish 풀 생성 (리셋 로직 추가)
        fishPool = new ObjectPool<FishCatchToken_st2>(
            createFunc: () => {
                var obj = Instantiate(fishPrefab, transform);
                return obj.GetComponent<FishCatchToken_st2>();
            },
            actionOnGet: (fish) => {
                // ✅ Get 시 활성화
                fish.gameObject.SetActive(true);
            },
            actionOnRelease: (fish) => {
                // ✅ Release 시 완전 리셋
                fish.transform.SetParent(transform); // Mold로 복귀
                fish.transform.localPosition = Vector3.zero;
                fish.transform.localRotation = Quaternion.identity;
                fish.transform.localScale = Vector3.one;
                fish.gameObject.SetActive(false);

                // ✅ 활성 리스트에서 제거
                activeFish.Remove(fish);
            },
            actionOnDestroy: (fish) => Destroy(fish.gameObject),
            defaultCapacity: 10
        );
    }

    public void Initialize(JudgeSystem_st2 judge)
    {
        judgeSystem = judge;
    }

    // ✅ 추가: Update에서 활성 fish 이동 처리
    void Update()
    {
        // ✅ 모든 활성 fish의 움직임 업데이트
        for (int i = 0; i < activeFish.Count; i++)
        {
            if (activeFish[i] != null && !activeFish[i].isResolved)
            {
                activeFish[i].UpdateMovement();
            }
        }
    }

    public void ScheduleTelegraph(double dspTime)
    {
        telegraphSource.clip = telegraphClip;
        telegraphSource.PlayScheduled(dspTime);

        // 시각 효과는 코루틴으로 처리
        double delay = dspTime - AudioSettings.dspTime;
        StartCoroutine(PlayTelegraphVisual((float)delay));
    }

    public void SchedulePop(double dspTime)
    {
        popSource.clip = popClip;
        popSource.PlayScheduled(dspTime);

        // fish 생성 예약
        double delay = dspTime - AudioSettings.dspTime;
        var spawnRoutine = StartCoroutine(SpawnFishDelayed((float)delay, dspTime));

        // ✅ 추가: 코루틴 추적
        scheduledSpawns.Add(spawnRoutine);
    }

    IEnumerator PlayTelegraphVisual(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (moldAnimator != null)
        {
            moldAnimator.SetTrigger("Telegraph");
        }

        // ✅ 이벤트 발행
        OnTelegraphPlayed?.Invoke();
    }

    IEnumerator SpawnFishDelayed(float delay, double popTime)
    {
        // ✅ 수정: unscaledTime 사용 (PAUSE 영향 최소화)
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // ✅ 추가: Ending/Result 체크
        if (GameFlowController_st2.Instance.isEndingTriggered ||
            GameFlowController_st2.Instance.CurrentState != GameState_st2.GameStatest2.Playing)
        {
            yield break; // 스폰 취소
        }

        // ✅ 추가: DSP 시간 재확인 (PAUSE로 인한 지연 감지)
        double actualDsp = AudioSettings.dspTime;
        if (actualDsp > popTime + 1.0f) // fishCatchableDuration
        {
            // 이미 timeout 지나서 스폰 취소
            yield break;
        }

        var fish = fishPool.Get();
        fish.Initialize(popTime, transform, this); // ✅ ownerMold 전달

        // ✅ 추가: 활성 리스트에 등록
        activeFish.Add(fish);

        if (judgeSystem != null)
        {
            judgeSystem.RegisterFish(fish);
        }

        if (moldAnimator != null)
        {
            moldAnimator.SetTrigger("Pop");
        }

        // ✅ 이벤트 발행
        OnPopPlayed?.Invoke();

        // ✅ 추가: 코루틴 리스트에서 제거
        scheduledSpawns.Remove(scheduledSpawns.Find(c => c == null));
    }

    public void ReleaseFish(FishCatchToken_st2 fish)
    {
        if (fish != null)
        {
            fishPool.Release(fish);
        }
    }

    // ✅ 추가: 예약된 스폰 취소
    public void CancelAllScheduledSpawns()
    {
        foreach (var routine in scheduledSpawns)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        scheduledSpawns.Clear();

        // ✅ 모든 활성 fish 정리
        foreach (var fish in activeFish.ToList())
        {
            if (fish != null)
            {
                fishPool.Release(fish);
            }
        }
        activeFish.Clear();
    }
}
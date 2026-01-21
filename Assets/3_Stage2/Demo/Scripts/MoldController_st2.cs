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
    public Animator moldAnimator;  // ✅ Animator 컴포넌트

    [Header("퐁 애니메이션 (뚜껑 열림)")]
    public string popAnimationTrigger = "Pop";  // ✅ Pop 애니메이션 트리거 이름
    public float popAnimationLeadTime = 0.2f;   // ✅ 퐁 소리보다 애니메이션을 먼저 시작할 시간

    [Header("달그락 애니메이션 (코드 흔들림)")]
    public bool useTelegraphShake = true;     // 달그락 흔들림 사용 여부
    public float shakeIntensity = 0.02f;      // 흔들림 강도
    public float shakeDuration = 0.3f;        // 흔들림 시간
    public int shakeCount = 8;                // 흔들림 횟수

    private ObjectPool<FishCatchToken_st2> fishPool;
    private JudgeSystem_st2 judgeSystem;

    // 이벤트
    public event Action OnTelegraphPlayed;
    public event Action OnPopPlayed;

    // 활성 fish 추적
    private List<FishCatchToken_st2> activeFish = new List<FishCatchToken_st2>();

    // 예약된 코루틴 추적
    private List<Coroutine> scheduledSpawns = new List<Coroutine>();

    // 틀 원본 위치 (흔들림 복원용)
    private Vector3 originalMoldPosition;

    void Awake()
    {
        // 원본 위치 저장
        originalMoldPosition = transform.localPosition;

        // fish 풀 생성
        fishPool = new ObjectPool<FishCatchToken_st2>(
            createFunc: () => {
                var obj = Instantiate(fishPrefab, transform);
                return obj.GetComponent<FishCatchToken_st2>();
            },
            actionOnGet: (fish) => {
                fish.gameObject.SetActive(true);
            },
            actionOnRelease: (fish) => {
                fish.OnReturnToPool(); // ✅ Rigidbody 리셋
                fish.transform.SetParent(transform);
                fish.transform.localPosition = Vector3.zero;
                fish.transform.localRotation = Quaternion.identity;
                fish.transform.localScale = Vector3.one;
                fish.gameObject.SetActive(false);
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

    void Update()
    {
        // 모든 활성 fish의 움직임 업데이트
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

        double delay = dspTime - AudioSettings.dspTime;
        StartCoroutine(PlayTelegraphVisual((float)delay));
    }

    public void SchedulePop(double dspTime)
    {
        popSource.clip = popClip;
        popSource.PlayScheduled(dspTime);

        // ✅ 애니메이션은 퐁 소리보다 먼저 시작
        double animationDelay = dspTime - AudioSettings.dspTime - popAnimationLeadTime;
        if (animationDelay > 0)
        {
            StartCoroutine(PlayPopAnimation((float)animationDelay));
        }

        // ✅ fish 생성은 퐁 소리 타이밍에 맞춤
        double fishDelay = dspTime - AudioSettings.dspTime;
        var spawnRoutine = StartCoroutine(SpawnFishDelayed((float)fishDelay, dspTime));
        scheduledSpawns.Add(spawnRoutine);
    }

    IEnumerator PlayTelegraphVisual(float delay)
    {
        yield return new WaitForSeconds(delay);

        // ✅ 달그락 애니메이션 (코드로 틀 흔들기)
        if (useTelegraphShake)
        {
            StartCoroutine(ShakeMold());
        }

        // 이벤트 발행
        OnTelegraphPlayed?.Invoke();
    }

    // ✅ 달그락 애니메이션 - 틀 흔들기 (Telegraph용 코드 효과)
    IEnumerator ShakeMold()
    {
        float elapsed = 0f;
        int currentShake = 0;
        float shakeInterval = shakeDuration / shakeCount;

        while (currentShake < shakeCount)
        {
            // 랜덤 방향으로 흔들기
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                0,
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity)
            );

            transform.localPosition = originalMoldPosition + randomOffset;

            yield return new WaitForSeconds(shakeInterval);
            currentShake++;
        }

        // 원위치로 복원
        transform.localPosition = originalMoldPosition;
    }

    // ✅ 퐁 애니메이션만 먼저 재생
    IEnumerator PlayPopAnimation(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (moldAnimator != null)
        {
            moldAnimator.SetTrigger(popAnimationTrigger);
            Debug.Log($"Pop 애니메이션 트리거 발동 (퐁 소리보다 {popAnimationLeadTime}초 먼저)");
        }
    }

    IEnumerator SpawnFishDelayed(float delay, double popTime)
    {
        // unscaledTime 사용 (PAUSE 영향 최소화)
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Ending/Result 체크
        if (GameFlowController_st2.Instance.isEndingTriggered ||
            GameFlowController_st2.Instance.CurrentState != GameState_st2.GameStatest2.Playing)
        {
            yield break;
        }

        // DSP 시간 재확인 (PAUSE로 인한 지연 감지)
        double actualDsp = AudioSettings.dspTime;
        if (actualDsp > popTime + 1.0f)
        {
            yield break;
        }

        // ✅ fish 생성 (퐁 소리 타이밍에 맞춤)
        var fish = fishPool.Get();
        fish.Initialize(popTime, transform, this);

        // 활성 리스트에 등록
        activeFish.Add(fish);

        if (judgeSystem != null)
        {
            judgeSystem.RegisterFish(fish);
        }

        // 이벤트 발행
        OnPopPlayed?.Invoke();

        // 코루틴 리스트에서 제거
        scheduledSpawns.Remove(scheduledSpawns.Find(c => c == null));
    }

    public void ReleaseFish(FishCatchToken_st2 fish)
    {
        if (fish != null)
        {
            fishPool.Release(fish);
        }
    }

    // 예약된 스폰 취소
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

        // 모든 활성 fish 정리
        foreach (var fish in activeFish.ToList())
        {
            if (fish != null)
            {
                fishPool.Release(fish);
            }
        }
        activeFish.Clear();
    }
    public int GetActiveFishCount()
    {
        return activeFish.Count;
    }
}
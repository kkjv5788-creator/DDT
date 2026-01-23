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

    [Header("퐁 애니메이션 (뚜껑 열림)")]
    public string popAnimationTrigger = "Pop";
    public float popAnimationLeadTime = 0.2f;

    [Header("달그락 애니메이션 (코드 흔들림)")]
    public bool useTelegraphShake = true;
    public float shakeIntensity = 0.02f;
    public float shakeDuration = 0.3f;
    public int shakeCount = 8;

    [Header("VFX (Pop / 퐁)")]
    public GameObject popVfxPrefab;                 /*[변경가능_퐁이펙트프리팹]*/
    public Transform popVfxAnchor;                  /*[변경가능_퐁이펙트기준점] (없으면 mold transform)*/
    public Vector3 popVfxLocalOffset = Vector3.zero;/*[변경가능_퐁이펙트오프셋]*/
    public Vector3 popVfxLocalEuler = Vector3.zero; /*[변경가능_퐁이펙트회전]*/
    public float popVfxAutoDestroySeconds = 2.0f;   /*[변경가능_퐁이펙트자동삭제]*/

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
        originalMoldPosition = transform.localPosition;

        fishPool = new ObjectPool<FishCatchToken_st2>(
            createFunc: () => {
                var obj = Instantiate(fishPrefab, transform);
                var fish = obj.GetComponent<FishCatchToken_st2>();
                if (fish == null) fish = obj.AddComponent<FishCatchToken_st2>();
                return fish;
            },
            actionOnGet: (fish) => {
                fish.gameObject.SetActive(true);
            },
            actionOnRelease: (fish) => {
                fish.OnReturnToPool();
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

    // ✅ GameFlowController가 호출함(복구)
    public void Initialize(JudgeSystem_st2 judge)
    {
        judgeSystem = judge;
    }

    void Update()
    {
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
        if (telegraphSource != null && telegraphClip != null)
        {
            telegraphSource.clip = telegraphClip;
            telegraphSource.PlayScheduled(dspTime);
        }

        double delay = dspTime - AudioSettings.dspTime;
        StartCoroutine(PlayTelegraphVisual((float)delay));
    }

    public void SchedulePop(double dspTime)
    {
        if (popSource != null && popClip != null)
        {
            popSource.clip = popClip;
            popSource.PlayScheduled(dspTime);
        }

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

        if (useTelegraphShake)
        {
            StartCoroutine(ShakeMold());
        }

        OnTelegraphPlayed?.Invoke();
    }

    IEnumerator ShakeMold()
    {
        int currentShake = 0;
        float shakeInterval = shakeDuration / Mathf.Max(1, shakeCount);

        while (currentShake < shakeCount)
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                0,
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity)
            );

            transform.localPosition = originalMoldPosition + randomOffset;

            yield return new WaitForSeconds(shakeInterval);
            currentShake++;
        }

        transform.localPosition = originalMoldPosition;
    }

    IEnumerator PlayPopAnimation(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (moldAnimator != null)
            moldAnimator.SetTrigger(popAnimationTrigger);
    }

    IEnumerator SpawnFishDelayed(float delay, double popTime)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Ending/Result 체크 (기존 유지)
        if (GameFlowController_st2.Instance != null)
        {
            if (GameFlowController_st2.Instance.isEndingTriggered ||
                GameFlowController_st2.Instance.CurrentState != GameState_st2.GameStatest2.Playing)
            {
                yield break;
            }
        }

        // DSP 시간 재확인 (PAUSE로 인한 지연 감지)
        double actualDsp = AudioSettings.dspTime;
        if (actualDsp > popTime + 1.0f)
            yield break;

        // ✅ fish 생성
        var fish = fishPool.Get();
        fish.Initialize(popTime, transform, this);

        // ✅ 퐁 이펙트 생성 (퐁 소리/스폰 타이밍)
        SpawnPopVfx();

        activeFish.Add(fish);

        if (judgeSystem != null)
        {
            judgeSystem.RegisterFish(fish);
        }

        OnPopPlayed?.Invoke();

        // 안전 정리
        scheduledSpawns.RemoveAll(c => c == null);
    }

    void SpawnPopVfx()
    {
        if (popVfxPrefab == null) return;

        Transform a = popVfxAnchor != null ? popVfxAnchor : transform;
        Vector3 pos = a.TransformPoint(popVfxLocalOffset);
        Quaternion rot = a.rotation * Quaternion.Euler(popVfxLocalEuler);

        var vfx = Instantiate(popVfxPrefab, pos, rot);
        AutoDestroyVfx(vfx, popVfxAutoDestroySeconds);
    }

    static void AutoDestroyVfx(GameObject vfxRoot, float fallbackSeconds)
    {
        if (vfxRoot == null) return;

        var ps = vfxRoot.GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            var main = ps.main;
            float lifeMax = 0f;

            var sl = main.startLifetime;
            if (sl.mode == ParticleSystemCurveMode.Constant) lifeMax = sl.constant;
            else if (sl.mode == ParticleSystemCurveMode.TwoConstants) lifeMax = sl.constantMax;
            else lifeMax = sl.constantMax;

            float total = Mathf.Max(0.1f, main.duration + lifeMax);
            UnityEngine.Object.Destroy(vfxRoot, total);
            return;
        }

        UnityEngine.Object.Destroy(vfxRoot, Mathf.Max(0.1f, fallbackSeconds));
    }

    public void ReleaseFish(FishCatchToken_st2 fish)
    {
        if (fish != null)
            fishPool.Release(fish);
    }

    // ✅ GameFlowController가 호출함(복구)
    public void CancelAllScheduledSpawns()
    {
        foreach (var routine in scheduledSpawns)
        {
            if (routine != null) StopCoroutine(routine);
        }
        scheduledSpawns.Clear();

        foreach (var fish in activeFish.ToList())
        {
            if (fish != null) fishPool.Release(fish);
        }
        activeFish.Clear();
    }

    public int GetActiveFishCount() => activeFish.Count;
}

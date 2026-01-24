// Assets/3_Stage2/Scripts/Fish&mold/MoldController_st2.cs
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

    [Header("애니메이션")]
    public Animator moldAnimator;
    public string popAnimationTrigger = "Pop";
    public float popAnimationLeadTime = 0.2f;

    // ✅ 달그락 애니메이션 (코드 흔들림) - 튜토리얼과 동일 컨셉
    [Header("달그락 애니메이션 (코드 흔들림)")]
    public bool useTelegraphShake = true;
    public float shakeIntensity = 0.02f;
    public float shakeDuration = 0.3f;
    public int shakeCount = 8;

    [Header("점프(수치)")]
    public float jumpHeight = 2.5f;
    public float forwardDistance = 2.0f;
    public float gravity = 9.81f;
    public LayerMask groundLayer;

    [Header("Pop VFX")]
    public GameObject popVfxPrefab;
    public Transform popVfxAnchor;
    public Vector3 popVfxLocalOffset;
    public Vector3 popVfxLocalEuler;
    public float popVfxAutoDestroySeconds = 2.0f;

    [Header("디버그")]
    public TextMeshProUGUI debugText;

    [Header("외부 시스템")]
    public JudgeSystem_st2 judgeSystem;

    private ObjectPool<FishCatchToken_st2> fishPool;
    private readonly List<FishCatchToken_st2> activeFish = new List<FishCatchToken_st2>();
    private Vector3 originalMoldPosition;

    // ✅ 텔레그래프 흔들림 코루틴 핸들
    private Coroutine shakeRoutine;

    // 스케줄 스폰(패턴 디렉터에서 예약)
    private readonly List<Coroutine> scheduledSpawns = new List<Coroutine>();

    void Awake()
    {
        originalMoldPosition = transform.localPosition;

        fishPool = new ObjectPool<FishCatchToken_st2>(
            CreateFish,
            OnGetFish,
            OnReleaseFish,
            OnDestroyFish,
            collectionCheck: false,
            defaultCapacity: 12,
            maxSize: 64
        );
    }

    public void Initialize(JudgeSystem_st2 judge)
    {
        judgeSystem = judge;
    }

    private FishCatchToken_st2 CreateFish()
    {
        var go = Instantiate(fishPrefab);
        go.SetActive(false);

        var fish = go.GetComponent<FishCatchToken_st2>();
        if (fish == null) fish = go.AddComponent<FishCatchToken_st2>();

        fish.SetPool(fishPool);
        return fish;
    }

    private void OnGetFish(FishCatchToken_st2 fish)
    {
        if (fish == null) return;
        fish.gameObject.SetActive(true);
    }

    private void OnReleaseFish(FishCatchToken_st2 fish)
    {
        if (fish == null) return;
        fish.gameObject.SetActive(false);
        fish.transform.SetParent(null);
    }

    private void OnDestroyFish(FishCatchToken_st2 fish)
    {
        if (fish == null) return;
        Destroy(fish.gameObject);
    }

    public void ScheduleTelegraph(double dspTime)
    {
        if (telegraphSource != null && telegraphClip != null)
        {
            telegraphSource.clip = telegraphClip;
            telegraphSource.PlayScheduled(dspTime);
        }

        double delay = dspTime - AudioSettings.dspTime;
        StartCoroutine(PlayTelegraphVisualDelayed((float)delay));
    }

    public void SchedulePop(double dspTime)
    {
        if (popSource != null && popClip != null)
        {
            popSource.clip = popClip;
            popSource.PlayScheduled(dspTime);
        }

        // 애니메이션은 퐁 소리보다 먼저
        double animationDelay = dspTime - AudioSettings.dspTime - popAnimationLeadTime;
        if (animationDelay > 0)
        {
            StartCoroutine(PlayPopAnimation((float)animationDelay));
        }

        // fish 생성은 퐁 소리 타이밍에 맞춤
        double fishDelay = dspTime - AudioSettings.dspTime;
        var spawnRoutine = StartCoroutine(SpawnFishDelayed((float)fishDelay, dspTime));
        scheduledSpawns.Add(spawnRoutine);
    }

    // =========================================================
    // ✅ Telegraph Visual (달그락 흔들림)
    // =========================================================
    IEnumerator PlayTelegraphVisualDelayed(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        PlayTelegraphVisual(); // 딜레이 후 흔들림 실행
    }

    // ✅ 외부에서 즉시 달그락 연출이 필요하면 이 메서드 호출 가능
    public void PlayTelegraphVisual()
    {
        if (!useTelegraphShake) return;

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeMold());
    }

    IEnumerator ShakeMold()
    {
        int currentShake = 0;
        float shakeInterval = shakeDuration / Mathf.Max(1, shakeCount);

        while (currentShake < shakeCount)
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                0f,
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity)
            );

            transform.localPosition = originalMoldPosition + randomOffset;

            float elapsed = 0f;
            while (elapsed < shakeInterval)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            currentShake++;
        }

        transform.localPosition = originalMoldPosition;
        shakeRoutine = null;
    }

    // =========================================================
    // Pop Animation
    // =========================================================
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

        // Ending/Result 체크
        if (GameFlowController_st2.Instance != null)
        {
            if (GameFlowController_st2.Instance.isEndingTriggered ||
                GameFlowController_st2.Instance.CurrentState != GameState_st2.GameStatest2.Playing)
            {
                yield break;
            }
        }

        // PAUSE로 인한 지연 감지
        double actualDsp = AudioSettings.dspTime;
        if (actualDsp > popTime + 1.0f)
            yield break;

        var fish = fishPool.Get();
        fish.Initialize(popTime, transform, this, jumpHeight, forwardDistance, gravity, groundLayer);

        SpawnPopVfx();

        activeFish.Add(fish);

        if (judgeSystem != null)
            judgeSystem.RegisterFish(fish);

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

    // ✅ GameFlowController가 호출(복구)
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

    void OnDisable()
    {
        // 흔들림 중이면 원위치 복구
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = null;
        transform.localPosition = originalMoldPosition;
    }
}

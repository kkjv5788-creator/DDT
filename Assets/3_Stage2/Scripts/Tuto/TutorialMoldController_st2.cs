// ===============================
// TutorialMoldController_st2.cs
// (달그락 비주얼 + Pop 애니 트리거 분리 / SpawnFish에서는 애니 제거)
// ===============================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class TutorialMoldController_st2 : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject tutorialFishPrefab;

    [Header("애니메이션")]
    public Animator moldAnimator;
    public string popAnimationTrigger = "Pop";
    public float popAnimationLeadTime = 0.25f; // ✅ 퐁 소리보다 먼저 열리는 리드타임(기본 0.25)

    [Header("달그락 애니메이션 (코드 흔들림)")]
    public bool useTelegraphShake = true;
    public float shakeIntensity = 0.02f;
    public float shakeDuration = 0.3f;
    public int shakeCount = 8;

    [Header("점프 설정")]
    public float jumpHeight = 2.5f;
    public float forwardDistance = 2.0f;
    public float gravity = 9.81f;
    public LayerMask groundLayer;

    [Header("VFX (Pop / 퐁)")]
    public GameObject popVfxPrefab;
    public Transform popVfxAnchor;
    public Vector3 popVfxLocalOffset = Vector3.zero;
    public Vector3 popVfxLocalEuler = Vector3.zero;
    public float popVfxAutoDestroySeconds = 2.0f;

    private ObjectPool<TutorialFishToken_st2> fishPool;
    private readonly List<TutorialFishToken_st2> activeFish = new List<TutorialFishToken_st2>();

    public event Action OnTelegraphVisualPlayed;

    private Vector3 originalMoldPosition;
    private Coroutine shakeRoutine;

    void Awake()
    {
        originalMoldPosition = transform.localPosition;
        InitializePool();
    }

    void InitializePool()
    {
        fishPool = new ObjectPool<TutorialFishToken_st2>(
            createFunc: () =>
            {
                var obj = Instantiate(tutorialFishPrefab, transform);
                var fish = obj.GetComponent<TutorialFishToken_st2>();
                if (fish == null) fish = obj.AddComponent<TutorialFishToken_st2>();
                return fish;
            },
            actionOnGet: (fish) =>
            {
                fish.gameObject.SetActive(true);
            },
            actionOnRelease: (fish) =>
            {
                fish.OnReturnToPool();
                fish.transform.SetParent(transform);
                fish.transform.localPosition = Vector3.zero;
                fish.transform.localRotation = Quaternion.identity;
                fish.transform.localScale = Vector3.one;
                fish.gameObject.SetActive(false);
                activeFish.Remove(fish);
            },
            actionOnDestroy: (fish) =>
            {
                if (fish != null && fish.gameObject != null)
                    Destroy(fish.gameObject);
            },
            defaultCapacity: 5
        );
    }

    // ✅ 컨트롤러가 사운드 재생한 직후 호출 (달그락 비주얼)
    public void PlayTelegraphVisual()
    {
        if (!useTelegraphShake) return;

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeMold());

        OnTelegraphVisualPlayed?.Invoke();
    }

    // ✅ Pop 애니메이션 트리거를 SpawnFish와 분리 (소리보다 먼저 열기 위해)
    public void TriggerPopAnimation()
    {
        if (moldAnimator != null)
            moldAnimator.SetTrigger(popAnimationTrigger);
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

    // ✅ SpawnFish에서는 Pop 애니메이션을 트리거하지 않음 (컨트롤러가 리드타임 제어)
    public void SpawnFish(double popTime)
    {
        // ✅ 퐁 이펙트
        SpawnPopVfx();

        // Fish 생성
        var fish = fishPool.Get();
        fish.Initialize(popTime, transform, this, jumpHeight, forwardDistance, gravity, groundLayer);
        activeFish.Add(fish);
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

    public void ReleaseFish(TutorialFishToken_st2 fish)
    {
        if (fish != null)
            fishPool.Release(fish);
    }

    void Update()
    {
        for (int i = activeFish.Count - 1; i >= 0; i--)
        {
            if (activeFish[i] != null && !activeFish[i].isResolved)
                activeFish[i].UpdateMovement();
        }
    }

    void OnDisable()
    {
        CleanupAllFish();
        transform.localPosition = originalMoldPosition;

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = null;
    }

    public void CleanupAllFish()
    {
        foreach (var fish in activeFish.ToArray())
        {
            if (fish != null)
                fishPool.Release(fish);
        }
        activeFish.Clear();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 튜토리얼 전용 몰드 - 메인 MoldController와 독립
/// </summary>
public class TutorialMoldController_st2 : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject tutorialFishPrefab;

    [Header("애니메이션")]
    public Animator moldAnimator;
    public string popAnimationTrigger = "Pop";
    public float popAnimationLeadTime = 0.2f;

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
    private List<TutorialFishToken_st2> activeFish = new List<TutorialFishToken_st2>();

    void Awake()
    {
        InitializePool();
    }

    void InitializePool()
    {
        fishPool = new ObjectPool<TutorialFishToken_st2>(
            createFunc: () => {
                var obj = Instantiate(tutorialFishPrefab, transform);
                var fish = obj.GetComponent<TutorialFishToken_st2>();
                if (fish == null) fish = obj.AddComponent<TutorialFishToken_st2>();
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
            actionOnDestroy: (fish) => {
                if (fish != null && fish.gameObject != null)
                    Destroy(fish.gameObject);
            },
            defaultCapacity: 5
        );
    }

    public void SpawnFish(double popTime)
    {
        // Pop 애니메이션
        if (moldAnimator != null)
            moldAnimator.SetTrigger(popAnimationTrigger);

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
    }

    // ✅ StandaloneTutorialController가 호출함(복구)
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

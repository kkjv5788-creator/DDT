using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using static GameState_st2;

/// <summary>
/// JudgeResolved(성공) 시: fish.OnCaught() 호출 → 손 스냅에 0.2초 붙임 → Success consume 요청
/// + (옵션) FishCatchToken에 catchSfx/Vfx 세팅이 없으면 여기 fallback으로 재생/생성
/// </summary>
public class CatchVfx_st2 : MonoBehaviour
{
    [Header("Hub")]
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Hand Snap (붙는 위치)")]
    public Transform leftHandSnap;
    public Transform rightHandSnap;
    public bool autoFindHandSnapsIfNull = true;

    [Header("Timing")]
    [Min(0f)] public float snapHoldSeconds = 0.2f;
    [Min(0f)] public float fadeSeconds = 0f; // 기본 0: "0.2초 붙었다가 바로 풀 반환" 느낌 유지

    [Header("Hold Behavior")]
    public bool parentToHand = true;
    public bool setKinematicWhileHeld = true;
    public bool disableCollidersWhileHeld = true;

    [Header("Fallback Catch SFX/VFX (Fish token에 세팅이 없을 때만)")]
    public AudioSource catchSfxSource;
    public AudioClip catchSnapClip;

    public GameObject catchVfxPrefab;
    public Vector3 catchVfxLocalOffset = Vector3.zero;
    public Vector3 catchVfxLocalEuler = Vector3.zero;
    [Min(0f)] public float catchVfxAutoDestroySeconds = 2f;

    private readonly HashSet<int> processing = new HashSet<int>();

    private static readonly BindingFlags PrivateInst = BindingFlags.Instance | BindingFlags.NonPublic;
    private static FieldInfo _fiCatchSnapClip;
    private static FieldInfo _fiCatchVfxPrefab;

    public void Construct(Stage2EventHub_st2 eventHub)
    {
        hub = eventHub;
    }

    private void OnEnable()
    {
        if (hub != null) hub.JudgeResolved += OnJudgeResolved;
    }

    private void OnDisable()
    {
        if (hub != null) hub.JudgeResolved -= OnJudgeResolved;
        processing.Clear();
    }

    private void OnJudgeResolved(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        if (fish == null) return;
        if (result != JudgeResult_st2.Good && result != JudgeResult_st2.Perfect) return;

        int id = fish.GetInstanceID();
        if (processing.Contains(id)) return;
        processing.Add(id);

        StartCoroutine(CoSuccess(fish, hand));
    }

    private IEnumerator CoSuccess(FishCatchToken_st2 fish, OVRInput.Controller hand)
    {
        if (fish == null)
        {
            yield break;
        }

        // 혹시 스냅이 비어있으면(런타임 AddComponent 등) 자동 탐색
        if (autoFindHandSnapsIfNull && (leftHandSnap == null || rightHandSnap == null))
        {
            AutoFindHandSnaps();
        }

        // 1) 잡힘 처리(여기서 fish 쪽 catch SFX/VFX도 시도)
        //    ※ FishCatchToken_st2.OnCaught()는 rb velocity를 먼저 0으로 만들고 kinematic으로 바꾸는 구조라 안전.
        fish.OnCaught();

        // 2) fish 쪽에 catch 세팅이 없으면 fallback으로 착 SFX/VFX
        bool fishHasOwnFx = FishHasOwnCatchFxConfigured(fish);
        if (!fishHasOwnFx)
        {
            PlayFallbackCatchSfx();
            SpawnFallbackCatchVfx(fish.transform);
        }

        // 3) 손에 붙이기(0.2초 유지)
        Transform snap = ResolveHandSnap(hand);
        Transform tr = fish.transform;

        // 상태 백업(콜라이더는 반드시 다시 켜줘야 다음 스폰 때 레이캐스트가 됨)
        Collider[] cols = null;
        bool[] colEnabled = null;

        if (disableCollidersWhileHeld)
        {
            cols = fish.GetComponentsInChildren<Collider>(true);
            colEnabled = new bool[cols.Length];
            for (int i = 0; i < cols.Length; i++)
            {
                colEnabled[i] = cols[i].enabled;
                cols[i].enabled = false;
            }
        }

        Rigidbody rb = fish.GetComponent<Rigidbody>();
        bool hadRb = rb != null;
        bool prevKinematic = false;
        bool prevUseGravity = false;

        if (hadRb && setKinematicWhileHeld)
        {
            prevKinematic = rb.isKinematic;
            prevUseGravity = rb.useGravity;

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Transform prevParent = tr.parent;

        if (parentToHand && snap != null)
        {
            tr.SetParent(snap, worldPositionStays: false);
            tr.localPosition = Vector3.zero;
            tr.localRotation = Quaternion.identity;
        }
        else
        {
            // 스냅이 없으면 최소한 위치만 손 근처로 한번 맞춤(없으면 그냥 현재 유지)
            if (snap != null)
            {
                tr.position = snap.position;
                tr.rotation = snap.rotation;
            }
        }

        if (snapHoldSeconds > 0f)
            yield return new WaitForSeconds(snapHoldSeconds);

        // (선택) 페이드 - 기본 0
        if (fadeSeconds > 0f && fish != null && fish.gameObject.activeInHierarchy)
        {
            yield return FadeOutRenderers(fish.gameObject, fadeSeconds);
        }

        // 콜라이더는 다시 켜두고 반환(안 켜면 다음 스폰 때 레이캐스트 안 맞음)
        if (disableCollidersWhileHeld && cols != null)
        {
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null) cols[i].enabled = colEnabled[i];
            }
        }

        if (hadRb && setKinematicWhileHeld)
        {
            // 풀로 돌아가면서 다시 초기화되긴 하지만, 혹시 consumer가 없을 때 대비해서 원복
            rb.isKinematic = prevKinematic;
            rb.useGravity = prevUseGravity;
        }

        // 부모도 원복(풀에서 parent 잡아주긴 하지만 깔끔하게 정리)
        if (tr != null)
            tr.SetParent(prevParent, worldPositionStays: true);

        // 4) 성공 consume 요청 → FishConsumer가 풀 반환, BagPolicy가 봉투 비주얼 추가
        if (hub != null)
            hub.PublishFishConsumeRequested(fish, FishConsumeReason_st2.Success);

        // 안전장치: consumer 누락돼도 남지 않게 1프레임 후에도 살아있으면 직접 반환
        yield return null;
        if (fish != null && fish.gameObject.activeInHierarchy)
        {
            fish.ConsumeToPool(FishConsumeReason_st2.Success);
        }

        processing.Remove(fish != null ? fish.GetInstanceID() : 0);
    }

    private Transform ResolveHandSnap(OVRInput.Controller hand)
    {
        // 대부분 프로젝트에서 여기 2개는 무조건 존재
        if ((hand & OVRInput.Controller.LTouch) != 0) return leftHandSnap;
        if ((hand & OVRInput.Controller.RTouch) != 0) return rightHandSnap;

        // hand tracking 같은 케이스(버전에 따라 enum이 다름)를 컴파일 안전하게 처리
        string hs = hand.ToString();
        if (hs.Contains("LHand") || hs.Contains("HandLeft")) return leftHandSnap;
        if (hs.Contains("RHand") || hs.Contains("HandRight")) return rightHandSnap;

        // fallback
        if (rightHandSnap != null) return rightHandSnap;
        return leftHandSnap;
    }

    private void AutoFindHandSnaps()
    {
        // 1) HandCatchSensor_st2 기반으로 찾기(가장 정확)
        var sensors = FindObjectsOfType<HandCatchSensor_st2>(true);
        foreach (var s in sensors)
        {
            if (s == null) continue;
            if (leftHandSnap == null && (s.handController & OVRInput.Controller.LTouch) != 0)
                leftHandSnap = s.transform;

            if (rightHandSnap == null && (s.handController & OVRInput.Controller.RTouch) != 0)
                rightHandSnap = s.transform;
        }

        // 2) 이름으로 찾기(보조)
        if (leftHandSnap == null) leftHandSnap = FindTransformByNameHints(true);
        if (rightHandSnap == null) rightHandSnap = FindTransformByNameHints(false);
    }

    private Transform FindTransformByNameHints(bool left)
    {
        string[] hints = left
            ? new[] { "LeftHandAnchor", "LeftControllerAnchor", "LeftHand", "L_Hand", "LeftHandSnap" }
            : new[] { "RightHandAnchor", "RightControllerAnchor", "RightHand", "R_Hand", "RightHandSnap" };

        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t == null) continue;
            for (int i = 0; i < hints.Length; i++)
            {
                if (t.name == hints[i])
                    return t;
            }
        }

        return null;
    }

    private bool FishHasOwnCatchFxConfigured(FishCatchToken_st2 fish)
    {
        if (fish == null) return false;

        if (_fiCatchSnapClip == null)
            _fiCatchSnapClip = typeof(FishCatchToken_st2).GetField("catchSnapClip", PrivateInst);
        if (_fiCatchVfxPrefab == null)
            _fiCatchVfxPrefab = typeof(FishCatchToken_st2).GetField("catchVfxPrefab", PrivateInst);

        var clip = _fiCatchSnapClip != null ? _fiCatchSnapClip.GetValue(fish) as AudioClip : null;
        var vfx = _fiCatchVfxPrefab != null ? _fiCatchVfxPrefab.GetValue(fish) as GameObject : null;

        return clip != null || vfx != null;
    }

    private void PlayFallbackCatchSfx()
    {
        if (catchSfxSource != null && catchSnapClip != null)
            catchSfxSource.PlayOneShot(catchSnapClip);
    }

    private void SpawnFallbackCatchVfx(Transform anchor)
    {
        if (catchVfxPrefab == null || anchor == null) return;

        GameObject vfx = Instantiate(catchVfxPrefab, anchor);
        vfx.transform.localPosition = catchVfxLocalOffset;
        vfx.transform.localRotation = Quaternion.Euler(catchVfxLocalEuler);

        if (catchVfxAutoDestroySeconds > 0f)
            Destroy(vfx, catchVfxAutoDestroySeconds);
    }

    private IEnumerator FadeOutRenderers(GameObject root, float seconds)
    {
        if (root == null || seconds <= 0f) yield break;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) yield break;

        // 원본 색 저장
        var mats = new List<Material>();
        var colors = new List<Color>();

        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var m in r.materials)
            {
                if (m != null && m.HasProperty("_Color"))
                {
                    mats.Add(m);
                    colors.Add(m.color);
                }
            }
        }

        float t = 0f;
        while (t < seconds)
        {
            float a = 1f - (t / seconds);
            for (int i = 0; i < mats.Count; i++)
            {
                var c = colors[i];
                c.a *= a;
                mats[i].color = c;
            }

            t += Time.deltaTime;
            yield return null;
        }
    }
}

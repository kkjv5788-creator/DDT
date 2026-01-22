using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static GameState_st2;

/// <summary>
/// JudgeResolved(성공) 시: fish.OnCaught() 호출 → 손 스냅에 0.2초 붙임 → Success consume 요청
/// (FX는 FishCatchToken_st2.OnCaught() 내부에서만 처리)
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
    [Min(0f)] public float fadeSeconds = 0f;

    [Header("Hold Behavior")]
    public bool parentToHand = true;
    public bool setKinematicWhileHeld = true;
    public bool disableCollidersWhileHeld = true;

    private readonly HashSet<int> processing = new HashSet<int>();

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
        if (fish == null) yield break;

        if (autoFindHandSnapsIfNull && (leftHandSnap == null || rightHandSnap == null))
            AutoFindHandSnaps();

        // 1) 잡힘 처리 (SFX/VFX는 fish.OnCaught() 안에서만)
        fish.OnCaught();

        // 2) 손에 붙이기
        Transform snap = ResolveHandSnap(hand);
        Transform tr = fish.transform;

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
        else if (snap != null)
        {
            tr.position = snap.position;
            tr.rotation = snap.rotation;
        }

        if (snapHoldSeconds > 0f)
            yield return new WaitForSeconds(snapHoldSeconds);

        if (fadeSeconds > 0f && fish != null && fish.gameObject.activeInHierarchy)
            yield return FadeOutRenderers(fish.gameObject, fadeSeconds);

        // 콜라이더 원복
        if (disableCollidersWhileHeld && cols != null)
        {
            for (int i = 0; i < cols.Length; i++)
                if (cols[i] != null) cols[i].enabled = colEnabled[i];
        }

        if (hadRb && setKinematicWhileHeld)
        {
            rb.isKinematic = prevKinematic;
            rb.useGravity = prevUseGravity;
        }

        if (tr != null)
            tr.SetParent(prevParent, worldPositionStays: true);

        // 3) 성공 consume 요청
        if (hub != null)
            hub.PublishFishConsumeRequested(fish, FishConsumeReason_st2.Success);

        // 안전장치
        yield return null;
        if (fish != null && fish.gameObject.activeInHierarchy)
            fish.ConsumeToPool(FishConsumeReason_st2.Success);

        processing.Remove(fish != null ? fish.GetInstanceID() : 0);
    }

    private Transform ResolveHandSnap(OVRInput.Controller hand)
    {
        if ((hand & OVRInput.Controller.LTouch) != 0) return leftHandSnap;
        if ((hand & OVRInput.Controller.RTouch) != 0) return rightHandSnap;

        string hs = hand.ToString();
        if (hs.Contains("LHand") || hs.Contains("HandLeft")) return leftHandSnap;
        if (hs.Contains("RHand") || hs.Contains("HandRight")) return rightHandSnap;

        if (rightHandSnap != null) return rightHandSnap;
        return leftHandSnap;
    }

    private void AutoFindHandSnaps()
    {
        var sensors = FindObjectsOfType<HandCatchSensor_st2>(true);
        foreach (var s in sensors)
        {
            if (s == null) continue;

            if (leftHandSnap == null && (s.handController & OVRInput.Controller.LTouch) != 0)
                leftHandSnap = s.transform;

            if (rightHandSnap == null && (s.handController & OVRInput.Controller.RTouch) != 0)
                rightHandSnap = s.transform;
        }

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
                if (t.name == hints[i]) return t;
        }
        return null;
    }

    private IEnumerator FadeOutRenderers(GameObject root, float seconds)
    {
        if (root == null || seconds <= 0f) yield break;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) yield break;

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

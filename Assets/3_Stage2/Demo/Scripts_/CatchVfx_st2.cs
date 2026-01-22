using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using static GameState_st2;

public class CatchVfx_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Timing")]
    public float snapHoldSeconds = 0.2f;
    public float fadeSeconds = 0.25f;

    [Header("Snap (optional)")]
    public Transform leftHandSnap;
    public Transform rightHandSnap;

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
    }

    private void OnJudgeResolved(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        if (fish == null) return;

        bool success = (result == JudgeResult_st2.Perfect || result == JudgeResult_st2.Good);
        if (!success) return;

        StartCoroutine(CoSuccess(fish, hand));
    }

    private IEnumerator CoSuccess(FishCatchToken_st2 fish, OVRInput.Controller hand)
    {
        // 1) fish.OnCaught()가 있으면 호출 (기존 기능 유지용)
        SafeInvokeNoArg(fish, "OnCaught");

        // 2) 1프레임 스냅(선택)
        Transform handT = (hand == OVRInput.Controller.LTouch) ? leftHandSnap : rightHandSnap;
        if (handT != null)
        {
            var tr = fish.transform;
            tr.position = handT.position;
            tr.rotation = handT.rotation;
        }
        yield return null;

        // 3) 홀드
        yield return new WaitForSeconds(snapHoldSeconds);

        // 4) 페이드(재질 컬러 프로퍼티가 있을 때만)
        yield return FadeOutRenderers(fish.gameObject, fadeSeconds);

        // 5) 소비 요청 발행 (BagPolicy는 여기(Success) 타이밍에 맞춰 담기)
        if (hub != null) hub.PublishFishConsumeRequested(fish, FishConsumeReason_st2.Success);
    }

    private static void SafeInvokeNoArg(object target, string methodName)
    {
        try
        {
            var mi = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            mi?.Invoke(target, null);
        }
        catch { /* ignore */ }
    }

    private static IEnumerator FadeOutRenderers(GameObject root, float duration)
    {
        if (root == null) yield break;
        if (duration <= 0f) yield break;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) yield break;

        var mpb = new MaterialPropertyBlock();

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(1f - (t / duration));

            foreach (var r in renderers)
            {
                if (r == null) continue;

                // _BaseColor 또는 _Color 지원
                r.GetPropertyBlock(mpb);

                if (r.sharedMaterial != null)
                {
                    if (r.sharedMaterial.HasProperty("_BaseColor"))
                    {
                        Color c = r.sharedMaterial.GetColor("_BaseColor");
                        c.a *= a;
                        mpb.SetColor("_BaseColor", c);
                        r.SetPropertyBlock(mpb);
                    }
                    else if (r.sharedMaterial.HasProperty("_Color"))
                    {
                        Color c = r.sharedMaterial.GetColor("_Color");
                        c.a *= a;
                        mpb.SetColor("_Color", c);
                        r.SetPropertyBlock(mpb);
                    }
                }
            }
            yield return null;
        }
    }
}

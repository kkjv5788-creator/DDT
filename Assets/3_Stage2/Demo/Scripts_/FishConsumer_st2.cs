using System;
using System.Reflection;
using UnityEngine;

public class FishConsumer_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;

    public void Construct(Stage2EventHub_st2 eventHub)
    {
        hub = eventHub;
    }

    private void Awake()
    {
        if (!hub) hub = FindObjectOfType<Stage2EventHub_st2>(true);
    }

    private void OnEnable()
    {
        if (hub != null) hub.FishConsumeRequested += OnConsume;
    }

    private void OnDisable()
    {
        if (hub != null) hub.FishConsumeRequested -= OnConsume;
    }

    private void OnConsume(FishCatchToken_st2 fish, FishConsumeReason_st2 reason)
    {
        if (fish == null) return;

        // 1) 프로젝트에 이미 있으면 우선 사용
        if (TryInvoke(fish, "ConsumeToPool", reason)) return;
        if (TryInvoke(fish, "ReturnToPool")) return;
        if (TryInvoke(fish, "Release")) return;

        // 2) Stage2 구조상 제일 안전한 풀 반환 루트
        if (fish.ownerMold != null)
        {
            fish.ownerMold.ReleaseFish(fish);
            return;
        }

        // 3) fallback
        fish.gameObject.SetActive(false);
    }

    private static bool TryInvoke(object target, string methodName, object arg = null)
    {
        try
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var t = target.GetType();
            var mi = t.GetMethod(methodName, flags);
            if (mi == null) return false;

            var ps = mi.GetParameters();
            if (ps.Length == 0)
            {
                mi.Invoke(target, null);
                return true;
            }

            if (ps.Length == 1)
            {
                object param = arg;

                if (arg != null && !ps[0].ParameterType.IsInstanceOfType(arg))
                {
                    if (ps[0].ParameterType.IsEnum)
                        param = Enum.Parse(ps[0].ParameterType, arg.ToString(), true);
                    else
                        return false;
                }

                mi.Invoke(target, new object[] { param });
                return true;
            }
        }
        catch { /* ignore */ }

        return false;
    }
}

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

        // ✅ 프로젝트에 이미 풀 반환 함수가 있으면 그걸 우선 호출
        if (TryInvoke(fish, "ConsumeToPool", reason)) return;
        if (TryInvoke(fish, "ReturnToPool")) return;
        if (TryInvoke(fish, "Release")) return;

        // fallback
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
                // reason 타입이 다를 수도 있어서 변환 시도
                object param = arg;
                if (arg != null && !ps[0].ParameterType.IsInstanceOfType(arg))
                {
                    // enum이면 name 매칭 시도
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

using System;
using System.Reflection;
using UnityEngine;
using static GameState_st2;

public class EconomyListener_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;
    [SerializeField] private EconomySystem_st2 economy;

    public void Construct(Stage2EventHub_st2 eventHub, EconomySystem_st2 eco)
    {
        hub = eventHub;
        economy = eco;
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
        if (economy == null) return;

        // ApplyJudgeResult(result) 호출 (reflection)
        try
        {
            var mi = economy.GetType().GetMethod("ApplyJudgeResult",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (mi != null)
            {
                if (mi.GetParameters().Length == 1) mi.Invoke(economy, new object[] { result });
                else mi.Invoke(economy, null);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[EconomyListener_st2] ApplyJudgeResult invoke failed: {e.Message}");
        }
    }
}

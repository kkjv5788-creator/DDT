using System;
using System.Reflection;
using UnityEngine;
using static GameState_st2;

/// <summary>
/// CatchAttempted ¡æ JudgeSystem.PerformJudge ¡æ JudgeResolved ¹ßÇà
/// </summary>
[DisallowMultipleComponent]
public class JudgeBridge_st2 : MonoBehaviour
{
    [Header("Event Hub")]
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Judge System")]
    [SerializeField] private JudgeSystem_st2 judge;

    private MethodInfo _performJudgeMi;
    private bool _bound;

    public void Construct(Stage2EventHub_st2 eventHub, JudgeSystem_st2 judgeSystem)
    {
        hub = eventHub;
        judge = judgeSystem;

        CacheReflection();
        TryAutoWire();
        Bind();
    }

    private void Awake()
    {
        CacheReflection();
        TryAutoWire();
    }

    private void OnEnable()
    {
        TryAutoWire();
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void TryAutoWire()
    {
        if (hub == null)
            hub = GetComponent<Stage2EventHub_st2>();

        if (judge == null)
            judge = FindObjectOfType<JudgeSystem_st2>();
    }

    private void CacheReflection()
    {
        if (judge == null) return;

        _performJudgeMi = judge.GetType().GetMethod(
            "PerformJudge",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new Type[] { typeof(FishCatchToken_st2), typeof(double) },
            null);
    }

    private void Bind()
    {
        if (_bound) return;
        if (hub == null) return;

        hub.CatchAttempted += OnCatchAttempted;
        _bound = true;
    }

    private void Unbind()
    {
        if (!_bound) return;

        if (hub != null)
            hub.CatchAttempted -= OnCatchAttempted;

        _bound = false;
    }

    private void OnCatchAttempted(FishCatchToken_st2 fish, OVRInput.Controller hand, double catchDspTime)
    {
        if (judge == null || fish == null || _performJudgeMi == null)
        {
            if (judge == null) TryAutoWire();
            if (judge != null && _performJudgeMi == null) CacheReflection();
            if (judge == null || _performJudgeMi == null) return;
        }

        JudgeResult_st2 result;

        try
        {
            object ret = _performJudgeMi.Invoke(judge, new object[] { fish, catchDspTime });
            result = (JudgeResult_st2)ret;
        }
        catch (Exception e)
        {
            Debug.LogError($"[JudgeBridge_st2] PerformJudge invoke failed: {e.Message}");
            return;
        }

        float delta = (float)(catchDspTime - fish.popDspTime);

        if (hub != null)
            hub.PublishJudgeResolved(fish, result, hand, delta);
    }
}

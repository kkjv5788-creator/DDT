using System;
using System.Reflection;
using UnityEngine;
using static GameState_st2;

/// <summary>
/// JudgeResolved → EconomySystem.ApplyJudgeResult
/// </summary>
[DisallowMultipleComponent]
public class EconomyListener_st2 : MonoBehaviour
{
    [Header("Event Hub")]
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Economy System")]
    [SerializeField] private EconomySystem_st2 economy;

    private MethodInfo _applyMi;
    private bool _bound;

    public void Construct(Stage2EventHub_st2 eventHub, EconomySystem_st2 economySystem)
    {
        hub = eventHub;
        economy = economySystem;

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

        if (economy == null)
            economy = FindObjectOfType<EconomySystem_st2>();
    }

    private void CacheReflection()
    {
        if (economy == null) return;

        _applyMi = economy.GetType().GetMethod(
            "ApplyJudgeResult",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new Type[] { typeof(JudgeResult_st2) },
            null);
    }

    private void Bind()
    {
        if (_bound) return;
        if (hub == null) return;

        hub.JudgeResolved += OnJudgeResolved;
        _bound = true;
    }

    private void Unbind()
    {
        if (!_bound) return;

        if (hub != null)
            hub.JudgeResolved -= OnJudgeResolved;

        _bound = false;
    }

    private void OnJudgeResolved(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        if (economy == null || _applyMi == null)
        {
            if (economy == null) TryAutoWire();
            if (economy != null && _applyMi == null) CacheReflection();
            if (economy == null || _applyMi == null) return;
        }

        try
        {
            _applyMi.Invoke(economy, new object[] { result });
        }
        catch (Exception e)
        {
            Debug.LogError($"[EconomyListener_st2] ApplyJudgeResult invoke failed: {e.Message}");
        }
    }
}

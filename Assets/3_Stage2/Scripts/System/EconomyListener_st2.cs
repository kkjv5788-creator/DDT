using UnityEngine;
using static GameState_st2;

[DisallowMultipleComponent]
public class EconomyListener_st2 : MonoBehaviour
{
    [Header("Optional Hub (legacy wiring)")]
    [SerializeField] private Stage2EventHub_st2 hub; // 호환용. 실제로는 안 써도 됨.

    [Header("Judge System")]
    [SerializeField] private JudgeSystem_st2 judge;

    [Header("Economy System")]
    [SerializeField] private EconomySystem_st2 economy;

    private bool _bound;

    // ✅ 신규: Judge 직접 주입
    public void Construct(JudgeSystem_st2 judgeSystem, EconomySystem_st2 economySystem)
    {
        judge = judgeSystem;
        economy = economySystem;
        Bind();
    }

    // ✅ 레거시 호환: MainInstaller가 (hub, economy)로 호출하던 형태 유지
    public void Construct(Stage2EventHub_st2 eventHub, EconomySystem_st2 economySystem)
    {
        hub = eventHub;
        economy = economySystem;

        // judge는 여기서 자동으로 찾거나, 필요하면 hub를 통해 찾는 로직을 추가할 수도 있음
        if (judge == null) judge = FindObjectOfType<JudgeSystem_st2>(true);

        Bind();
    }

    // ✅ 레거시+명시 주입: (hub, judge, economy)도 지원
    public void Construct(Stage2EventHub_st2 eventHub, JudgeSystem_st2 judgeSystem, EconomySystem_st2 economySystem)
    {
        hub = eventHub;
        judge = judgeSystem;
        economy = economySystem;
        Bind();
    }

    private void Awake()
    {
        if (judge == null) judge = FindObjectOfType<JudgeSystem_st2>(true);
        if (economy == null) economy = FindObjectOfType<EconomySystem_st2>(true);
    }

    private void OnEnable() => Bind();
    private void OnDisable() => Unbind();

    private void Bind()
    {
        if (_bound) return;
        if (judge == null || economy == null) return;

        judge.OnJudgeResult += OnJudgeResult;
        _bound = true;
    }

    private void Unbind()
    {
        if (!_bound) return;
        if (judge != null) judge.OnJudgeResult -= OnJudgeResult;
        _bound = false;
    }

    private void OnJudgeResult(JudgeResult_st2 result, float delta)
    {
        economy.ApplyJudgeResult(result);
        // Debug.Log($"[EconomyListener] result={result} wage={economy.currentWage}");
    }
}

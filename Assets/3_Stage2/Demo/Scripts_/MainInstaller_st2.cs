using UnityEngine;

public class MainInstaller_st2 : MonoBehaviour
{
    [Header("Core Refs")]
    [SerializeField] private CatchInput_st2[] catchInputs;
    [SerializeField] private JudgeSystem_st2 judgeSystem;
    [SerializeField] private EconomySystem_st2 economySystem;
    [SerializeField] private BagManager_st2 bagManager;

    [Header("Optional")]
    [SerializeField] private GroundMissReporter_st2 groundMissReporter;

    private void Awake()
    {
        // 1) 허브는 MainRoot 아래에서만 생성/참조
        var hub = GetComponent<Stage2EventHub_st2>();
        if (hub == null) hub = gameObject.AddComponent<Stage2EventHub_st2>();

        // 2) CatchInput은 허브만 주입
        foreach (var input in catchInputs)
            if (input != null) input.Initialize(hub);

        // 3) 브리지/리스너 구성
        var judgeBridge = gameObject.AddComponent<JudgeBridge_st2>();
        judgeBridge.Construct(hub, judgeSystem);

        var ecoListener = gameObject.AddComponent<EconomyListener_st2>();
        ecoListener.Construct(hub, economySystem);

        var vfx = gameObject.AddComponent<CatchVfx_st2>();
        vfx.Construct(hub);

        var bagPolicy = gameObject.AddComponent<BagPolicy_st2>();
        bagPolicy.Construct(hub, bagManager);

        var consumer = gameObject.AddComponent<FishConsumer_st2>();
        consumer.Construct(hub);

        if (groundMissReporter != null)
            groundMissReporter.Construct(hub, judgeSystem);
    }
}

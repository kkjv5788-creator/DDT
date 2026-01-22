using UnityEngine;

public class MainInstaller_st2 : MonoBehaviour
{
    [Header("Main Systems (MainRoot)")]
    public CatchInput_st2[] catchInputs;
    public JudgeSystem_st2 judgeSystem;
    public EconomySystem_st2 economySystem;
    public BagManager_st2 bagManager;

    [Header("Optional")]
    public GroundMissReporter_st2 groundMissReporter;

    private void Awake()
    {
        // ✅ MainRoot 아래에만 허브가 존재해야 함
        var hub = GetComponent<Stage2EventHub_st2>();
        if (hub == null) hub = gameObject.AddComponent<Stage2EventHub_st2>();

        // CatchInput은 허브만 받음
        if (catchInputs != null)
        {
            foreach (var input in catchInputs)
                if (input != null) input.Initialize(hub);
        }

        // 브리지/리스너/정책/연출/소비
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

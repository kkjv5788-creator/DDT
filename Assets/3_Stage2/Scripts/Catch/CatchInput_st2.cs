// Assets/3_Stage2/Scripts/Catch/CatchInput_st2.cs
using UnityEngine;
using static GameState_st2;

public class CatchInput_st2 : MonoBehaviour
{
    // ✅ DebugHUD/GameFlowController 호환용(표시만)
    public JudgeResult_st2 lastJudgeResult = JudgeResult_st2.Miss;
    public string lastHandName = "-";

    [Header("Event Hub")]
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Sensors (Required)")]
    [SerializeField] private HandCatchSensor_st2 leftSensor;
    [SerializeField] private HandCatchSensor_st2 rightSensor;

    [Header("Input")]
    [Range(0f, 1f)] public float triggerThreshold = 0.9f;
    public float perHandCooldown = 0.05f;

    private double _lastLeftAttemptDsp = -999;
    private double _lastRightAttemptDsp = -999;
    private bool _subscribed = false;

    public void Initialize(Stage2EventHub_st2 eventHub)
    {
        hub = eventHub;
        TryBindHub();
    }

    private void OnEnable()
    {
        TryBindHub();
    }

    private void OnDisable()
    {
        TryUnbindHub();
    }

    private void TryBindHub()
    {
        if (_subscribed) return;
        if (hub == null) return;

        hub.JudgeResolved += OnJudgeResolved_Debug;
        _subscribed = true;
    }

    private void TryUnbindHub()
    {
        if (!_subscribed) return;

        if (hub != null)
            hub.JudgeResolved -= OnJudgeResolved_Debug;

        _subscribed = false;
    }

    private void Update()
    {
        // Playing에서만 입력 받기 (원하면 다른 상태도 허용 가능)
        if (GameFlowController_st2.Instance != null &&
            GameFlowController_st2.Instance.CurrentState != GameStatest2.Playing)
            return;

        if (hub == null) return;
        if (leftSensor == null || rightSensor == null) return;

        // Left
        float lt = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        if (lt >= triggerThreshold)
            TryAttempt(OVRInput.Controller.LTouch, leftSensor);

        // Right
        float rt = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        if (rt >= triggerThreshold)
            TryAttempt(OVRInput.Controller.RTouch, rightSensor);
    }

    private void TryAttempt(OVRInput.Controller hand, HandCatchSensor_st2 sensor)
    {
        if (sensor == null) return;

        double now = AudioSettings.dspTime;
        if (hand == OVRInput.Controller.LTouch)
        {
            if (now - _lastLeftAttemptDsp < perHandCooldown) return;
            _lastLeftAttemptDsp = now;
            lastHandName = "Left";
        }
        else
        {
            if (now - _lastRightAttemptDsp < perHandCooldown) return;
            _lastRightAttemptDsp = now;
            lastHandName = "Right";
        }

        var fish = sensor.GetCurrentTarget();
        if (fish == null) return;
        if (fish.isResolved) return;

        // ✅ “잡기 시도”만 발행 → JudgeBridge가 판정 → JudgeResolved로 퍼짐
        hub.PublishCatchAttempted(fish, hand, now);
    }

    private void OnJudgeResolved_Debug(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        lastJudgeResult = result;
        lastHandName = (hand == OVRInput.Controller.LTouch) ? "Left" : "Right";
    }
}

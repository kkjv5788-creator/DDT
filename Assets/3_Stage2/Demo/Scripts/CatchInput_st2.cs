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
        if (hub == null) { _subscribed = false; return; }

        hub.JudgeResolved -= OnJudgeResolved_Debug;
        _subscribed = false;
    }

    // ✅ 표시용 값만 업데이트
    private void OnJudgeResolved_Debug(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        lastJudgeResult = result;
        lastHandName = HandToName(hand);
    }

    private void Update()
    {
        if (hub == null) return;

        ProcessHand(OVRInput.Controller.LTouch, leftSensor, ref _lastLeftAttemptDsp);
        ProcessHand(OVRInput.Controller.RTouch, rightSensor, ref _lastRightAttemptDsp);
    }

    private void ProcessHand(OVRInput.Controller hand, HandCatchSensor_st2 sensor, ref double lastAttemptDsp)
    {
        double dsp = AudioSettings.dspTime;
        if (dsp - lastAttemptDsp < perHandCooldown) return;

        float axis = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, hand);
        if (axis < triggerThreshold) return;

        // 표시용
        lastHandName = HandToName(hand);

        // ✅ 튜토와 동일: 센서 레이 타겟이 없으면 "아무것도 안 함"
        if (sensor == null) return;

        var target = sensor.GetCurrentTarget();
        if (target == null) return;
        if (target.isResolved) return;

        lastAttemptDsp = dsp;

        // ✅ v6.2 원칙 유지: 여기서는 이벤트만 발행(판정/봉투/임금 직접 호출 금지)
        hub.PublishCatchAttempted(target, hand, dsp);
    }

    private static string HandToName(OVRInput.Controller hand)
    {
        return hand == OVRInput.Controller.LTouch ? "Left" :
               hand == OVRInput.Controller.RTouch ? "Right" :
               hand.ToString();
    }
}

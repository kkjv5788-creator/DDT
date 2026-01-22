using System.Reflection;
using UnityEngine;
using static GameState_st2;

public class CatchInput_st2 : MonoBehaviour
{
    // ✅ DebugHUD/GameFlowController 호환용 (기존 스크립트가 읽는 필드)
    // - 판정 결과는 hub.JudgeResolved 구독으로 "값만" 업데이트됨 (로직 결합 X)
    public JudgeResult_st2 lastJudgeResult = JudgeResult_st2.Miss;
    public string lastHandName = "-";

    [Header("Event Hub")]
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Hand Roots (required if no sensor)")]
    public Transform leftHandRoot;
    public Transform rightHandRoot;

    [Header("Optional Sensors (if you already have HandCatchSensor_st2 or similar)")]
    public MonoBehaviour leftSensor;
    public MonoBehaviour rightSensor;

    [Header("Target Search Fallback")]
    public LayerMask fishLayer = ~0;
    public float catchRadius = 0.12f;

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

    // ✅ Debug 필드 업데이트 전용 (표시용)
    private void OnJudgeResolved_Debug(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        lastJudgeResult = result;
        lastHandName = HandToName(hand);
    }

    private void Update()
    {
        if (hub == null) return;

        ProcessHand(OVRInput.Controller.LTouch, leftHandRoot, leftSensor, ref _lastLeftAttemptDsp);
        ProcessHand(OVRInput.Controller.RTouch, rightHandRoot, rightSensor, ref _lastRightAttemptDsp);
    }

    private void ProcessHand(OVRInput.Controller hand, Transform handRoot, MonoBehaviour sensor, ref double lastAttemptDsp)
    {
        double dsp = AudioSettings.dspTime;
        if (dsp - lastAttemptDsp < perHandCooldown) return;

        float axis = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, hand);
        bool pressed = axis >= triggerThreshold;
        if (!pressed) return;

        // ✅ HUD가 "마지막 입력 손" 표시할 수 있게 시도 시점에도 갱신(표시용)
        lastHandName = HandToName(hand);

        var target = TryGetTargetFromSensor(sensor);
        if (target == null)
            target = FindClosestFishByOverlap(handRoot);

        if (target == null) return;

        lastAttemptDsp = dsp;

        // ✅ 여기서부터가 v6.2 핵심: 판정/봉투/임금 직접 호출 금지
        hub.PublishCatchAttempted(target, hand, dsp);
    }

    private FishCatchToken_st2 TryGetTargetFromSensor(MonoBehaviour sensor)
    {
        if (sensor == null) return null;

        try
        {
            var t = sensor.GetType();

            // 1) GetCurrentTarget()
            var mi = t.GetMethod("GetCurrentTarget",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (mi != null)
            {
                var r = mi.Invoke(sensor, null);
                if (r is FishCatchToken_st2 fish) return fish;
            }

            // 2) CurrentTarget property
            var pi = t.GetProperty("CurrentTarget",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (pi != null)
            {
                var r = pi.GetValue(sensor);
                if (r is FishCatchToken_st2 fish) return fish;
            }

            // 3) currentTarget field
            var fi = t.GetField("currentTarget",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (fi != null)
            {
                var r = fi.GetValue(sensor);
                if (r is FishCatchToken_st2 fish) return fish;
            }
        }
        catch { /* ignore */ }

        return null;
    }

    private FishCatchToken_st2 FindClosestFishByOverlap(Transform handRoot)
    {
        if (handRoot == null) return null;

        Collider[] hits = Physics.OverlapSphere(handRoot.position, catchRadius, fishLayer, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return null;

        FishCatchToken_st2 best = null;
        float bestD = float.MaxValue;

        foreach (var c in hits)
        {
            if (c == null) continue;
            var fish = c.GetComponentInParent<FishCatchToken_st2>();
            if (fish == null) continue;

            float d = (fish.transform.position - handRoot.position).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = fish;
            }
        }

        return best;
    }

    private static string HandToName(OVRInput.Controller hand)
    {
        return hand == OVRInput.Controller.LTouch ? "Left" :
               hand == OVRInput.Controller.RTouch ? "Right" :
               hand.ToString();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (leftHandRoot != null) Gizmos.DrawWireSphere(leftHandRoot.position, catchRadius);
        if (rightHandRoot != null) Gizmos.DrawWireSphere(rightHandRoot.position, catchRadius);
    }
#endif
}

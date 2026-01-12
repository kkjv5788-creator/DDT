using UnityEngine;

public class SmartHeightManager : MonoBehaviour
{
    [Header("Editor/Simulator Test Offset Only")]
    public bool enableEditorOffset = true;

    [Tooltip("Title/Start simulated eye height (meters)")]
    public float titleEyeHeight = 0.0f;

    [Tooltip("Gameplay simulated eye height (meters)")]
    public float gameEyeHeight = 1.7f;

    [Tooltip("Apply once on Start")]
    public bool applyOnStart = false;

    [Tooltip("Start mode: true = game height, false = title height")]
    public bool startAsGameHeight = false;

#if UNITY_EDITOR
    private Transform _trackingSpace;

    private void Awake()
    {
        _trackingSpace = FindTrackingSpace();
    }

    private void Start()
    {
        if (!enableEditorOffset) return;

        if (applyOnStart)
        {
            if (startAsGameHeight) SwitchToGameHeight();
            else SwitchToTitleHeight();
        }
    }

    // ===== 호환 API (AppSceneFlow가 호출하는 메서드) =====
    public void SwitchToTitleHeight()
    {
        ApplyHeight(titleEyeHeight);
    }

    public void SwitchToGameHeight()
    {
        ApplyHeight(gameEyeHeight);
    }

    // 필요할 때 수동 적용(에디터 우클릭 메뉴)
    [ContextMenu("Apply Title Height (Editor)")]
    private void ApplyTitleHeightMenu() => SwitchToTitleHeight();

    [ContextMenu("Apply Game Height (Editor)")]
    private void ApplyGameHeightMenu() => SwitchToGameHeight();

    private void ApplyHeight(float y)
    {
        if (!enableEditorOffset) return;

        if (_trackingSpace == null)
            _trackingSpace = FindTrackingSpace();

        if (_trackingSpace == null)
        {
            Debug.LogWarning("[SmartHeightManager] TrackingSpace not found.");
            return;
        }

        var p = _trackingSpace.localPosition;
        p.y = y;
        _trackingSpace.localPosition = p;
    }

    private Transform FindTrackingSpace()
    {
        // 씬 내 TrackingSpace를 1회 탐색(OVRCameraRig 아래)
        var all = FindObjectsOfType<Transform>(true);
        foreach (var t in all)
        {
            if (t.name == "TrackingSpace") return t;
        }
        return null;
    }
#else
    // 빌드에서는 완전 무효 (옵션 A)
    public void SwitchToTitleHeight() { }
    public void SwitchToGameHeight() { }
#endif
}

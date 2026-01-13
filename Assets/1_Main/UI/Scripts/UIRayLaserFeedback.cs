using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIRayLaserFeedback : MonoBehaviour
{
    [Header("Ray Origin (권장: right_laser_begin)")]
    public Transform rayOrigin;

    [Header("Laser Visual (둘 중 하나만)")]
    [Tooltip("권장: LaserLineDriver 사용(색/두께/스무딩 통일)")]
    public LaserLineDriver laserVisual;

    [Tooltip("LaserLineDriver 없을 때만 LineRenderer 직접 제어")]
    public LineRenderer line;

    [Header("Distance")]
    public float maxDistance = 8f;
    public float minDistance = 0.05f;

    [Header("Auto Find (옵션)")]
    public bool autoFindRayOriginByName = true;
    public string rayOriginName = "right_laser_begin";

    // --- internal ---
    private PointerEventData _ovrPed; // 반드시 OVRPointerEventData 인스턴스여야 함(아니면 null)
    private EventSystem _cachedEventSystem;

    private readonly List<RaycastResult> _results = new();

    // Reflection cache
    private static Type _ovrPedType;
    private static FieldInfo _worldSpaceRayField;

    private void Awake()
    {
        if (line == null) line = GetComponentInChildren<LineRenderer>(true);
        if (rayOrigin == null && autoFindRayOriginByName) rayOrigin = FindTransformByName(rayOriginName);

        CacheOVRPointerEventDataReflection();

        _cachedEventSystem = EventSystem.current;
        if (_cachedEventSystem != null)
            _ovrPed = CreateOVRPointerEventData(_cachedEventSystem); // 실패하면 null
    }

    private void OnEnable()
    {
        if (rayOrigin == null && autoFindRayOriginByName)
            rayOrigin = FindTransformByName(rayOriginName);

        _cachedEventSystem = EventSystem.current;
        if (_cachedEventSystem != null)
            _ovrPed = CreateOVRPointerEventData(_cachedEventSystem); // 실패하면 null
    }

    private void Update()
    {
        // 예외 금지: 필수 요소 없으면 그냥 idle로 그림
        if (rayOrigin == null || EventSystem.current == null)
        {
            ApplyVisual(maxDistance, false);
            return;
        }

        // EventSystem 바뀌면 재생성
        if (_cachedEventSystem != EventSystem.current)
        {
            _cachedEventSystem = EventSystem.current;
            _ovrPed = CreateOVRPointerEventData(_cachedEventSystem); // 실패하면 null
        }

        // OVRPointerEventData가 없으면 UI 히트 감지를 못함(예외 없이 그냥 idle)
        if (_ovrPed == null)
        {
            ApplyVisual(maxDistance, false);
            return;
        }

        float dist;
        bool hitUI = TryGetUIHitDistance(out dist);
        ApplyVisual(dist, hitUI);

        // 디버그 출력 추가
        if (hitUI)
        {
            Debug.Log($"[UIRayLaser] UI Hit! Distance: {dist}");
        }
    }

    private bool TryGetUIHitDistance(out float hitDistance)
    {
        hitDistance = maxDistance;

        // worldSpaceRay 주입이 가능한 상태가 아니면 감지 불가
        if (_ovrPedType == null || _worldSpaceRayField == null) return false;
        if (_ovrPed == null || !_ovrPedType.IsInstanceOfType(_ovrPed)) return false;

        // OVRPointerEventData.worldSpaceRay 세팅
        var wsRay = new Ray(rayOrigin.position, rayOrigin.forward);
        _worldSpaceRayField.SetValue(_ovrPed, wsRay);

        // RaycastAll 수행
        _results.Clear();
        EventSystem.current.RaycastAll(_ovrPed, _results);
        if (_results.Count == 0) return false;

        // 가장 가까운 것 선택
        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < _results.Count; i++)
        {
            var rr = _results[i];

            float d = rr.distance;
            if (d <= 0f && rr.worldPosition != Vector3.zero)
                d = Vector3.Distance(rayOrigin.position, rr.worldPosition);

            if (d <= 0f) continue;

            if (d < best)
            {
                best = d;
                found = true;
            }
        }

        if (!found) return false;

        hitDistance = Mathf.Clamp(best, minDistance, maxDistance);
        return true;
    }

    private void ApplyVisual(float distance, bool hovering)
    {
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // 1) LaserLineDriver 권장
        if (laserVisual != null && rayOrigin != null)
        {
            var state = hovering ? LaserLineDriver.VisualState.Hover : LaserLineDriver.VisualState.Idle;
            laserVisual.Apply(rayOrigin, distance, state);
            return;
        }

        // 2) fallback: LineRenderer 직접 제어
        if (line == null || rayOrigin == null) return;

        if (line.positionCount != 2) line.positionCount = 2;

        Vector3 start = rayOrigin.position;
        Vector3 end = start + rayOrigin.forward * distance;

        if (line.useWorldSpace)
        {
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }
        else
        {
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.forward * distance);
        }

        // 최소한의 색 변경 (상세 스타일은 LaserLineDriver로 통일 권장)
        var c = hovering ? new Color(1f, 0.75f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.8f);
        line.startColor = c;
        line.endColor = c;
    }

    // ===== reflection / helpers =====

    private static void CacheOVRPointerEventDataReflection()
    {
        if (_ovrPedType != null && _worldSpaceRayField != null) return;

        _ovrPedType = FindTypeBySimpleName("OVRPointerEventData");
        if (_ovrPedType == null) return;

        // 보통 public field, 버전에 따라 nonpublic 가능성도 있어서 둘 다 시도
        _worldSpaceRayField = _ovrPedType.GetField("worldSpaceRay", BindingFlags.Public | BindingFlags.Instance)
                           ?? _ovrPedType.GetField("worldSpaceRay", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    /// <summary>
    /// OVRPointerEventData를 만들 수 있으면 만들고, 못 만들면 null.
    /// 절대 PointerEventData로 대체하지 않음(대체하면 worldSpaceRay 주입에서 예외남).
    /// </summary>
    private static PointerEventData CreateOVRPointerEventData(EventSystem es)
    {
        CacheOVRPointerEventDataReflection();
        if (_ovrPedType == null) return null;

        try
        {
            // (EventSystem) 생성자가 있는 경우가 대부분
            return (PointerEventData)Activator.CreateInstance(_ovrPedType, es);
        }
        catch
        {
            return null;
        }
    }

    private static Transform FindTransformByName(string name)
    {
        var all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == name) return all[i];
        }
        return null;
    }

    private static Type FindTypeBySimpleName(string simpleName)
    {
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asms.Length; i++)
        {
            Type[] types;
            try { types = asms[i].GetTypes(); }
            catch { continue; }

            for (int k = 0; k < types.Length; k++)
            {
                if (types[k].Name == simpleName) return types[k];
            }
        }
        return null;
    }
}

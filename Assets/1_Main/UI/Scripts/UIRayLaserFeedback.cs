using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIRayLaserFeedback : MonoBehaviour
{
    [Header("Ray Origin")]
    public Transform rayOrigin;

    [Header("Laser Visual")]
    public LaserLineDriver laserVisual;
    public LineRenderer line;

    [Header("Distance")]
    public float maxDistance = 8f;
    public float minDistance = 0.05f;

    private PointerEventData _ovrPed;
    private EventSystem _cachedEventSystem;
    private readonly List<RaycastResult> _results = new();

    private static Type _ovrPedType;
    private static FieldInfo _worldSpaceRayField;

    private void Awake()
    {
        // rayOrigin이 없으면 자기 자신 사용
        if (rayOrigin == null)
            rayOrigin = transform;

        if (line == null)
            line = GetComponentInChildren<LineRenderer>(true);

        CacheOVRPointerEventDataReflection();

        _cachedEventSystem = EventSystem.current;
        if (_cachedEventSystem != null)
            _ovrPed = CreateOVRPointerEventData(_cachedEventSystem);
    }

    private void OnEnable()
    {
        if (rayOrigin == null)
            rayOrigin = transform;

        _cachedEventSystem = EventSystem.current;
        if (_cachedEventSystem != null)
            _ovrPed = CreateOVRPointerEventData(_cachedEventSystem);
    }

    private void Update()
    {
        if (rayOrigin == null)
        {
            Debug.LogWarning("[UIRayLaser] rayOrigin is null!");
            return;
        }

        if (EventSystem.current == null)
        {
            ApplyVisual(maxDistance, false);
            return;
        }

        if (_cachedEventSystem != EventSystem.current)
        {
            _cachedEventSystem = EventSystem.current;
            _ovrPed = CreateOVRPointerEventData(_cachedEventSystem);
        }

        if (_ovrPed == null)
        {
            ApplyVisual(maxDistance, false);
            return;
        }

        float dist;
        bool hitUI = TryGetUIHitDistance(out dist);
        ApplyVisual(dist, hitUI);
    }

    private bool TryGetUIHitDistance(out float hitDistance)
    {
        hitDistance = maxDistance;

        if (_ovrPedType == null || _worldSpaceRayField == null) return false;
        if (_ovrPed == null || !_ovrPedType.IsInstanceOfType(_ovrPed)) return false;

        var wsRay = new Ray(rayOrigin.position, rayOrigin.forward);
        _worldSpaceRayField.SetValue(_ovrPed, wsRay);

        _results.Clear();
        EventSystem.current.RaycastAll(_ovrPed, _results);
        if (_results.Count == 0) return false;

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

        if (laserVisual != null && rayOrigin != null)
        {
            var state = hovering ? LaserLineDriver.VisualState.Hover : LaserLineDriver.VisualState.Idle;
            laserVisual.Apply(rayOrigin, distance, state);
            return;
        }

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

        var c = hovering ? new Color(1f, 0.75f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.8f);
        line.startColor = c;
        line.endColor = c;
    }

    private static void CacheOVRPointerEventDataReflection()
    {
        if (_ovrPedType != null && _worldSpaceRayField != null) return;

        _ovrPedType = FindTypeBySimpleName("OVRPointerEventData");
        if (_ovrPedType == null) return;

        _worldSpaceRayField = _ovrPedType.GetField("worldSpaceRay", BindingFlags.Public | BindingFlags.Instance)
                           ?? _ovrPedType.GetField("worldSpaceRay", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static PointerEventData CreateOVRPointerEventData(EventSystem es)
    {
        CacheOVRPointerEventDataReflection();
        if (_ovrPedType == null) return null;

        try
        {
            return (PointerEventData)Activator.CreateInstance(_ovrPedType, es);
        }
        catch
        {
            return null;
        }
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
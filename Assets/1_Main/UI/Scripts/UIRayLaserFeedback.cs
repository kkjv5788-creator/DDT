using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRayLaserFeedback : MonoBehaviour
{
    [Header("Refs")]
    public Transform rayOrigin;              // right_laser_begin
    public LineRenderer line;                // LaserVisual_R의 LineRenderer
    public BaseRaycaster uiRaycaster;        // Title Canvas의 OVRRaycaster

    [Header("Laser")]
    public float maxDistance = 20f;
    public float minDistance = 0.05f;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.cyan;

    private PointerEventData _ped;
    private readonly List<RaycastResult> _results = new();

    private void Awake()
    {
        if (line == null) line = GetComponentInChildren<LineRenderer>();
        if (EventSystem.current != null)
            _ped = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        if (rayOrigin == null || line == null || uiRaycaster == null || EventSystem.current == null)
        {
            Draw(maxDistance, false);
            return;
        }

        if (_ped == null) _ped = new PointerEventData(EventSystem.current);

        // OVRRaycaster가 읽을 수 있도록 world-space ray를 세팅 (Oculus Integration 기준)
        // PointerEventData의 확장 필드로 worldSpaceRay를 쓰는 경우가 많아서 Reflection 없이 가장 흔한 패턴으로 처리:
        // 1) OVRPointerEventData가 있으면 그걸 쓰고, 2) 없으면 fallback(항상 maxDistance)
        float dist;
        bool hovering = TryRaycastUI(out dist);

        Draw(dist, hovering);
    }

    private bool TryRaycastUI(out float hitDistance)
    {
        hitDistance = maxDistance;

        // Oculus Integration에 OVRPointerEventData가 있으면 그걸 우선 사용
        // (없으면 컴파일 에러 방지 위해 reflection-like 분기 없이 타입명을 문자열로 탐지)
        var ovrPedType = System.Type.GetType("OVRPointerEventData, Assembly-CSharp");
        if (ovrPedType == null)
            return false;

        if (_ped == null || _ped.GetType() != ovrPedType)
        {
            _ped = (PointerEventData)System.Activator.CreateInstance(ovrPedType, EventSystem.current);
        }

        // OVRPointerEventData.worldSpaceRay = new Ray(...)
        var wsRayField = ovrPedType.GetField("worldSpaceRay");
        if (wsRayField == null) return false;

        wsRayField.SetValue(_ped, new Ray(rayOrigin.position, rayOrigin.forward));

        _results.Clear();
        uiRaycaster.Raycast(_ped, _results);

        if (_results.Count == 0) return false;

        // 가장 가까운 hit
        var rr = _results[0];

        // RaycastResult.distance가 들어오는 게 정상
        float d = rr.distance;
        if (d <= 0f)
        {
            // distance가 비어있으면 worldPosition로 대체
            if (rr.worldPosition != Vector3.zero)
                d = Vector3.Distance(rayOrigin.position, rr.worldPosition);
        }

        d = Mathf.Clamp(d, minDistance, maxDistance);
        hitDistance = d;
        return true;
    }

    private void Draw(float distance, bool hovering)
    {
        var c = hovering ? hoverColor : normalColor;

        // 라인 색
        // (Material color 사용 중이면 material.color로 바꿔도 됨)
        line.startColor = c;
        line.endColor = c;

        // 라인 길이
        Vector3 start = rayOrigin.position;
        Vector3 end = start + rayOrigin.forward * distance;

        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }
}

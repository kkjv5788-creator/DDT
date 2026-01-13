using UnityEngine;

[DisallowMultipleComponent]
public class LaserLineDriver : MonoBehaviour
{
    [Header("Ray Origin (추천: *_laser_begin)")]
    [SerializeField] private Transform rayOrigin;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Visual Target")]
    [SerializeField] private LineRenderer line;

    [Header("Style - Idle")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private float idleWidth = 0.005f;

    [Header("Style - Hit")]
    [SerializeField] private Color hitColor = new Color(1f, 0.6f, 0.2f, 1f);
    [SerializeField] private float hitWidth = 0.008f;

    [Header("Smoothing")]
    [Tooltip("0이면 즉시 반영. 0.05~0.1 권장")]
    [SerializeField] private float smoothTime = 0.06f;

    private float _currentDistance;
    private float _distanceVel;
    private float _currentWidth;
    private float _widthVel;
    private Color _currentColor;

    private void Awake()
    {
        if (!line) line = GetComponentInChildren<LineRenderer>(true);

        if (line)
        {
            line.positionCount = 2;
            line.useWorldSpace = true;
        }

        _currentDistance = maxDistance;
        _currentWidth = idleWidth;
        _currentColor = idleColor;

        ApplyVisual(idleColor, idleWidth);
    }

    private void LateUpdate()
    {
        if (!line || !rayOrigin) return;

        Vector3 start = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        bool hitSomething = Physics.Raycast(start, dir, out RaycastHit hit, maxDistance, hitMask, triggerInteraction);
        float targetDistance = hitSomething ? hit.distance : maxDistance;

        // distance smoothing
        if (smoothTime <= 0f) _currentDistance = targetDistance;
        else _currentDistance = Mathf.SmoothDamp(_currentDistance, targetDistance, ref _distanceVel, smoothTime);

        Vector3 end = start + dir * _currentDistance;

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        // hit feedback (color/width)
        Color targetColor = hitSomething ? hitColor : idleColor;
        float targetWidth = hitSomething ? hitWidth : idleWidth;

        if (smoothTime <= 0f)
        {
            _currentColor = targetColor;
            _currentWidth = targetWidth;
        }
        else
        {
            _currentColor = Color.Lerp(_currentColor, targetColor, Time.deltaTime / smoothTime);
            _currentWidth = Mathf.SmoothDamp(_currentWidth, targetWidth, ref _widthVel, smoothTime);
        }

        ApplyVisual(_currentColor, _currentWidth);
    }

    private void ApplyVisual(Color c, float w)
    {
        if (!line) return;
        line.startColor = c;
        line.endColor = c;
        line.startWidth = w;
        line.endWidth = w;
    }
}

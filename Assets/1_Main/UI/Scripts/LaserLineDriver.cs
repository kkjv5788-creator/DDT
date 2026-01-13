using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class LaserLineDriver : MonoBehaviour
{
    public enum VisualState { Idle, Hover }

    [Header("Line")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private bool useWorldSpace = true;

    [Header("Style - Idle")]
    public Color idleColor = new Color(1f, 1f, 1f, 0.8f);
    public float idleWidth = 0.005f;

    [Header("Style - Hover/Hit")]
    public Color hoverColor = new Color(1f, 0.75f, 0.2f, 1f);
    public float hoverWidth = 0.008f;

    [Header("Smoothing")]
    [Tooltip("0이면 즉시. 0.03~0.08 권장")]
    public float smoothTime = 0.06f;

    private float _curLen;
    private float _lenVel;
    private float _curWidth;
    private float _widthVel;
    private Color _curColor;

    private void Awake()
    {
        if (!line) line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = useWorldSpace;

        _curLen = 5f;
        _curWidth = idleWidth;
        _curColor = idleColor;
        ApplyStyle(idleColor, idleWidth);
    }

    /// <summary>
    /// rayOrigin 기준으로 "start->end"를 갱신한다.
    /// </summary>
    public void Apply(Transform rayOrigin, float targetLength, VisualState state)
    {
        if (!line || !rayOrigin) return;

        // length smoothing
        if (smoothTime <= 0f) _curLen = targetLength;
        else _curLen = Mathf.SmoothDamp(_curLen, targetLength, ref _lenVel, smoothTime);

        Vector3 start = rayOrigin.position;
        Vector3 end = start + rayOrigin.forward * _curLen;

        if (line.useWorldSpace)
        {
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }
        else
        {
            // 로컬을 쓰려면 LaserVisual이 rayOrigin과 같은 좌표계여야 함
            // (지금은 worldSpace 권장)
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.forward * _curLen);
        }

        // style smoothing
        Color targetColor = (state == VisualState.Hover) ? hoverColor : idleColor;
        float targetWidth = (state == VisualState.Hover) ? hoverWidth : idleWidth;

        if (smoothTime <= 0f)
        {
            _curColor = targetColor;
            _curWidth = targetWidth;
        }
        else
        {
            _curColor = Color.Lerp(_curColor, targetColor, Time.deltaTime / smoothTime);
            _curWidth = Mathf.SmoothDamp(_curWidth, targetWidth, ref _widthVel, smoothTime);
        }

        ApplyStyle(_curColor, _curWidth);
    }

    private void ApplyStyle(Color c, float w)
    {
        line.startColor = c;
        line.endColor = c;
        line.startWidth = w;
        line.endWidth = w;
    }
}

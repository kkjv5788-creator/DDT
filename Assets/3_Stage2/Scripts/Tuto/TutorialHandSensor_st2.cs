using UnityEngine;

/// <summary>
/// 튜토리얼 전용 손 센서 - 메인 HandCatchSensor와 독립
/// </summary>
public class TutorialHandSensor_st2 : MonoBehaviour
{
    [Header("설정")]
    public OVRInput.Controller handController;
    public LayerMask fishLayer;
    public float raycastLength = 0.15f;

    [Header("LineRenderer")]
    public LineRenderer lineRenderer;
    public bool showLineRenderer = true;

    [Header("라인 색상")]
    public Color noTargetColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    public Color hasTargetColor = new Color(0f, 1f, 0f, 0.8f);
    public Color resolvedTargetColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("라인 스타일")]
    public float lineWidth = 0.002f;

    private TutorialFishToken_st2 currentTarget;
    private RaycastHit[] hits = new RaycastHit[10];
    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
        SetupLineRenderer();
    }

    public void Initialize(OVRInput.Controller controller)
    {
        handController = controller;

        // Awake에서 이미 초기화되었지만 안전하게 재확인
        if (cachedTransform == null)
            cachedTransform = transform;

        SetupLineRenderer();
    }

    void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        if (lineRenderer.material == null || lineRenderer.material.shader.name != "Unlit/Color")
        {
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        }

        lineRenderer.startColor = noTargetColor;
        lineRenderer.endColor = noTargetColor;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    void Update()
    {
        // ✅ Null 체크 추가
        if (cachedTransform == null)
            cachedTransform = transform;

        UpdateCurrentTarget();
        UpdateLineRenderer();
    }

    void UpdateCurrentTarget()
    {
        // ✅ 안전 체크
        if (cachedTransform == null) return;

        Ray ray = new Ray(cachedTransform.position, cachedTransform.forward);
        int hitCount = Physics.RaycastNonAlloc(ray, hits, raycastLength, fishLayer);

        TutorialFishToken_st2 closestFish = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            var fish = hits[i].collider.GetComponent<TutorialFishToken_st2>();
            if (fish == null) continue;
            if (fish.isResolved) continue;

            float distance = hits[i].distance;

            // 히스테리시스
            if (currentTarget != null)
            {
                if (fish == currentTarget)
                {
                    closestFish = fish;
                    break;
                }

                if (distance < closestDistance - 0.03f)
                {
                    closestDistance = distance;
                    closestFish = fish;
                }
            }
            else
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFish = fish;
                }
            }
        }

        currentTarget = closestFish;
    }

    void UpdateLineRenderer()
    {
        if (lineRenderer == null || !showLineRenderer)
            return;

        // ✅ 안전 체크
        if (cachedTransform == null) return;

        lineRenderer.enabled = true;

        Vector3 startPos = cachedTransform.position;
        Vector3 endPos = startPos + cachedTransform.forward * raycastLength;

        if (currentTarget != null)
        {
            endPos = currentTarget.transform.position;
        }

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        Color lineColor = GetLineColor();
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    Color GetLineColor()
    {
        if (currentTarget == null)
            return noTargetColor;
        else if (currentTarget.isResolved)
            return resolvedTargetColor;
        else
            return hasTargetColor;
    }

    public TutorialFishToken_st2 GetCurrentTarget()
    {
        return currentTarget;
    }

    public void SetLineRendererVisible(bool visible)
    {
        showLineRenderer = visible;
        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }

    // ✅ 추가: 튜토리얼 종료 시 정리
    void OnDisable()
    {
        currentTarget = null;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }
}
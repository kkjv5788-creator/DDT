using UnityEngine;

/// <summary>
/// 메인 전용 손 센서 - 레이캐스트 기반 타겟 선택 + 라인렌더러 표시
/// (튜토 TutorialHandSensor_st2 로직을 FishCatchToken_st2로 이식)
/// </summary>
public class HandCatchSensor_st2 : MonoBehaviour
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

    [Header("현재 타겟(읽기용)")]
    public FishCatchToken_st2 currentTarget;

    private readonly RaycastHit[] hits = new RaycastHit[16];
    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
        SetupLineRenderer();
    }

    public void Initialize(OVRInput.Controller controller)
    {
        handController = controller;

        if (cachedTransform == null)
            cachedTransform = transform;

        SetupLineRenderer();
    }

    void SetupLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        if (lineRenderer.material == null || lineRenderer.material.shader == null || lineRenderer.material.shader.name != "Unlit/Color")
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));

        lineRenderer.startColor = noTargetColor;
        lineRenderer.endColor = noTargetColor;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    void Update()
    {
        if (cachedTransform == null)
            cachedTransform = transform;

        UpdateCurrentTarget();
        UpdateLineRenderer();
    }

    void UpdateCurrentTarget()
    {
        if (cachedTransform == null) return;

        Ray ray = new Ray(cachedTransform.position, cachedTransform.forward);
        int hitCount = Physics.RaycastNonAlloc(ray, hits, raycastLength, fishLayer, QueryTriggerInteraction.Collide);

        FishCatchToken_st2 closestFish = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            var col = hits[i].collider;
            if (col == null) continue;

            // ✅ 자식 콜라이더 대응(메인 붕어빵은 보통 자식에 콜라이더가 있음)
            var fish = col.GetComponentInParent<FishCatchToken_st2>();
            if (fish == null) continue;
            if (!fish.gameObject.activeInHierarchy) continue;
            if (fish.isResolved) continue;

            float distance = hits[i].distance;

            // ✅ 히스테리시스(튜토와 동일): 이미 잡고 있는 타겟이 있으면 쉽게 안 바뀜
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
        if (lineRenderer == null || !showLineRenderer) return;
        if (cachedTransform == null) return;

        lineRenderer.enabled = true;

        Vector3 startPos = cachedTransform.position;
        Vector3 endPos = startPos + cachedTransform.forward * raycastLength;

        if (currentTarget != null)
            endPos = currentTarget.transform.position;

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        Color lineColor = GetLineColor();
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    Color GetLineColor()
    {
        if (currentTarget == null) return noTargetColor;
        if (currentTarget.isResolved) return resolvedTargetColor;
        return hasTargetColor;
    }

    public FishCatchToken_st2 GetCurrentTarget()
    {
        return currentTarget;
    }

    public void SetLineRendererVisible(bool visible)
    {
        showLineRenderer = visible;
        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }

    void OnDisable()
    {
        currentTarget = null;
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }
}

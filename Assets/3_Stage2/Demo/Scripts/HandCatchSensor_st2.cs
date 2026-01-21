using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class HandCatchSensor_st2 : MonoBehaviour
{
    [Header("설정")]
    public OVRInput.Controller handController;
    public LayerMask fishLayer;
    public float raycastLength = 0.15f;

    [Header("LineRenderer 설정")]
    public LineRenderer lineRenderer;
    public bool showLineRenderer = true;

    [Header("라인 색상")]
    public Color noTargetColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    public Color hasTargetColor = new Color(0f, 1f, 0f, 0.8f);
    public Color resolvedTargetColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("라인 스타일")]
    public float lineWidth = 0.002f;
    public int lineSegments = 20;
    public AnimationCurve lineWidthCurve = AnimationCurve.Linear(0, 1, 1, 0.3f);

    [Header("상태")]
    public FishCatchToken_st2 currentTarget;

    private RaycastHit[] hits = new RaycastHit[10];
    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
        SetupLineRenderer();
    }

    void Start()
    {
        if (lineRenderer != null && !showLineRenderer)
        {
            lineRenderer.enabled = false;
        }
    }

    // ✅ 추가: 활성화 시 초기화
    void OnEnable()
    {
        currentTarget = null;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = showLineRenderer;
        }
    }

    // ✅ 추가: 비활성화 시 정리
    void OnDisable()
    {
        currentTarget = null;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    void Update()
    {
        UpdateCurrentTarget();
        UpdateLineRenderer();
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
        lineRenderer.widthCurve = lineWidthCurve;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    void UpdateCurrentTarget()
    {
        Ray ray = new Ray(cachedTransform.position, cachedTransform.forward);
        int hitCount = Physics.RaycastNonAlloc(ray, hits, raycastLength, fishLayer);

        FishCatchToken_st2 closestFish = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            var fish = hits[i].collider.GetComponent<FishCatchToken_st2>();

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

        // 게임 상태 체크 (Playing 상태에서만 표시)
        if (GameFlowController_st2.Instance.CurrentState != GameState_st2.GameStatest2.Playing)
        {
            lineRenderer.enabled = false;
            return;
        }

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

    public void ToggleLineRenderer()
    {
        showLineRenderer = !showLineRenderer;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = showLineRenderer;
        }
    }

    public void SetLineRendererVisible(bool visible)
    {
        showLineRenderer = visible;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
    }
}
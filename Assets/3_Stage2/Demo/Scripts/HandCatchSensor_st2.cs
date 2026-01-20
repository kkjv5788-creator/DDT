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
    public Color noTargetColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // 회색 반투명
    public Color hasTargetColor = new Color(0f, 1f, 0f, 0.8f);      // 초록색
    public Color resolvedTargetColor = new Color(1f, 0f, 0f, 0.5f); // 빨간색 (이미 처리됨)

    [Header("라인 스타일")]
    public float lineWidth = 0.002f; // 라인 굵기 (0.2cm)
    public int lineSegments = 20;    // 라인 세그먼트 수 (부드러운 곡선용)
    public AnimationCurve lineWidthCurve = AnimationCurve.Linear(0, 1, 1, 0.3f); // 시작->끝 굵기 변화

    [Header("상태")]
    public FishCatchToken_st2 currentTarget;

    private RaycastHit[] hits = new RaycastHit[10];
    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;

        // LineRenderer 자동 설정
        SetupLineRenderer();
    }

    void Start()
    {
        if (lineRenderer != null && !showLineRenderer)
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
            // LineRenderer가 없으면 자동 생성
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // 기본 설정
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        // Material 설정 (Unlit/Color 셰이더 사용)
        if (lineRenderer.material == null || lineRenderer.material.shader.name != "Unlit/Color")
        {
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        }

        // 초기 색상
        lineRenderer.startColor = noTargetColor;
        lineRenderer.endColor = noTargetColor;

        // 굵기 곡선 적용
        lineRenderer.widthCurve = lineWidthCurve;

        // 그림자/라이팅 끄기 (성능 최적화)
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    void UpdateCurrentTarget()
    {
        Ray ray = new Ray(cachedTransform.position, cachedTransform.forward);
        int hitCount = Physics.RaycastNonAlloc(ray, hits, raycastLength, fishLayer);

        // ✅ 디버그 추가
        Debug.Log($"[{handController}] Raycast Hit Count: {hitCount}");

        if (hitCount == 0)
        {
            Debug.LogWarning($"[{handController}] 아무것도 감지 안 됨! Layer: {fishLayer.value}");
        }

        FishCatchToken_st2 closestFish = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            // ✅ 디버그 추가
            Debug.Log($"[{handController}] Hit [{i}]: {hits[i].collider.gameObject.name}, Layer: {hits[i].collider.gameObject.layer}");

            var fish = hits[i].collider.GetComponent<FishCatchToken_st2>();

            if (fish == null)
            {
                Debug.LogError($"[{handController}] FishCatchToken_st2 컴포넌트 없음: {hits[i].collider.gameObject.name}");
                continue;
            }

            if (fish.isResolved)
            {
                Debug.Log($"[{handController}] 이미 처리된 Fish: {fish.name}");
                continue;
            }

            float distance = hits[i].distance;

            // 히스테리시스: 현재 타겟 유지
            if (currentTarget != null)
            {
                if (fish == currentTarget)
                {
                    closestFish = fish;
                    Debug.Log($"[{handController}] 기존 타겟 유지: {fish.name}");
                    break;
                }

                if (distance < closestDistance - 0.03f)
                {
                    closestDistance = distance;
                    closestFish = fish;
                    Debug.Log($"[{handController}] 새 타겟 발견: {fish.name}, 거리: {distance:F3}m");
                }
            }
            else
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFish = fish;
                    Debug.Log($"[{handController}] 첫 타겟 발견: {fish.name}, 거리: {distance:F3}m");
                }
            }
        }

        currentTarget = closestFish;

        // ✅ 최종 결과 디버그
        if (currentTarget != null)
        {
            Debug.Log($"[{handController}] ✓ 현재 타겟: {currentTarget.name}");
        }
        else
        {
            Debug.Log($"[{handController}] ✗ 타겟 없음");
        }
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

        // 시작점: 손 위치
        Vector3 startPos = cachedTransform.position;

        // 끝점: Raycast 방향
        Vector3 endPos = startPos + cachedTransform.forward * raycastLength;

        // 타겟이 있으면 타겟까지만 그리기
        if (currentTarget != null)
        {
            endPos = currentTarget.transform.position;
        }

        // LineRenderer 위치 설정
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // 색상 설정
        Color lineColor = GetLineColor();
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    Color GetLineColor()
    {
        if (currentTarget == null)
        {
            // 타겟 없음: 회색
            return noTargetColor;
        }
        else if (currentTarget.isResolved)
        {
            // 이미 처리된 타겟: 빨간색
            return resolvedTargetColor;
        }
        else
        {
            // 잡을 수 있는 타겟: 초록색
            return hasTargetColor;
        }
    }

    /// <summary>
    /// 런타임에서 LineRenderer 표시/숨김 토글
    /// </summary>
    public void ToggleLineRenderer()
    {
        showLineRenderer = !showLineRenderer;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = showLineRenderer;
        }
    }

    /// <summary>
    /// LineRenderer 표시 설정
    /// </summary>
    public void SetLineRendererVisible(bool visible)
    {
        showLineRenderer = visible;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
    }
}
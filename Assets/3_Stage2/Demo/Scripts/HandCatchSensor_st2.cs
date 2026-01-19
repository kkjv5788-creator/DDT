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

    [Header("상태")]
    public FishCatchToken_st2 currentTarget;

    private RaycastHit[] hits = new RaycastHit[10];
    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
    }

    void Update()
    {
        UpdateCurrentTarget();
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
            if (fish == null || fish.isResolved) continue;

            float distance = hits[i].distance;

            // 히스테리시스: 현재 타겟 유지
            if (currentTarget != null)
            {
                if (fish == currentTarget)
                {
                    closestFish = fish;
                    break;
                }

                if (distance < closestDistance - 0.03f) // 3cm 이상 차이나야 교체
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
}

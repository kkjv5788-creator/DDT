using UnityEngine;

public class HandCatchSensor_st2 : MonoBehaviour
{
    [Header("레이캐스트 설정")]
    public float raycastLength = 0.35f;
    public LayerMask fishLayer;

    [Header("현재 타겟")]
    public FishCatchToken_st2 currentTarget;

    // (기존에 LineRenderer, 디버그 등이 있으면 그대로 유지 가능)
    private Transform cachedTransform;
    private readonly RaycastHit[] hits = new RaycastHit[16];

    void Awake()
    {
        cachedTransform = transform;
    }

    void Update()
    {
        UpdateCurrentTarget();
        // 기존 UpdateLineRenderer() 같은 게 있으면 여기서 호출 유지
    }

    void UpdateCurrentTarget()
    {
        if (cachedTransform == null) cachedTransform = transform;

        Vector3 origin = cachedTransform.position;
        Vector3 dir = cachedTransform.forward;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            dir,
            hits,
            raycastLength,
            fishLayer,
            QueryTriggerInteraction.Collide
        );

        FishCatchToken_st2 closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            var col = hits[i].collider;
            if (col == null) continue;

            // ✅ 메인에서 “안 잡히는” 주요 원인 해결: 자식 콜라이더 → 루트 토큰 탐색
            var fish = col.GetComponentInParent<FishCatchToken_st2>();
            if (fish == null) continue;
            if (!fish.gameObject.activeInHierarchy) continue;
            if (fish.isResolved) continue; // 이미 판정 끝난 애는 타겟 제외

            float d = Vector3.Distance(origin, fish.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = fish;
            }
        }

        currentTarget = closest;
    }
}

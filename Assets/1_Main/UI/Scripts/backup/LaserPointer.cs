using UnityEngine;
using UnityEngine.EventSystems; // PointerEventData를 쓰기 위해 필요

public class LaserPointer : MonoBehaviour
{
    [Header("설정")]
    public float rayDistance = 100f; // 레이저 길이
    public LayerMask interactableLayer; // 상호작용할 레이어 (설정 안하면 모든 물체 감지)

    private LineRenderer line;
    private DoorTrigger currentTrigger = null; // 현재 가리키고 있는 문

    void Start()
    {
        line = GetComponent<LineRenderer>();
        // 기본적으로 레이어 설정이 없으면 Everything으로 설정
        if (interactableLayer == 0) interactableLayer = -1;
    }

    void Update()
    {
        line.SetPosition(0, transform.position); // 시작점: 컨트롤러

        RaycastHit hit;
        // 컨트롤러 정면으로 레이저 발사
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance, interactableLayer))
        {
            // 1. 레이저 끝점 그리기
            line.SetPosition(1, hit.point);

            // 2. 닿은 물체가 DoorTrigger를 가지고 있는지 확인
            DoorTrigger hitTrigger = hit.collider.GetComponent<DoorTrigger>();

            if (hitTrigger != null)
            {
                // A. 새로운 문을 가리키기 시작했으면 -> Enter 효과 실행
                if (currentTrigger != hitTrigger)
                {
                    if (currentTrigger != null) currentTrigger.OnPointerExit(null); // 기존꺼 끄기
                    currentTrigger = hitTrigger;
                    currentTrigger.OnPointerEnter(null); // 새거 켜기
                }

                // B. 오른쪽 컨트롤러 트리거(검지)를 눌렀을 때 -> Click 실행
                // (Oculus Quest 기준: SecondaryIndexTrigger)
                if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
                {
                    currentTrigger.OnPointerClick(null);
                }
            }
            else
            {
                // 문이 아닌 다른 벽을 보고 있을 때
                ClearCurrentTrigger();
            }
        }
        else
        {
            // 허공을 볼 때
            line.SetPosition(1, transform.position + transform.forward * 3.0f);
            ClearCurrentTrigger();
        }
    }

    // 포커스 해제 처리 함수
    private void ClearCurrentTrigger()
    {
        if (currentTrigger != null)
        {
            currentTrigger.OnPointerExit(null);
            currentTrigger = null;
        }
    }
}
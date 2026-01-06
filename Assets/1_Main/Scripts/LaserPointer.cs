using UnityEngine;

public class LaserPointer : MonoBehaviour
{
    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
    }

    void Update()
    {
        // 1. 시작점: 컨트롤러 위치
        line.SetPosition(0, transform.position);

        // 2. 끝점 계산 (레이저 쏘기)
        RaycastHit hit;
        // 컨트롤러 정면 방향으로 레이저 발사
        if (Physics.Raycast(transform.position, transform.forward, out hit, 100f))
        {
            // 무언가 닿았으면 거기까지만 그림
            line.SetPosition(1, hit.point);
        }
        else
        {
            // 허공이면 3미터 앞까지 그림
            line.SetPosition(1, transform.position + transform.forward * 3.0f);
        }
    }
}
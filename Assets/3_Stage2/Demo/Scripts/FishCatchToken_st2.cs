using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishCatchToken_st2 : MonoBehaviour
{
    [Header("판정 데이터")]
    public double popTime;
    public bool isResolved = false;
    public bool isCaught = false;
    public OVRInput.Controller? assignedHand = null;

    [Header("이동 데이터")]
    private Vector3 moldPosition;      // 틀 위치
    private Transform parentMold;

    [Header("점프 설정 (Rigidbody)")]
    public float jumpHeight = 2.5f;       // 점프 높이 (위로 얼마나 올라갈지)
    public float forwardDistance = 2.0f;  // ✅ -Z 방향으로 날아갈 거리
    public float gravity = 9.81f;         // 중력 가속도
    public LayerMask groundLayer;         // ✅ 땅 레이어

    private Rigidbody rb;
    private bool hasJumped = false;

    // 소유 몰드 추적
    public MoldController_st2 ownerMold;

    void Awake()
    {
        // Rigidbody 컴포넌트 가져오기 또는 추가
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Rigidbody 설정
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // 회전 고정 (선택)
    }

    // 초기화
    public void Initialize(double popDspTime, Transform mold, MoldController_st2 owner)
    {
        popTime = popDspTime;
        isResolved = false;
        isCaught = false;
        assignedHand = null;
        parentMold = mold;
        ownerMold = owner;
        hasJumped = false;

        // 위치 설정
        moldPosition = mold.position;
        transform.position = moldPosition;

        // Rigidbody 초기화
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.isKinematic = false;

        // ✅ Scale 설정
        transform.localScale = Vector3.one * 100.0f;

        // ✅ 점프 시작 (플레이어 방향 회전 전에 실행)
        PerformJump();

        // 플레이어 방향 회전 (점프 후에 실행)
        Transform playerCam = Camera.main.transform;
        Vector3 toPlayer = (playerCam.position - transform.position).normalized;
        transform.right = toPlayer;
    }

    // ✅ Rigidbody 기반 점프 (포물선 운동 - 월드 좌표 기준)
    void PerformJump()
    {
        if (hasJumped) return;

        // 위쪽 속도 계산 (점프 높이 기준)
        // v_y = sqrt(2 * g * h)
        float jumpVelocityY = Mathf.Sqrt(2f * gravity * jumpHeight);

        // 비행 시간 계산 (올라갔다 떨어지는 시간)
        // t = 2 * v_y / g
        float flightTime = 2f * jumpVelocityY / gravity;

        // -Z 방향 속도 계산 (포물선을 그리며 forwardDistance만큼 이동)
        // v_z = distance / time
        float forwardVelocityZ = forwardDistance / flightTime;

        // ✅ 월드 좌표 기준: Y축(위), -Z축(앞)
        Vector3 velocity = new Vector3(0, jumpVelocityY, -forwardVelocityZ);

        rb.velocity = velocity;

        // 중력 설정
        Physics.gravity = new Vector3(0, -gravity, 0);

        hasJumped = true;

        Debug.Log($"Jump! Y velocity: {jumpVelocityY}, Z velocity: {-forwardVelocityZ}, flight time: {flightTime}s");
    }

    // fish 움직임 업데이트
    public void UpdateMovement()
    {
        if (isResolved) return;

        // Rigidbody가 자동으로 물리 처리
        // 땅 충돌 감지는 OnCollisionEnter에서 처리
    }

    // ✅ 충돌 감지 - 땅에 닿으면 Miss 처리
    void OnCollisionEnter(Collision collision)
    {
        // 이미 처리된 경우 무시
        if (isResolved) return;

        // 땅 레이어와 충돌했는지 확인
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Debug.Log("붕어빵이 땅에 떨어짐 - Miss 처리");

            // Miss 처리
            isResolved = true;
            isCaught = false;

            // ownerMold로 릴리즈
            if (ownerMold != null)
            {
                ownerMold.ReleaseFish(this);
            }
        }
    }

    // ✅ 잡혔을 때 Rigidbody 비활성화
    public void OnCaught()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ✅ 풀로 반환될 때 Rigidbody 리셋
    public void OnReturnToPool()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        hasJumped = false;
    }
}
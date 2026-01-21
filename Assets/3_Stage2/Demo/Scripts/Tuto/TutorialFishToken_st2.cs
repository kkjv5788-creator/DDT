using UnityEngine;

/// <summary>
/// 튜토리얼 전용 물고기 토큰 - 메인 FishCatchToken과 독립
/// </summary>
public class TutorialFishToken_st2 : MonoBehaviour
{
    [Header("판정 데이터")]
    public double popTime;
    public bool isResolved = false;
    public bool isCaught = false;

    [Header("물리")]
    private Rigidbody rb;
    private bool hasJumped = false;

    // 소유 몰드
    public TutorialMoldController_st2 ownerMold;

    // 점프 설정 (런타임에 주입)
    private float jumpHeight;
    private float forwardDistance;
    private float gravity;
    private LayerMask groundLayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void Initialize(double popDspTime, Transform mold, TutorialMoldController_st2 owner,
                           float jh, float fd, float grav, LayerMask gl)
    {
        popTime = popDspTime;
        isResolved = false;
        isCaught = false;
        ownerMold = owner;
        hasJumped = false;

        jumpHeight = jh;
        forwardDistance = fd;
        gravity = grav;
        groundLayer = gl;

        transform.position = mold.position;
        transform.localScale = Vector3.one * 100.0f;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.isKinematic = false;

        PerformJump();

        // 플레이어 방향 보기
        Transform playerCam = Camera.main.transform;
        Vector3 toPlayer = (playerCam.position - transform.position).normalized;
        transform.right = toPlayer;
    }

    void PerformJump()
    {
        if (hasJumped) return;

        float jumpVelocityY = Mathf.Sqrt(2f * gravity * jumpHeight);
        float flightTime = 2f * jumpVelocityY / gravity;
        float forwardVelocityZ = forwardDistance / flightTime;

        Vector3 velocity = new Vector3(0, jumpVelocityY, -forwardVelocityZ);
        rb.velocity = velocity;

        Physics.gravity = new Vector3(0, -gravity, 0);
        hasJumped = true;
    }

    public void UpdateMovement()
    {
        // Rigidbody가 자동으로 물리 처리
    }

    void OnCollisionEnter(Collision collision)
    {
        // 땅에 떨어지면 풀로 반환
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (ownerMold != null)
            {
                ownerMold.ReleaseFish(this);
            }
        }
    }

    public void OnCaught()
    {
        isCaught = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void OnReturnToPool()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        hasJumped = false;
        isResolved = false;
        isCaught = false;
    }
}
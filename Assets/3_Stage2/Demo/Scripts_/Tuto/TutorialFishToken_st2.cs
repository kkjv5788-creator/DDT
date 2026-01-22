using UnityEngine;

/// <summary>
/// 튜토리얼 전용 붕어빵 토큰 - 메인 FishCatchToken_st2와 독립
/// </summary>
public class TutorialFishToken_st2 : MonoBehaviour
{
    [Header("판정 데이터")]
    public double popTime;
    public bool isResolved = false;
    public bool isCaught = false;

    [Header("SFX (Catch Success)")]
    [SerializeField] private AudioSource catchSfxSource; /*[변경가능_잡기성공오디오소스]*/
    [SerializeField] private AudioClip catchSnapClip;    /*[변경가능_착사운드클립]*/
    [SerializeField, Range(0f, 1f)] private float catchSnapVolume = 1f; /*[변경가능_착볼륨]*/
    [SerializeField] private bool catchSnap3D = true; /*[변경가능_3D사운드]*/

    [Header("물리")]
    private Rigidbody rb;
    private bool hasJumped = false;

    // 소유 몰드 추적
    public TutorialMoldController_st2 ownerMold;

    // 점프 설정 (인스펙터에서 주입)
    private float jumpHeight;
    private float forwardDistance;
    private float gravity;
    private LayerMask groundLayer;

    private void Reset()
    {
        if (catchSfxSource == null) catchSfxSource = GetComponent<AudioSource>();
    }

    private AudioSource GetOrCreateCatchSfxSource()
    {
        if (catchSfxSource != null) return catchSfxSource;

        catchSfxSource = GetComponent<AudioSource>();
        if (catchSfxSource == null) catchSfxSource = gameObject.AddComponent<AudioSource>();

        catchSfxSource.playOnAwake = false;
        catchSfxSource.spatialBlend = catchSnap3D ? 1f : 0f;
        return catchSfxSource;
    }

    private void PlayCatchSnapSfx()
    {
        if (catchSnapClip == null) return;

        var src = GetOrCreateCatchSfxSource();
        if (src == null)
        {
            AudioSource.PlayClipAtPoint(catchSnapClip, transform.position, catchSnapVolume);
            return;
        }

        src.spatialBlend = catchSnap3D ? 1f : 0f;
        src.PlayOneShot(catchSnapClip, catchSnapVolume);
    }

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

        if (catchSfxSource == null) catchSfxSource = GetComponent<AudioSource>();
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

        // 플레이어 방향 바라보기
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
        // 바닥 충돌하면 풀로 반환
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

        // ✅ '착' 사운드 (성공 시 1회)
        PlayCatchSnapSfx();

        if (rb != null)
        {
            // ✅ kinematic 바꾸기 전에 velocity를 먼저 0으로 (Unity warning 방지)
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    public void OnReturnToPool()
    {
        if (catchSfxSource != null) catchSfxSource.Stop();

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

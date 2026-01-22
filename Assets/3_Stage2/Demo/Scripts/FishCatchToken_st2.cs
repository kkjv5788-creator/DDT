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

    [Header("SFX (Catch Success)")]
    [SerializeField] private AudioSource catchSfxSource; /*[변경가능_잡기성공오디오소스]*/
    [SerializeField] private AudioClip catchSnapClip;    /*[변경가능_착사운드클립]*/
    [SerializeField, Range(0f, 1f)] private float catchSnapVolume = 1f; /*[변경가능_착볼륨]*/
    [SerializeField] private bool catchSnap3D = true; /*[변경가능_3D사운드]*/

    [Header("이동 데이터")]
    private Vector3 moldPosition;
    private Transform parentMold;

    [Header("점프 설정 (Rigidbody)")]
    public float jumpHeight = 2.5f;
    public float forwardDistance = 2.0f;
    public float gravity = 9.81f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private bool hasJumped = false;

    // 소유 몰드 추적
    public MoldController_st2 ownerMold;

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

        // SFX 소스 자동 세팅(필요 시)
        if (catchSfxSource == null) catchSfxSource = GetComponent<AudioSource>();
    }

    public void Initialize(double popDspTime, Transform mold, MoldController_st2 owner)
    {
        popTime = popDspTime;
        isResolved = false;
        isCaught = false;
        assignedHand = null;
        parentMold = mold;
        ownerMold = owner;
        hasJumped = false;

        moldPosition = mold.position;
        transform.position = moldPosition;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.isKinematic = false;

        transform.localScale = Vector3.one * 100.0f;

        PerformJump();

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
        // ✅ 잡아서(Perfect/Good) 손에 붙은 애는 바닥 충돌 무시
        if (isCaught) return;

        // ✅ 바닥 충돌 체크 (레이어 또는 태그로 확인)
        bool isGround = false;

        // 방법 1: LayerMask 사용
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGround = true;
        }

        // 방법 2: 태그 사용 (보조 체크)
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = true;
        }

        if (!isGround) return;

        // ✅ 아직 판정 안 된 상태에서 바닥 = Miss 확정 (기존 유지)
        if (!isResolved)
        {
            Debug.Log($"✅ Fish hit ground - triggering MISS for {gameObject.name}");

            // MISS 피드백
            FeedbackManager_st2.Instance?.ShowJudgeFeedback(transform.position, "MISS");
        }

        // ✅ 이미 Miss든 판정 전이든 "바닥 닿으면 무조건 사라짐" (풀로 반환)
        if (ownerMold != null)
        {
            ownerMold.ReleaseFish(this);
            Debug.Log($"✅ Fish returned to pool after ground collision");
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
    }
}

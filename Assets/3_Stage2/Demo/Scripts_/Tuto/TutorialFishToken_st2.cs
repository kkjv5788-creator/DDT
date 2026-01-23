using UnityEngine;

/// <summary>
/// 튜토리얼 전용 붕어빵 토큰 - 메인 FishCatchToken과 독립
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

    public TutorialMoldController_st2 ownerMold;

    private float jumpHeight;
    private float forwardDistance;
    private float gravity;
    private LayerMask groundLayer;

    [Header("SFX (Catch Success)")]
    [SerializeField] private AudioSource catchSfxSource;
    [SerializeField] private AudioClip catchSnapClip;
    [SerializeField, Range(0f, 1f)] private float catchSnapVolume = 1f;
    [SerializeField] private bool catchSnap3D = true;

    [Header("VFX (Catch Success)")]
    [SerializeField] private GameObject catchVfxPrefab;
    [SerializeField] private Transform catchVfxAnchor;
    [SerializeField] private Vector3 catchVfxLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 catchVfxLocalEuler = Vector3.zero;
    [SerializeField] private float catchVfxAutoDestroySeconds = 2.0f;

    private bool catchFxPlayed = false;

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
        src.spatialBlend = catchSnap3D ? 1f : 0f;
        src.PlayOneShot(catchSnapClip, catchSnapVolume);
    }

    private void SpawnCatchVfx()
    {
        if (catchVfxPrefab == null) return;

        Transform a = catchVfxAnchor != null ? catchVfxAnchor : transform;
        Vector3 pos = a.TransformPoint(catchVfxLocalOffset);
        Quaternion rot = a.rotation * Quaternion.Euler(catchVfxLocalEuler);
        var vfx = Instantiate(catchVfxPrefab, pos, rot);
        AutoDestroyVfx(vfx, catchVfxAutoDestroySeconds);
    }

    static void AutoDestroyVfx(GameObject vfxRoot, float fallbackSeconds)
    {
        if (vfxRoot == null) return;

        var ps = vfxRoot.GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            var main = ps.main;
            float lifeMax = 0f;

            var sl = main.startLifetime;
            if (sl.mode == ParticleSystemCurveMode.Constant) lifeMax = sl.constant;
            else if (sl.mode == ParticleSystemCurveMode.TwoConstants) lifeMax = sl.constantMax;
            else lifeMax = sl.constantMax;

            float total = Mathf.Max(0.1f, main.duration + lifeMax);
            UnityEngine.Object.Destroy(vfxRoot, total);
            return;
        }

        UnityEngine.Object.Destroy(vfxRoot, Mathf.Max(0.1f, fallbackSeconds));
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

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

        catchFxPlayed = false;

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
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (ownerMold != null)
                ownerMold.ReleaseFish(this);
        }
    }

    public void OnCaught()
    {
        if (isCaught) return;
        isCaught = true;

        if (!catchFxPlayed)
        {
            catchFxPlayed = true;
            PlayCatchSnapSfx();
            SpawnCatchVfx();
        }

        if (rb != null)
        {
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
        catchFxPlayed = false;
    }
}

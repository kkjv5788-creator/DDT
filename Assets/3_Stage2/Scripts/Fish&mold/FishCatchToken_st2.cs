using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class FishCatchToken_st2 : MonoBehaviour
{
    [Header("판정 데이터")]
    public double popTime;
    public bool isResolved = false;
    public bool isCaught = false;
    public OVRInput.Controller? assignedHand = null;

    [Header("이동 데이터")]
    private Vector3 moldPosition;
    private Transform parentMold;

    [Header("점프 설정 (Rigidbody)")]
    public float jumpHeight = 2.5f;
    public float forwardDistance = 2.0f;
    public float gravity = 9.81f;
    public LayerMask groundLayer;

    [Header("SFX (Catch Success)")]
    [SerializeField] private AudioSource catchSfxSource; /*[변경가능_잡기성공오디오소스]*/
    [SerializeField] private AudioClip catchSnapClip;    /*[변경가능_착사운드클립]*/
    [SerializeField, Range(0f, 1f)] private float catchSnapVolume = 1f; /*[변경가능_착볼륨]*/
    [SerializeField] private bool catchSnap3D = true;    /*[변경가능_3D사운드]*/

    [Header("VFX (Catch Success)")]
    [SerializeField] private GameObject catchVfxPrefab;  /*[변경가능_착이펙트프리팹]*/
    [SerializeField] private Transform catchVfxAnchor;   /*[변경가능_착이펙트기준점] (없으면 fish transform)*/
    [SerializeField] private Vector3 catchVfxLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 catchVfxLocalEuler = Vector3.zero;
    [SerializeField] private float catchVfxAutoDestroySeconds = 2.0f;

    public double popDspTime => popTime;

    private bool catchFxPlayed = false;

    private Rigidbody rb;
    private bool hasJumped = false;

    // 바닥 반환 1회 가드
    private bool _releasedToPoolOnce = false;

    // ✅ MoldController가 요구하는 풀 주입/반환용
    private IObjectPool<FishCatchToken_st2> _pool;

    // 소유 몰드 추적
    public MoldController_st2 ownerMold;

    // =========================
    // ✅ 호환 API (컴파일 핵심)
    // =========================

    // MoldController.CreateFish()에서 호출됨
    public void SetPool(IObjectPool<FishCatchToken_st2> pool)
    {
        _pool = pool;
    }

    // MoldController.SpawnFishDelayed()에서 호출되는 7인자 버전
    public void Initialize(double popDspTime, Transform mold, MoldController_st2 owner,
                           float jumpHeight, float forwardDistance, float gravity, LayerMask groundLayer)
    {
        // 외부에서 들어온 점프 파라미터 우선 반영
        this.jumpHeight = jumpHeight;
        this.forwardDistance = forwardDistance;
        this.gravity = gravity;
        this.groundLayer = groundLayer;

        // 기존 3인자 Initialize로 공통 초기화
        Initialize(popDspTime, mold, owner);
    }

    // CatchInput에서 OnCaught(hand) 호출할 수 있게 오버로드 제공
    public void OnCaught(OVRInput.Controller hand)
    {
        assignedHand = hand;
        OnCaught(); // 기존 연출/고정 로직 재사용
    }

    // =========================
    // Pool Return
    // =========================

    public void ConsumeToPool(FishConsumeReason_st2 reason)
    {
        // ✅ 성공/미스/타임아웃 등 어떤 이유든 "정상 풀 반환"
        transform.SetParent(null, true);

        // 반환 직전 정리
        OnReturnToPool();

        if (ownerMold != null)
        {
            ownerMold.ReleaseFish(this);
            return;
        }

        if (_pool != null)
        {
            _pool.Release(this);
            return;
        }

        // fallback
        gameObject.SetActive(false);
    }

    public void ReturnToPool()
    {
        ConsumeToPool(FishConsumeReason_st2.Despawn);
    }

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

    // ✅ scale=100이어도 VFX가 튀지 않도록 TransformPoint 대신 "position + rotation * offset" 사용
    private void SpawnCatchVfx()
    {
        if (catchVfxPrefab == null) return;

        Transform a = catchVfxAnchor != null ? catchVfxAnchor : transform;

        Vector3 pos = a.position + (a.rotation * catchVfxLocalOffset); // 스케일 영향 없음
        Quaternion rot = a.rotation * Quaternion.Euler(catchVfxLocalEuler);

        var vfx = Instantiate(catchVfxPrefab, pos, rot);

        // 프리팹이 부모 스케일 가정일 경우(특히 파티클) 안전하게 1로 고정
        vfx.transform.localScale = Vector3.one;

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

    // ✅ 기존 3인자 Initialize(내부 공통)
    public void Initialize(double popDspTime, Transform mold, MoldController_st2 owner)
    {
        popTime = popDspTime;
        isResolved = false;
        isCaught = false;
        assignedHand = null;

        parentMold = mold;
        ownerMold = owner;

        hasJumped = false;
        catchFxPlayed = false;
        _releasedToPoolOnce = false;

        moldPosition = (mold != null) ? mold.position : transform.position;
        transform.position = moldPosition;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        // ⚠️ 기존 네 연출 유지(스케일 100)
        transform.localScale = Vector3.one * 100.0f;

        PerformJump();

        // 카메라 없을 수도 있으니 안전 처리
        if (Camera.main != null)
        {
            Transform playerCam = Camera.main.transform;
            Vector3 toPlayer = (playerCam.position - transform.position).normalized;
            transform.right = toPlayer;
        }
    }

    void PerformJump()
    {
        if (hasJumped) return;

        float jumpVelocityY = Mathf.Sqrt(2f * gravity * jumpHeight);
        float flightTime = 2f * jumpVelocityY / gravity;
        float forwardVelocityZ = forwardDistance / flightTime;

        Vector3 velocity = new Vector3(0, jumpVelocityY, -forwardVelocityZ);

        if (rb != null)
            rb.velocity = velocity;

        // ⚠️ 주의: 이건 "전역 중력" 변경이라 원하면 GameFlow 시작 때 1회만 세팅하는 게 더 안전함.
        Physics.gravity = new Vector3(0, -gravity, 0);

        hasJumped = true;
    }

    public void UpdateMovement()
    {
        // Rigidbody가 자동으로 물리 처리
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isCaught) return;

        bool isGround = false;
        if (((1 << collision.gameObject.layer) & groundLayer) != 0) isGround = true;
        if (collision.gameObject.CompareTag("Ground")) isGround = true;
        if (!isGround) return;

        if (ownerMold != null)
            ownerMold.ReleaseFish(this);
    }

    public void OnCaught()
    {
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
        catchFxPlayed = false;
        _releasedToPoolOnce = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryReleaseOnGround(other.gameObject);
    }

    private void TryReleaseOnGround(GameObject otherGo)
    {
        if (_releasedToPoolOnce) return;
        if (otherGo == null) return;

        if (isCaught) return;

        bool isGround =
            ((groundLayer.value & (1 << otherGo.layer)) != 0) ||
            otherGo.CompareTag("Ground");

        if (!isGround) return;

        _releasedToPoolOnce = true;

        if (ownerMold != null)
        {
            ownerMold.ReleaseFish(this);
        }
        else if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

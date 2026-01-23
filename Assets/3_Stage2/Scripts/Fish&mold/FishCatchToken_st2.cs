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

    // 소유 몰드 추적
    public MoldController_st2 ownerMold;

    public void ConsumeToPool(FishConsumeReason_st2 reason)
    {
        // ✅ 성공/미스/타임아웃 등 어떤 이유든 "정상 풀 반환"
        if (ownerMold != null)
        {
            // 손에 붙어있을 수 있으니 분리
            transform.SetParent(null, true);
            ownerMold.ReleaseFish(this);
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
        _releasedToPoolOnce = false; // ✅ 풀 재사용 시 반드시 리셋

        moldPosition = mold.position;
        transform.position = moldPosition;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.isKinematic = false;

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
        if (isCaught) return;

        bool isGround = false;
        if (((1 << collision.gameObject.layer) & groundLayer) != 0) isGround = true;
        if (collision.gameObject.CompareTag("Ground")) isGround = true;
        if (!isGround) return;

        if (!isResolved)
        {
            
        }

        if (ownerMold != null)
            ownerMold.ReleaseFish(this);
    }

    public void OnCaught()
    {
        // ✅ isCaught가 이미 true여도(외부에서 먼저 세팅했어도) FX는 1회 보장
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
        _releasedToPoolOnce = false; // ✅ 안전 리셋
        // isResolved/isCaught는 MoldController actionOnRelease에서 OnReturnToPool 호출 후 SetActive(false)라
        // 다음 Get() 때 Initialize에서 다시 세팅됨
    }

    private void OnTriggerEnter(Collider other)
    {
        TryReleaseOnGround(other.gameObject);
    }

    private void TryReleaseOnGround(GameObject otherGo)
    {
        if (_releasedToPoolOnce) return;
        if (otherGo == null) return;

        // 이미 잡혀서 처리 중이면 바닥으로 반환 막기(성공 연출/스냅 중 충돌 방지)
        if (isCaught) return;

        bool isGround =
            ((groundLayer.value & (1 << otherGo.layer)) != 0) ||
            otherGo.CompareTag("Ground");

        if (!isGround) return;

        _releasedToPoolOnce = true;

        // ✅ 여기서 isResolved 여부와 상관없이 "풀 반환"이 목표
        if (ownerMold != null)
        {
            ownerMold.ReleaseFish(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

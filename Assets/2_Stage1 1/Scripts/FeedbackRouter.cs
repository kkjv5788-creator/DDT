using UnityEngine;

public class FeedbackRouter : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public KnifeSlicer knifeSlicer; // (프로젝트에서 참조 걸려있을 수 있어 유지)
    public AudioSource sfxSource;
    public FeedbackSetSO feedbackSet;
    public PlateController plateController;

    [Header("VFX Spawn Override")]
    [Tooltip("체크하면, Slice 성공 VFX를 hitPos 대신 지정한 위치에서 스폰합니다.")]
    public bool overrideSliceSuccessVfxSpawn = false;

    [Tooltip("Slice 성공 VFX 스폰 위치(Transform). 비어있으면 이 오브젝트(transform)를 사용.")]
    public Transform sliceSuccessVfxSpawnPoint;

    [Tooltip("체크하면, Result(성공/실패) VFX를 transform.position 대신 지정한 위치에서 스폰합니다.")]
    public bool overrideResultVfxSpawn = false;

    [Tooltip("Result 성공 VFX 스폰 위치(Transform). 비어있으면 이 오브젝트(transform)를 사용.")]
    public Transform resultSuccessVfxSpawnPoint;

    [Tooltip("Result 실패 VFX 스폰 위치(Transform). 비어있으면 이 오브젝트(transform)를 사용.")]
    public Transform resultFailVfxSpawnPoint;

    float _lastFailFeedbackTime = -999f;

    void ResolveSpawn(Transform overridePoint, out Vector3 pos, out Quaternion rot)
    {
        Transform t = overridePoint ? overridePoint : transform;
        pos = t.position;
        rot = t.rotation;
    }

    void OnEnable()
    {
        // RhythmConductor가 UnityEvent로 발행하는 이벤트를 구독
        if (conductor)
        {
            conductor.OnSliceSuccess.AddListener(HandleSliceSuccess);
            conductor.OnSliceFail.AddListener(HandleSliceFail);
            conductor.OnRoundResult.AddListener(HandleRoundResult);
            conductor.OnWrongCut.AddListener(HandleWrongCut);
        }
    }

    void OnDisable()
    {
        if (conductor)
        {
            conductor.OnSliceSuccess.RemoveListener(HandleSliceSuccess);
            conductor.OnSliceFail.RemoveListener(HandleSliceFail);
            conductor.OnRoundResult.RemoveListener(HandleRoundResult);
            conductor.OnWrongCut.RemoveListener(HandleWrongCut);
        }
    }

    void HandleSliceSuccess(Vector3 hitPos, Vector3 hitNormal, float knifeSpeed)
    {
        if (!feedbackSet) return;

        // VFX
        if (feedbackSet.vfxSliceSuccessPrefab)
        {
            Vector3 spawnPos = hitPos;
            Quaternion spawnRot = Quaternion.LookRotation(hitNormal);

            if (overrideSliceSuccessVfxSpawn)
            {
                ResolveSpawn(sliceSuccessVfxSpawnPoint, out spawnPos, out spawnRot);
            }

            var vfx = Instantiate(feedbackSet.vfxSliceSuccessPrefab, spawnPos, spawnRot);
            Destroy(vfx, 2f);
        }

        // 플레이팅 조각 추가
        if (plateController)
            plateController.AddPlatingPiece();
    }

    void HandleSliceFail(Vector3 hitPos, string reason)
    {
        if (!feedbackSet) return;

        // Spam 방지 (FeedbackSetSO의 failCooldown 사용)
        if (Time.time - _lastFailFeedbackTime < feedbackSet.failCooldown)
            return;
        _lastFailFeedbackTime = Time.time;

        // SFX
        if (feedbackSet.sfxSliceFail && sfxSource)
            sfxSource.PlayOneShot(feedbackSet.sfxSliceFail);

        // VFX (실패는 기존처럼 hitPos에 유지)
        if (feedbackSet.vfxSliceFailPrefab)
        {
            var vfx = Instantiate(feedbackSet.vfxSliceFailPrefab, hitPos, Quaternion.identity);
            Destroy(vfx, 2f);
        }
    }

    void HandleRoundResult(bool success)
    {
        if (!feedbackSet) return;

        if (success)
        {
            // Success SFX
            if (feedbackSet.sfxResultSuccess && sfxSource)
                sfxSource.PlayOneShot(feedbackSet.sfxResultSuccess);

            // Success VFX
            if (feedbackSet.vfxResultSuccessPrefab)
            {
                Vector3 spawnPos = transform.position;
                Quaternion spawnRot = Quaternion.identity;

                if (overrideResultVfxSpawn)
                    ResolveSpawn(resultSuccessVfxSpawnPoint, out spawnPos, out spawnRot);

                var vfx = Instantiate(feedbackSet.vfxResultSuccessPrefab, spawnPos, spawnRot);
                Destroy(vfx, 3f);
            }

            // 접시 교체
            if (plateController)
                plateController.ShowSuccessPlate();
        }
        else
        {
            // Fail SFX
            if (feedbackSet.sfxResultFail && sfxSource)
                sfxSource.PlayOneShot(feedbackSet.sfxResultFail);

            // Fail VFX
            if (feedbackSet.vfxResultFailPrefab)
            {
                Vector3 spawnPos = transform.position;
                Quaternion spawnRot = Quaternion.identity;

                if (overrideResultVfxSpawn)
                    ResolveSpawn(resultFailVfxSpawnPoint, out spawnPos, out spawnRot);

                var vfx = Instantiate(feedbackSet.vfxResultFailPrefab, spawnPos, spawnRot);
                Destroy(vfx, 3f);
            }

            // 접시 교체
            if (plateController)
                plateController.ShowFailPlate();
        }
    }

    void HandleWrongCut(Vector3 hitPos)
    {
        if (!feedbackSet) return;

        // SFX
        if (feedbackSet.sfxWrongCut && sfxSource)
            sfxSource.PlayOneShot(feedbackSet.sfxWrongCut);

        // VFX
        if (feedbackSet.vfxWrongCutPrefab)
        {
            var vfx = Instantiate(feedbackSet.vfxWrongCutPrefab, hitPos, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        // 짧은 약진동
        XRHaptics.SendHaptic(true, 0.2f, 0.08f);
    }
}

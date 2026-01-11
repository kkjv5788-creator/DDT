using UnityEngine;

public class FeedbackRouter : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public KnifeSlicer knifeSlicer;
    public AudioSource sfxSource;
    public FeedbackSetSO feedbackSet;
    public PlateController plateController;

    float _lastFailFeedbackTime = -999f;

    void OnEnable()
    {
        // 이벤트 구독 (기존 스크립트에서 발행해야 함)
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

        // SFX (기존 KnifeSlicer에서도 재생하지만 중복 방지 가능)
        // if (feedbackSet.sfxSliceSuccess && sfxSource)
        //     sfxSource.PlayOneShot(feedbackSet.sfxSliceSuccess);

        // VFX
        if (feedbackSet.vfxSliceSuccessPrefab)
        {
            var vfx = Instantiate(feedbackSet.vfxSliceSuccessPrefab, hitPos, Quaternion.LookRotation(hitNormal));
            Destroy(vfx, 2f);
        }

        // 플레이팅 조각 추가
        if (plateController)
            plateController.AddPlatingPiece();
    }

    void HandleSliceFail(Vector3 hitPos, string reason)
    {
        if (!feedbackSet) return;

        // Spam 방지
        if (Time.time - _lastFailFeedbackTime < feedbackSet.failCooldown)
            return;
        _lastFailFeedbackTime = Time.time;

        // SFX
        if (feedbackSet.sfxSliceFail && sfxSource)
            sfxSource.PlayOneShot(feedbackSet.sfxSliceFail);

        // VFX
        if (feedbackSet.vfxSliceFailPrefab)
        {
            var vfx = Instantiate(feedbackSet.vfxSliceFailPrefab, hitPos, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        // 약한 햅틱 (선택)
        XRHaptics.SendHaptic(true, 0.15f, 0.05f);
    }

    void HandleRoundResult(bool success)
    {
        if (!feedbackSet) return;

        if (success)
        {
            // Success SFX/VFX
            if (feedbackSet.sfxResultSuccess && sfxSource)
                sfxSource.PlayOneShot(feedbackSet.sfxResultSuccess);

            if (feedbackSet.vfxResultSuccessPrefab)
            {
                var vfx = Instantiate(feedbackSet.vfxResultSuccessPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            // 접시 교체
            if (plateController)
                plateController.ShowSuccessPlate();
        }
        else
        {
            // Fail SFX/VFX
            if (feedbackSet.sfxResultFail && sfxSource)
                sfxSource.PlayOneShot(feedbackSet.sfxResultFail);

            if (feedbackSet.vfxResultFailPrefab)
            {
                var vfx = Instantiate(feedbackSet.vfxResultFailPrefab, transform.position, Quaternion.identity);
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


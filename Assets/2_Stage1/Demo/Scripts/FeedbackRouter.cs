using UnityEngine;
using Project.Core;
using Project.Data;

namespace Project.Gameplay
{
    public class FeedbackRouter : MonoBehaviour
    {
        public AudioSource audioSource;
        public Transform vfxParent;

        [Header("Haptics")]
        public XRHaptics haptics;

        FeedbackSetSO set;
        float lastFailTime = -999f;

        void Awake()
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>();
            if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        }

        void OnEnable()
        {
            GameEvents.FeedbackSetChanged += OnFeedbackSetChanged;

            GameEvents.SliceSuccess += OnSliceSuccess;
            GameEvents.SliceFail += OnSliceFail;
            GameEvents.WrongCut += OnWrongCut;
            GameEvents.RoundResult += OnRoundResult;
        }

        void OnDisable()
        {
            GameEvents.FeedbackSetChanged -= OnFeedbackSetChanged;

            GameEvents.SliceSuccess -= OnSliceSuccess;
            GameEvents.SliceFail -= OnSliceFail;
            GameEvents.WrongCut -= OnWrongCut;
            GameEvents.RoundResult -= OnRoundResult;
        }

        void OnFeedbackSetChanged(FeedbackSetSO s)
        {
            set = s;
        }

        void OnSliceSuccess(Vector3 pos, Vector3 normal, float speed)
        {
            if (!set) return;

            // SFX
            if (set.sfxSliceSuccess) audioSource.PlayOneShot(set.sfxSliceSuccess);

            // VFX
            SpawnVfx(set.vfxSliceSuccessPrefab, pos, normal);

            // Haptics "Å¹ + ¶Ç°¢" (2-stage)
            if (haptics) haptics.PlayTwoStepImpact(speed);
        }

        void OnSliceFail(Vector3 pos, SliceFailReason reason)
        {
            if (!set) return;

            float cd = Mathf.Max(0.01f, set.failCooldown);
            if (Time.time - lastFailTime < cd) return;
            lastFailTime = Time.time;

            if (set.sfxSliceFail) audioSource.PlayOneShot(set.sfxSliceFail);
            SpawnVfx(set.vfxSliceFailPrefab, pos, Vector3.up);
            if (haptics) haptics.PlayWeakBuzz();
        }

        void OnWrongCut(Vector3 pos)
        {
            if (!set) return;

            if (set.sfxWrongCut) audioSource.PlayOneShot(set.sfxWrongCut);
            SpawnVfx(set.vfxWrongCutPrefab, pos, Vector3.up);
            if (haptics) haptics.PlayWarningTap();
        }

        void OnRoundResult(bool success)
        {
            if (!set) return;

            if (success)
            {
                if (set.sfxResultSuccess) audioSource.PlayOneShot(set.sfxResultSuccess);
                SpawnVfx(set.vfxResultSuccessPrefab, transform.position, Vector3.up);
            }
            else
            {
                if (set.sfxResultFail) audioSource.PlayOneShot(set.sfxResultFail);
                SpawnVfx(set.vfxResultFailPrefab, transform.position, Vector3.up);
            }
        }

        void SpawnVfx(GameObject prefab, Vector3 pos, Vector3 normal)
        {
            if (!prefab) return;
            Quaternion rot = (normal.sqrMagnitude > 0.0001f) ? Quaternion.LookRotation(normal) : Quaternion.identity;
            var go = Instantiate(prefab, pos, rot, vfxParent);
            Destroy(go, 2.5f);
        }
    }
}

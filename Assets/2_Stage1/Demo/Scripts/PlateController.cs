using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Data;

namespace Project.Gameplay.Plate
{
    public class PlateController : MonoBehaviour
    {
        [Header("Anchors")]
        public Transform plateAnchor;
        public Transform stackRootOverride; // optional: inside plate prefab

        [Header("Stack shape (disk-like)")]
        public float pieceRadius = 0.03f;
        public float pieceThickness = 0.015f;

        [Header("Jitter")]
        public float posJitterMul = 0.08f;
        public float yawJitterDeg = 10f;
        public float tiltJitterDeg = 6f;

        FeedbackSetSO set;

        GameObject currentPlateInstance;
        Transform stackRoot;

        readonly List<GameObject> spawnedPlatingPieces = new List<GameObject>();
        int maxPieces = 12;

        void OnEnable()
        {
            GameEvents.FeedbackSetChanged += OnFeedbackSetChanged;
            GameEvents.SliceSuccess += OnSliceSuccess;
        }

        void OnDisable()
        {
            GameEvents.FeedbackSetChanged -= OnFeedbackSetChanged;
            GameEvents.SliceSuccess -= OnSliceSuccess;
        }

        void OnFeedbackSetChanged(FeedbackSetSO s)
        {
            set = s;
            if (set) maxPieces = Mathf.Max(1, set.maxPlatingPiecesPerRound);
        }

        public void PrepareEmptyPlate()
        {
            ReplacePlate(set ? set.platePrefabEmptyStack : null);
            ResetStack();
        }

        public void ShowResultPlateSuccess()
        {
            ReplacePlate(set ? set.platePrefabSuccessNeat : null);
        }

        public void ShowResultPlateFail()
        {
            ReplacePlate(set ? set.platePrefabFailExplode : null);

            // If fail plate has an animator script with PlayFail(), call it.
            if (currentPlateInstance)
            {
                var p = currentPlateInstance.GetComponentInChildren<PlateFailExplodePlayer>();
                if (p) p.PlayFail();
            }
        }

        public void CleanupLoosePieces()
        {
            // optional: if any stray pieces exist
        }

        void ReplacePlate(GameObject prefab)
        {
            if (currentPlateInstance) Destroy(currentPlateInstance);
            currentPlateInstance = null;
            stackRoot = null;

            if (!prefab || !plateAnchor) return;

            currentPlateInstance = Instantiate(prefab, plateAnchor.position, plateAnchor.rotation);

            // find stack root
            if (stackRootOverride) stackRoot = stackRootOverride;
            else
            {
                var found = currentPlateInstance.transform.Find("StackRoot");
                stackRoot = found ? found : currentPlateInstance.transform;
            }
        }

        void ResetStack()
        {
            for (int i = 0; i < spawnedPlatingPieces.Count; i++)
                if (spawnedPlatingPieces[i]) Destroy(spawnedPlatingPieces[i]);

            spawnedPlatingPieces.Clear();
        }

        void OnSliceSuccess(Vector3 hitPos, Vector3 hitNormal, float knifeSpeed)
        {
            // Only stack on empty plate phase: if current plate is result plate, skip
            if (!currentPlateInstance) return;
            if (!set || !set.platingPiecePrefab) return;

            // Heuristic: only stack if empty plate prefab is active. (You can refine by tagging plate prefabs.)
            // Here: if StackRoot exists -> considered stackable.
            if (!stackRoot) return;

            if (spawnedPlatingPieces.Count >= maxPieces) return;

            var piece = Instantiate(set.platingPiecePrefab, stackRoot);
            ApplyStackPose(piece.transform, spawnedPlatingPieces.Count);

            spawnedPlatingPieces.Add(piece);
        }

        void ApplyStackPose(Transform t, int index)
        {
            bool isTop = index >= 8;
            int localIndex = isTop ? (index - 8) : index;

            int countInLayer = isTop ? 4 : 8;

            float ringRadius = isTop ? pieceRadius * 0.85f : pieceRadius * 1.35f;
            float y = isTop ? (pieceThickness * 0.85f) : 0f;

            float angle = (Mathf.PI * 2f) * (localIndex / (float)countInLayer);

            float x = Mathf.Cos(angle) * ringRadius;
            float z = Mathf.Sin(angle) * ringRadius;

            float inward = pieceRadius * (isTop ? 0.08f : 0.15f);
            x -= Mathf.Cos(angle) * inward;
            z -= Mathf.Sin(angle) * inward;

            float posJitter = pieceRadius * posJitterMul;
            x += Random.Range(-posJitter, posJitter);
            z += Random.Range(-posJitter, posJitter);

            t.localPosition = new Vector3(x, y, z);

            float yaw = Random.Range(-yawJitterDeg, yawJitterDeg);
            float tiltX = Random.Range(-tiltJitterDeg, tiltJitterDeg);
            float tiltZ = Random.Range(-tiltJitterDeg, tiltJitterDeg);
            t.localRotation = Quaternion.Euler(tiltX, yaw, tiltZ);
        }
    }

    /// <summary>
    /// 폭발 접시 프리팹 내부에 붙여두면, 외부에서 PlayFail()만 호출하면 됨.
    /// (Animator/Particle/Audio는 프리팹 내부에서 세팅)
    /// </summary>
    public class PlateFailExplodePlayer : MonoBehaviour
    {
        public Animator animator;
        public string triggerName = "Explode";
        public ParticleSystem vfx;
        public AudioSource sfx;

        public void PlayFail()
        {
            if (animator && !string.IsNullOrEmpty(triggerName))
                animator.SetTrigger(triggerName);
            if (vfx) vfx.Play(true);
            if (sfx) sfx.Play();
        }
    }
}

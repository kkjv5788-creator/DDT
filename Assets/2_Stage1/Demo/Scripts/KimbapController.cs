using UnityEngine;

namespace Project.Gameplay.Kimbap
{
    public class KimbapController : MonoBehaviour
    {
        [Header("Main piece")]
        public GameObject currentMain; // if null, use this.gameObject

        [Header("Right direction definition")]
        public Transform rightDirSource; // local +X direction provider (set to kimbap root or a child)

        [Header("RightThin thickness")]
        [Range(0.02f, 0.5f)] public float thinSliceThicknessNorm = 0.12f;
        public float minThinThicknessWorld = 0.01f;

        [Header("Thin piece handling")]
        public ThinPieceAutoCleanup thinPieceCleanupPrefab; // optional (if you want to enforce component)
        public float thinPieceLifeSeconds = 2.5f;
        public float thinDropSeconds = 0.2f;
        public Vector3 thinDropOffset = new Vector3(0.02f, -0.01f, 0.02f);

        void Awake()
        {
            if (!currentMain) currentMain = gameObject;
            if (!rightDirSource) rightDirSource = transform;
        }

        public void TrySliceRightThin(Vector3 hitPos, Vector3 hitNormal)
        {
#if EZYSLICE
            DoEzySlice();
#else
            // If EzySlice symbols not defined, just simulate by scaling main (fallback)
            SimulateFallback();
#endif
        }

#if EZYSLICE
        void DoEzySlice()
        {
            if (!currentMain) return;

            var mf = currentMain.GetComponentInChildren<MeshFilter>();
            if (!mf) return;

            // Estimate main bounds along rightDir
            Vector3 rightDir = rightDirSource.right.normalized;

            var rend = currentMain.GetComponentInChildren<Renderer>();
            if (!rend) return;

            Bounds b = rend.bounds;
            float mainLengthWorld = Vector3.Dot(b.size, new Vector3(Mathf.Abs(rightDir.x), Mathf.Abs(rightDir.y), Mathf.Abs(rightDir.z)));
            float thinWorld = Mathf.Max(mainLengthWorld * thinSliceThicknessNorm, minThinThicknessWorld);

            // Compute right end in world
            Vector3 center = b.center;
            Vector3 ext = b.extents;
            // approximate right end by projecting extents
            float projExt = Vector3.Dot(ext, new Vector3(Mathf.Abs(rightDir.x), Mathf.Abs(rightDir.y), Mathf.Abs(rightDir.z)));
            Vector3 rightEnd = center + rightDir * projExt;

            Vector3 planePoint = rightEnd - rightDir * thinWorld;
            Vector3 planeNormal = rightDir; // right-facing plane normal

            // Slice
            var hull = currentMain.Slice(planePoint, planeNormal);
            if (hull == null) return;

            // Create pieces
            GameObject upper = hull.CreateUpperHull(currentMain);
            GameObject lower = hull.CreateLowerHull(currentMain);

            if (!upper || !lower)
            {
                if (upper) Destroy(upper);
                if (lower) Destroy(lower);
                return;
            }

            // Decide which is thin (smaller bounds magnitude)
            float upperSize = GetSizeMetric(upper);
            float lowerSize = GetSizeMetric(lower);

            GameObject thin = (upperSize < lowerSize) ? upper : lower;
            GameObject big  = (thin == upper) ? lower : upper;

            // Replace main
            Destroy(currentMain);
            currentMain = big;
            currentMain.transform.SetParent(transform, true);

            // Thin piece small animation + cleanup
            StartCoroutine(ThinDropThenCleanup(thin));
        }

        float GetSizeMetric(GameObject go)
        {
            var r = go.GetComponentInChildren<Renderer>();
            if (!r) return 999f;
            var s = r.bounds.size;
            return s.x * s.y * s.z;
        }

        System.Collections.IEnumerator ThinDropThenCleanup(GameObject thin)
        {
            if (!thin) yield break;

            Vector3 start = thin.transform.position;
            Vector3 end = start + rightDirSource.right.normalized * thinDropOffset.x + Vector3.up * thinDropOffset.y + rightDirSource.forward.normalized * thinDropOffset.z;

            float t = 0f;
            while (t < thinDropSeconds)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / thinDropSeconds);
                thin.transform.position = Vector3.Lerp(start, end, a);
                yield return null;
            }

            var cleanup = thin.GetComponent<ThinPieceAutoCleanup>();
            if (!cleanup) cleanup = thin.AddComponent<ThinPieceAutoCleanup>();
            cleanup.lifeSeconds = thinPieceLifeSeconds;

            // NOTE: In real project you can pool instead of destroy
        }
#endif

        void SimulateFallback()
        {
            // fallback when EzySlice isn't present: just shrink main slightly and spawn a fake thin piece cube
            var rend = currentMain.GetComponentInChildren<Renderer>();
            if (!rend) return;

            currentMain.transform.localScale = currentMain.transform.localScale * 0.99f;

            GameObject thin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            thin.transform.position = rend.bounds.center + rightDirSource.right * 0.05f;
            thin.transform.localScale = Vector3.one * 0.02f;
            var col = thin.GetComponent<Collider>();
            if (col) Destroy(col);

            var cleanup = thin.AddComponent<ThinPieceAutoCleanup>();
            cleanup.lifeSeconds = thinPieceLifeSeconds;
        }
    }
}

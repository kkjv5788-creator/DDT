using System.Diagnostics;
using UnityEngine;

public class KimbapController : MonoBehaviour
{
    [Header("Refs")]
    public Collider sliceTrigger;     // Trigger collider (KimbapSliceable layer)
    public Collider blockCollider;    // Solid collider (KimbapBlocked layer)
    public Transform mainMeshRoot;    // Object to slice (has MeshFilter/MeshRenderer)
    public Material crossSectionMaterial;

    [Header("Runtime")]
    public bool sliceable;

    RhythmTriggerListSO.Trigger _trigger;

    // for RightThin slicing
    GameObject _currentMainGO;
    int _activeThinPieces;

    void Awake()
    {
        if (!mainMeshRoot) mainMeshRoot = transform;
        if (!sliceTrigger || !blockCollider)
            UnityEngine.Debug.LogWarning("[KimbapController] Assign sliceTrigger & blockCollider for best results.");

        // Default: blocked until Judging
        SetSliceable(false);

        _currentMainGO = mainMeshRoot.gameObject;
    }

    public void BeginTrigger(RhythmTriggerListSO.Trigger trigger)
    {
        _trigger = trigger;
        _activeThinPieces = 0;
        _currentMainGO = mainMeshRoot.gameObject;
        SetSliceable(false);
    }

    public void SetSliceable(bool value)
    {
        sliceable = value;

        if (sliceTrigger) sliceTrigger.enabled = value;
        if (blockCollider) blockCollider.enabled = !value;
    }

    public void OnTriggerResult(bool success)
    {
        // 안정적으로 접시 프리셋으로 귀결시키고 싶다면 여기서 교체/연출하면 됨
        // 지금은 디버깅 목적상 성공/실패 로그만
        UnityEngine.Debug.Log($"[KimbapController] Trigger result: {(success ? "SUCCESS" : "FAIL")}");
    }

    public bool CanSpawnThinPiece()
    {
        if (_trigger == null) return true;
        return _activeThinPieces < Mathf.Max(1, _trigger.maxActiveThinPieces);
    }

    public void NotifyThinPieceSpawned()
    {
        _activeThinPieces++;
    }

    public void NotifyThinPieceDespawned()
    {
        _activeThinPieces = Mathf.Max(0, _activeThinPieces - 1);
    }

    /// <summary>
    /// Execute one RightThin slice on current main piece.
    /// Must be called only on "valid slice" events.
    /// </summary>
    public bool ExecuteRightThinSlice(int sliceIndex0Based)
    {
#if !EZYSlice
        // If you don't define EZYSlice symbol, we still compile.
        UnityEngine.Debug.LogWarning("[KimbapController] EZYSlice not enabled. Add EzySlice and define scripting symbol EZYSlice.");
        return false;
#else
        if (_trigger == null)
        {
            Debug.LogWarning("[KimbapController] No trigger bound.");
            return false;
        }
        if (_currentMainGO == null)
        {
            Debug.LogWarning("[KimbapController] currentMain is null.");
            return false;
        }

        // Calculate thickness
        float t = _trigger.thinSliceThicknessNorm;
        if (_trigger.thinThicknessCurve != null && _trigger.thinThicknessCurve.length > 0 && _trigger.requiredSliceCount > 1)
        {
            float u = sliceIndex0Based / (float)(_trigger.requiredSliceCount - 1);
            float cu = _trigger.thinThicknessCurve.Evaluate(u);
            if (cu > 0f) t = cu;
        }

        var rend = _currentMainGO.GetComponentInChildren<Renderer>();
        if (!rend)
        {
            Debug.LogWarning("[KimbapController] Renderer not found on currentMain.");
            return false;
        }

        Bounds wb = rend.bounds;
        Vector3 right = transform.right.normalized; // +X = right
        float mainLenApprox = wb.size.x; // works best if Kimbap aligned; ok for prototype

        float thinWorld = Mathf.Max(mainLenApprox * t, _trigger.minThinThicknessWorld);

        Vector3 rightEnd = wb.center + right * (wb.extents.x);
        Vector3 planePoint = rightEnd - right * thinWorld;
        Vector3 planeNormal = right;

        // EzySlice
        // NOTE: EzySlice requires convex meshes for best results (per project note).
        // Ensure your Kimbap mesh is reasonably convex, or pre-approximate it.
        var mf = _currentMainGO.GetComponentInChildren<MeshFilter>();
        if (!mf)
        {
            Debug.LogWarning("[KimbapController] MeshFilter not found on currentMain.");
            return false;
        }

        // Slice call (EzySlice extension methods)
        var slicedHull = _currentMainGO.Slice(planePoint, planeNormal, crossSectionMaterial);
        if (slicedHull == null)
        {
            Debug.LogWarning("[KimbapController] Slice returned null (failed).");
            return false;
        }

        GameObject upper = slicedHull.CreateUpperHull(_currentMainGO, crossSectionMaterial);
        GameObject lower = slicedHull.CreateLowerHull(_currentMainGO, crossSectionMaterial);

        if (!upper || !lower)
        {
            Debug.LogWarning("[KimbapController] Hull create failed.");
            if (upper) Destroy(upper);
            if (lower) Destroy(lower);
            return false;
        }

        // Decide which is big/small by bounds volume
        float vu = BoundsVolume(upper);
        float vl = BoundsVolume(lower);

        GameObject big = (vu >= vl) ? upper : lower;
        GameObject small = (big == upper) ? lower : upper;

        // Replace currentMain: keep big
        // Place them at same transform as currentMain for consistency
        big.transform.SetPositionAndRotation(_currentMainGO.transform.position, _currentMainGO.transform.rotation);
        small.transform.SetPositionAndRotation(_currentMainGO.transform.position, _currentMainGO.transform.rotation);

        // Cleanup old
        Destroy(_currentMainGO);
        _currentMainGO = big;

        // Setup thin piece (small)
        if (CanSpawnThinPiece())
        {
            NotifyThinPieceSpawned();
            var tp = small.AddComponent<ThinPieceAutoCleanup>();
            tp.owner = this;
            tp.flyDirection = right;
            tp.flySeconds = 0.22f;
            tp.lifeSeconds = 1.2f;
        }
        else
        {
            Destroy(small);
        }

        return true;
#endif
    }

    float BoundsVolume(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (!r) return 0f;
        var b = r.bounds;
        return b.size.x * b.size.y * b.size.z;
    }
}

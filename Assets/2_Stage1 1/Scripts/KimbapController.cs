using System.Diagnostics;
using UnityEngine;

#if EZYSlice
using EzySlice;
#endif

public class KimbapController : MonoBehaviour
{
    public enum AxisMode
    {
        WorldX,             // 월드 +X (사진 기준 동쪽을 이걸로 쓰는 경우가 많음)
        WorldZ,             // 월드 +Z
        ReferenceRight,     // axisReference.right
        ReferenceForward    // axisReference.forward
    }

    [Header("Thin Piece Timing")]
    public float thinFlySeconds = 0.22f;
    public float thinLifeSeconds = 1.2f;

    [Header("Refs")]
    public Collider sliceTrigger;     // Trigger collider (KimbapSliceable layer)
    public Collider blockCollider;    // Solid collider (KimbapBlocked layer)
    public Transform mainMeshRoot;    // Mesh-only object (없으면 런타임에 자동 생성/분리)
    public Material crossSectionMaterial;

    [Header("Slice Direction")]
    public AxisMode axisMode = AxisMode.WorldX;
    public Transform axisReference;   // 테이블 같은 기준 Transform (Reference 모드에서 사용)
    public bool invertAxis = false;

    [Header("Debug")]
    public bool debugDrawPlane = true;
    public float debugDrawSeconds = 0.25f;

    [Header("Runtime")]
    public bool sliceable;

    RhythmTriggerListSO.Trigger _trigger;

    // current mesh GO (항상 "메쉬 전용" 오브젝트를 가리키게 유지)
    GameObject _currentMeshGO;

    int _activeThinPieces;

    void Awake()
    {
        // 기본은 막힘
        SetSliceable(false);

        // 루트에 Mesh가 붙어있으면 런타임에 메쉬 전용 자식으로 분리
        EnsureMeshRoot();

        _currentMeshGO = FindMeshGameObject(mainMeshRoot);
        if (_currentMeshGO == null)
            UnityEngine.Debug.LogWarning("[KimbapController] MeshFilter not found under mainMeshRoot.");
    }

    public void BeginTrigger(RhythmTriggerListSO.Trigger trigger)
    {
        _trigger = trigger;
        _activeThinPieces = 0;

        EnsureMeshRoot();
        _currentMeshGO = FindMeshGameObject(mainMeshRoot);
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
        UnityEngine.Debug.Log($"[KimbapController] Trigger result: {(success ? "SUCCESS" : "FAIL")}");
    }

    public bool CanSpawnThinPiece()
    {
        if (_trigger == null) return true;
        return _activeThinPieces < Mathf.Max(1, _trigger.maxActiveThinPieces);
    }

    public void NotifyThinPieceSpawned() => _activeThinPieces++;
    public void NotifyThinPieceDespawned() => _activeThinPieces = Mathf.Max(0, _activeThinPieces - 1);

    Vector3 GetCutAxis()
    {
        Vector3 axis = Vector3.right;

        switch (axisMode)
        {
            case AxisMode.WorldX: axis = Vector3.right; break;
            case AxisMode.WorldZ: axis = Vector3.forward; break;
            case AxisMode.ReferenceRight:
                axis = axisReference ? axisReference.right : Vector3.right;
                break;
            case AxisMode.ReferenceForward:
                axis = axisReference ? axisReference.forward : Vector3.forward;
                break;
        }

        axis = axis.normalized;
        if (invertAxis) axis = -axis;
        return axis;
    }

    /// <summary>
    /// 사진 기준 "동쪽(=Axis)"에서 얇게 한 장씩 떼어내는 절단.
    /// sliceIndex0Based는 thin thickness curve용.
    /// </summary>
    public bool ExecuteRightThinSlice(int sliceIndex0Based)
    {
#if !EZYSlice
        UnityEngine.Debug.LogWarning("[KimbapController] EZYSlice not enabled. Add EzySlice and define scripting symbol EZYSlice.");
        return false;
#else
        if (_trigger == null)
        {
            UnityEngine.Debug.LogWarning("[KimbapController] No trigger bound.");
            return false;
        }

        EnsureMeshRoot();

        if (_currentMeshGO == null)
        {
            _currentMeshGO = FindMeshGameObject(mainMeshRoot);
            if (_currentMeshGO == null)
            {
                UnityEngine.Debug.LogWarning("[KimbapController] Mesh object not found.");
                return false;
            }
        }

        var rend = _currentMeshGO.GetComponentInChildren<Renderer>();
        if (!rend)
        {
            UnityEngine.Debug.LogWarning("[KimbapController] Renderer not found on mesh.");
            return false;
        }

        // 절단 축(사진 기준 동쪽)
        Vector3 axis = GetCutAxis();

        // thickness 계산
        float tNorm = _trigger.thinSliceThicknessNorm;
        if (_trigger.thinThicknessCurve != null && _trigger.thinThicknessCurve.length > 0 && _trigger.requiredSliceCount > 1)
        {
            float u = sliceIndex0Based / (float)(_trigger.requiredSliceCount - 1);
            float cu = _trigger.thinThicknessCurve.Evaluate(u);
            if (cu > 0f) tNorm = cu;
        }

        Bounds wb = rend.bounds;

        // bounds corners를 axis에 투영해서 길이 측정
        GetMinMaxAlongDir(wb, axis, out float minD, out float maxD);
        float lengthAlongAxis = Mathf.Max(0.0001f, maxD - minD);

        float thinWorld = Mathf.Max(lengthAlongAxis * tNorm, _trigger.minThinThicknessWorld);

        // "동쪽 끝" (axis 방향의 max 쪽)
        // AABB는 axis 방향으로 center 기준 대칭이므로 아래 방식이 안정적
        Vector3 axisEnd = wb.center + axis * (lengthAlongAxis * 0.5f);

        // axisEnd에서 thinWorld만큼 안쪽으로 들어온 지점에 절단 평면 생성
        Vector3 planePoint = axisEnd - axis * thinWorld;
        Vector3 planeNormal = axis; // normal이 axis 쪽을 향하면 axisEnd 쪽 조각이 얇게 떨어짐

        if (debugDrawPlane)
        {
           UnityEngine.Debug.DrawRay(planePoint, planeNormal * 0.2f, Color.green, debugDrawSeconds);
            UnityEngine.Debug.DrawRay(planePoint, -planeNormal * 0.2f, Color.red, debugDrawSeconds);
        }

        // 슬라이스는 "메쉬 전용 오브젝트"만 대상으로!
        // (루트는 절대 Destroy 하면 안 됨)
        Transform meshT = _currentMeshGO.transform;

        // 루트 기준 로컬 포즈 저장(슬라이스 후에도 동일 포즈 유지)
        Transform root = transform;
        Vector3 localPos = root.InverseTransformPoint(meshT.position);
        Quaternion localRot = Quaternion.Inverse(root.rotation) * meshT.rotation;
        Vector3 localScale = meshT.localScale;

        // 실제 슬라이스
        var hull = _currentMeshGO.Slice(planePoint, planeNormal, crossSectionMaterial);
        if (hull == null)
        {
            UnityEngine.Debug.LogWarning("[KimbapController] Slice returned null (failed). Try enabling Read/Write on mesh import, or adjust axis.");
            return false;
        }

        GameObject upper = hull.CreateUpperHull(_currentMeshGO, crossSectionMaterial);
        GameObject lower = hull.CreateLowerHull(_currentMeshGO, crossSectionMaterial);

        if (!upper || !lower)
        {
            if (upper) Destroy(upper);
            if (lower) Destroy(lower);
            UnityEngine.Debug.LogWarning("[KimbapController] Hull create failed.");
            return false;
        }

        // big/small 선택(더 큰 쪽이 본체)
        float vu = BoundsVolume(upper);
        float vl = BoundsVolume(lower);
        GameObject big = (vu >= vl) ? upper : lower;
        GameObject small = (big == upper) ? lower : upper;

        // 기존 메쉬 오브젝트 제거
        Destroy(_currentMeshGO);

        // big/small을 루트 아래로 붙이고 로컬 포즈 복원
        big.name = "MainMeshRuntime";
        big.transform.SetParent(root, false);
        big.transform.localPosition = localPos;
        big.transform.localRotation = localRot;
        big.transform.localScale = localScale;

        small.transform.SetParent(root, false);
        small.transform.localPosition = localPos;
        small.transform.localRotation = localRot;
        small.transform.localScale = localScale;

        // 현재 메쉬 갱신
        mainMeshRoot = big.transform;
        _currentMeshGO = big;

        // 얇은 조각 연출
        if (CanSpawnThinPiece())
        {
            NotifyThinPieceSpawned();

            var tp = small.AddComponent<ThinPieceAutoCleanup>();
            tp.owner = this;
            tp.flyDirection = axis;     // 동쪽으로 살짝 튕기기
            tp.flySeconds = 0.22f;
            tp.lifeSeconds = 1.2f;

            foreach (var c in small.GetComponentsInChildren<Collider>())
                c.enabled = false;
        }
        else
        {
            Destroy(small);
        }

        return true;
#endif
    }

    // --- 핵심: 루트에 Mesh가 붙어있으면 메쉬만 자식으로 분리해서 "루트 파괴" 방지 ---
    void EnsureMeshRoot()
    {
        if (mainMeshRoot && FindMeshGameObject(mainMeshRoot)) return;

        // 루트에 Mesh가 붙어 있으면 런타임 복제해서 자식으로 분리
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        if (mf && mr)
        {
            var go = new GameObject("MainMeshRuntime");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var newMf = go.AddComponent<MeshFilter>();
            newMf.sharedMesh = mf.sharedMesh;

            var newMr = go.AddComponent<MeshRenderer>();
            newMr.sharedMaterials = mr.sharedMaterials;

            // 루트 MeshRenderer는 꺼두기(콜라이더/스크립트는 유지)
            mr.enabled = false;
            mf.mesh = null;

            mainMeshRoot = go.transform;
            _currentMeshGO = go;
            return;
        }

        // 루트에 Mesh가 없다면(이미 자식 구조일 수도)
        // mainMeshRoot를 루트로 두고 검색
        if (!mainMeshRoot) mainMeshRoot = transform;
    }

    static GameObject FindMeshGameObject(Transform root)
    {
        if (!root) return null;
        var mfSelf = root.GetComponent<MeshFilter>();
        if (mfSelf) return mfSelf.gameObject;
        var mf = root.GetComponentInChildren<MeshFilter>();
        return mf ? mf.gameObject : null;
    }

    static float BoundsVolume(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (!r) return 0f;
        var b = r.bounds;
        return b.size.x * b.size.y * b.size.z;
    }

    static void GetMinMaxAlongDir(Bounds b, Vector3 dir, out float min, out float max)
    {
        dir.Normalize();
        Vector3 c = b.center;
        Vector3 e = b.extents;

        Vector3[] corners = new Vector3[8]
        {
            c + new Vector3( e.x,  e.y,  e.z),
            c + new Vector3( e.x,  e.y, -e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3(-e.x,  e.y,  e.z),
            c + new Vector3(-e.x,  e.y, -e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x, -e.y, -e.z),
        };

        min = float.PositiveInfinity;
        max = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            float d = Vector3.Dot(corners[i], dir);
            if (d < min) min = d;
            if (d > max) max = d;
        }
    }
}

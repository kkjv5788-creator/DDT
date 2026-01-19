using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class PlateController : MonoBehaviour
{
    [Header("Refs")]
    public FeedbackSetSO feedbackSet;
    public Transform plateSpawnPoint;

    [Header("Stack Layout (12 pieces: 8+4)")]
    public float pieceRadius = 0.04f;
    public float pieceThickness = 0.015f;
    public float ringRadius0 = 0.054f;  // pieceRadius * 1.35
    public float ringRadius1 = 0.034f;  // pieceRadius * 0.85
    public float layerHeight = 0.01275f; // pieceThickness * 0.85
    public float posJitter = 0.0032f;   // pieceRadius * 0.08
    public float yawJitter = 10f;       // degrees
    public float tiltJitter = 6f;       // degrees

    [Header("Piece Rotation Offset")]
    public Vector3 pieceFlatEulerOffset = new Vector3(90f, 0f, 0f);

    [Header("Pile Placement (Slicing Only)")]
    [Tooltip("젓징(슬라이스 성공) 중 생성되는 조각 배치만 '무더기(사진 느낌)'로 바꿉니다. 기존 8+4 원형 배치는 아래 옵션을 끄면 그대로 유지됩니다.")]
    public bool usePilePlacementForSlicingPieces = false;

    [Tooltip("빈 접시 프리팹(_currentPlate) 하위에 이 이름의 Transform을 만들어두면 그 지점이 무더기 중심이 됩니다.")]
    public string platingAnchorName = "PlatingAnchor";

    [Tooltip("무더기가 퍼질 수 있는 최대 반경(미터)")]
    public float pileMaxRadius = 0.12f;
    [Tooltip("반경 증가 속도(값이 클수록 더 빨리 퍼짐)")]
    public float pileTightness = 0.035f;
    [Tooltip("X/Z 미세 흔들림(미터)")]
    public float pilePosJitter = 0.006f;

    [Tooltip("조각마다 쌓이는 Y 증가(사진처럼 낮게면 0~0.001 추천)")]
    public float pileYStep = 0.001f;
    [Tooltip("Y 미세 흔들림(미터)")]
    public float pileYJitter = 0.001f;
    [Tooltip("프리팹 피벗이 바닥이 아니라 떠 보이면 -값으로 보정")]
    public float pileBottomOffsetY = 0.0f;

    [Tooltip("Yaw(수평 회전) 랜덤 범위(도)")]
    public float pileYawJitter = 180f;
    [Tooltip("기울기(도)")]
    public float pileTiltJitter = 5f;

    // ✅ 추가: 스폰포인트 회전을 기준으로 쌓이는 방향을 '직접' 보정할 수 있는 오프셋
    [Header("Pile Orientation Override")]
    [Tooltip("PlatingAnchor/plateSpawnPoint의 회전에 이 오프셋을 더해 '쌓이는 방향(원형 평면)'을 보정합니다.")]
    public Vector3 pileBaseRotationOffsetEuler = Vector3.zero;

    GameObject _currentPlate;
    List<GameObject> _platingPieces = new List<GameObject>();
    int _stackCount = 0;

    static readonly Vector2[] _layer0Offsets = new Vector2[8]
    {
        new Vector2(1f, 0f),
        new Vector2(0.707f, 0.707f),
        new Vector2(0f, 1f),
        new Vector2(-0.707f, 0.707f),
        new Vector2(-1f, 0f),
        new Vector2(-0.707f, -0.707f),
        new Vector2(0f, -1f),
        new Vector2(0.707f, -0.707f)
    };

    static readonly Vector2[] _layer1Offsets = new Vector2[4]
    {
        new Vector2(0.707f, 0f),
        new Vector2(0f, 0.707f),
        new Vector2(-0.707f, 0f),
        new Vector2(0f, -0.707f)
    };

    void Start()
    {
        ResetToEmptyPlate();
    }

    Transform FindPlatingAnchorInCurrentPlate()
    {
        if (_currentPlate == null) return null;
        return _currentPlate.transform.Find(platingAnchorName);
    }

    // ✅ 수정: baseRot에 스폰포인트 회전 + 오프셋을 확실히 반영
    void GetPileBase(out Vector3 basePos, out Quaternion baseRot)
    {
        Transform anchor = FindPlatingAnchorInCurrentPlate();
        Transform refT = anchor != null ? anchor : (plateSpawnPoint != null ? plateSpawnPoint : transform);

        basePos = refT.position;

        // 스폰포인트 rotation을 기반으로, 인스펙터 오프셋으로 보정 가능하게
        baseRot = refT.rotation * Quaternion.Euler(pileBaseRotationOffsetEuler);
    }

    Vector3 ComputePileWorldPos(int index, Vector3 basePos, Quaternion baseRot)
    {
        var prevState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(1000 + index * 17);

        const float goldenAngle = 2.39996323f; // radians
        float i = index;
        float r = Mathf.Min(pileMaxRadius, pileTightness * Mathf.Sqrt(i));
        float a = i * goldenAngle;

        Vector2 j = UnityEngine.Random.insideUnitCircle * pilePosJitter;
        float y = (index * pileYStep) + UnityEngine.Random.Range(-pileYJitter, pileYJitter) + pileBottomOffsetY;

        UnityEngine.Random.state = prevState;

        Vector3 localOffset = new Vector3(Mathf.Cos(a) * r + j.x, y, Mathf.Sin(a) * r + j.y);
        return basePos + baseRot * localOffset;
    }

    Quaternion ComputePileWorldRot(int index, Quaternion baseRot)
    {
        var prevState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(2000 + index * 31);

        float yaw = UnityEngine.Random.Range(-pileYawJitter, pileYawJitter);
        float tx = UnityEngine.Random.Range(-pileTiltJitter, pileTiltJitter);
        float tz = UnityEngine.Random.Range(-pileTiltJitter, pileTiltJitter);

        UnityEngine.Random.state = prevState;

        return baseRot * Quaternion.Euler(tx, yaw, tz);
    }

    public void ResetToEmptyPlate()
    {
        UnityEngine.Debug.Log("[PlateController] ResetToEmptyPlate called");

        ClearCurrentPlate();
        ClearPlatingPieces();

        if (feedbackSet && feedbackSet.platePrefabEmptyStack)
        {
            Vector3 pos = plateSpawnPoint ? plateSpawnPoint.position : transform.position;
            Quaternion rot = plateSpawnPoint ? plateSpawnPoint.rotation : transform.rotation;
            _currentPlate = Instantiate(feedbackSet.platePrefabEmptyStack, pos, rot);
        }
        else
        {
            UnityEngine.Debug.LogWarning("[PlateController] feedbackSet or empty plate prefab missing");
        }
    }

    public void ShowSuccessPlate()
    {
        UnityEngine.Debug.Log("[PlateController] ShowSuccessPlate called");

        ClearCurrentPlate();
        ClearPlatingPieces();

        if (feedbackSet && feedbackSet.platePrefabSuccessNeat)
        {
            Vector3 pos = plateSpawnPoint ? plateSpawnPoint.position : transform.position;
            Quaternion rot = plateSpawnPoint ? plateSpawnPoint.rotation : transform.rotation;
            _currentPlate = Instantiate(feedbackSet.platePrefabSuccessNeat, pos, rot);
        }
        else
        {
            UnityEngine.Debug.LogWarning("[PlateController] feedbackSet or success plate prefab missing");
        }
    }

    public void ShowFailPlate()
    {
        UnityEngine.Debug.Log("[PlateController] ShowFailPlate called");

        ClearCurrentPlate();
        ClearPlatingPieces();

        if (feedbackSet && feedbackSet.platePrefabFailExplode)
        {
            Vector3 pos = plateSpawnPoint ? plateSpawnPoint.position : transform.position;
            Quaternion rot = plateSpawnPoint ? plateSpawnPoint.rotation : transform.rotation;
            _currentPlate = Instantiate(feedbackSet.platePrefabFailExplode, pos, rot);

            var anim = _currentPlate.GetComponent<Animator>();
            if (anim) anim.SetTrigger("Explode");
        }
        else
        {
            UnityEngine.Debug.LogWarning("[PlateController] feedbackSet or fail plate prefab missing");
        }
    }

    void ClearCurrentPlate()
    {
        if (_currentPlate)
        {
            Destroy(_currentPlate);
            _currentPlate = null;
        }
    }

    public void AddPlatingPiece()
    {
        if (!feedbackSet || !feedbackSet.platingPiecePrefab)
        {
            UnityEngine.Debug.LogWarning("[PlateController] feedbackSet or platingPiecePrefab missing");
            return;
        }

        if (_stackCount >= feedbackSet.maxPlatingPiecesPerRound)
        {
            UnityEngine.Debug.LogWarning($"[PlateController] Stack full! {_stackCount}/{feedbackSet.maxPlatingPiecesPerRound}");
            return;
        }

        Quaternion flatOffset = Quaternion.Euler(pieceFlatEulerOffset);

        if (usePilePlacementForSlicingPieces)
        {
            GetPileBase(out Vector3 pileBasePos, out Quaternion pileBaseRot);

            Vector3 pileWorldPos = ComputePileWorldPos(_stackCount, pileBasePos, pileBaseRot);

            // ✅ 수평 눕힘은 그대로 적용
            Quaternion pileWorldRot = ComputePileWorldRot(_stackCount, pileBaseRot) * flatOffset;

            GameObject piecePile = Instantiate(feedbackSet.platingPiecePrefab, pileWorldPos, pileWorldRot);
            if (_currentPlate) piecePile.transform.SetParent(_currentPlate.transform, true);

            _platingPieces.Add(piecePile);
            _stackCount++;
            return;
        }

        Vector3 basePos = plateSpawnPoint ? plateSpawnPoint.position : transform.position;
        Quaternion baseRot = plateSpawnPoint ? plateSpawnPoint.rotation : transform.rotation;

        int layer = (_stackCount < 8) ? 0 : 1;
        int indexInLayer = (_stackCount < 8) ? _stackCount : (_stackCount - 8);

        Vector2 offset2D = (layer == 0)
            ? _layer0Offsets[indexInLayer] * ringRadius0
            : _layer1Offsets[indexInLayer] * ringRadius1;

        float yPos = layer * layerHeight;

        Vector3 jitterPos = new Vector3(
            UnityEngine.Random.Range(-posJitter, posJitter),
            0f,
            UnityEngine.Random.Range(-posJitter, posJitter)
        );

        Vector3 localPos = new Vector3(offset2D.x, yPos, offset2D.y) + jitterPos;
        Vector3 worldPos = basePos + baseRot * localPos;

        Quaternion jitterRot = Quaternion.Euler(
            UnityEngine.Random.Range(-tiltJitter, tiltJitter),
            UnityEngine.Random.Range(-yawJitter, yawJitter),
            UnityEngine.Random.Range(-tiltJitter, tiltJitter)
        );

        Quaternion worldRot = baseRot * flatOffset * jitterRot;

        GameObject piece = Instantiate(feedbackSet.platingPiecePrefab, worldPos, worldRot);
        if (_currentPlate) piece.transform.SetParent(_currentPlate.transform, true);

        _platingPieces.Add(piece);
        _stackCount++;
    }

    void ClearPlatingPieces()
    {
        UnityEngine.Debug.Log($"[PlateController] Clearing {_platingPieces.Count} plating pieces");

        foreach (var p in _platingPieces)
        {
            if (p) Destroy(p);
        }
        _platingPieces.Clear();
        _stackCount = 0;
    }
}

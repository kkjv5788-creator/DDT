// PlateController.cs (조각 정리 수정)
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

    GameObject _currentPlate;
    List<GameObject> _platingPieces = new List<GameObject>();
    int _stackCount = 0;

    // 고정 레이아웃: 1층 8개(링) + 2층 4개(내부)
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

    public void ResetToEmptyPlate()
    {
        UnityEngine.Debug.Log("[PlateController] ResetToEmptyPlate called");

        ClearCurrentPlate();
        ClearPlatingPieces(); // 🔥 조각들도 정리

        if (feedbackSet && feedbackSet.platePrefabEmptyStack)
        {
            Vector3 pos = plateSpawnPoint ? plateSpawnPoint.position : transform.position;
            Quaternion rot = plateSpawnPoint ? plateSpawnPoint.rotation : transform.rotation;
            _currentPlate = Instantiate(feedbackSet.platePrefabEmptyStack, pos, rot);
        }

        _stackCount = 0;
    }

    public void ShowSuccessPlate()
    {
        UnityEngine.Debug.Log("[PlateController] ShowSuccessPlate called");

        ClearCurrentPlate();
        ClearPlatingPieces(); // 🔥 조각들 정리 추가

        if (feedbackSet && feedbackSet.platePrefabSuccessNeat)
        {
            Vector3 pos = plateSpawnPoint ? plateSpawnPoint.position : transform.position;
            Quaternion rot = plateSpawnPoint ? plateSpawnPoint.rotation : transform.rotation;
            _currentPlate = Instantiate(feedbackSet.platePrefabSuccessNeat, pos, rot);
        }
    }

    public void ShowFailPlate()
    {
        UnityEngine.Debug.Log("[PlateController] ShowFailPlate called");

        ClearCurrentPlate();
        ClearPlatingPieces(); // 🔥 조각들 정리 추가

        if (feedbackSet && feedbackSet.platePrefabFailExplode)
        {
            Vector3 pos = plateSpawnPoint ? plateSpawnPoint.position : transform.position;
            Quaternion rot = plateSpawnPoint ? plateSpawnPoint.rotation : transform.rotation;
            _currentPlate = Instantiate(feedbackSet.platePrefabFailExplode, pos, rot);

            // 프리팹 내부에 폭발 애니/SFX 있다면 여기서 트리거
            var anim = _currentPlate.GetComponent<Animator>();
            if (anim) anim.SetTrigger("Explode");
        }
    }

    public void AddPlatingPiece()
    {
        if (!feedbackSet || !feedbackSet.platingPiecePrefab)
        {
            UnityEngine.Debug.LogWarning("[PlateController] feedbackSet or platingPiecePrefab is null!");
            return;
        }

        if (_stackCount >= feedbackSet.maxPlatingPiecesPerRound)
        {
            UnityEngine.Debug.LogWarning($"[PlateController] Stack full! {_stackCount}/{feedbackSet.maxPlatingPiecesPerRound}");
            return;
        }

        Vector3 basePos = plateSpawnPoint ? plateSpawnPoint.position : transform.position;
        Quaternion baseRot = plateSpawnPoint ? plateSpawnPoint.rotation : transform.rotation;

        // 레이아웃 계산
        int layer = (_stackCount < 8) ? 0 : 1;
        int indexInLayer = (_stackCount < 8) ? _stackCount : (_stackCount - 8);

        Vector2 offset2D = (layer == 0)
            ? _layer0Offsets[indexInLayer] * ringRadius0
            : _layer1Offsets[indexInLayer] * ringRadius1;

        float yPos = layer * layerHeight;

        // Jitter
        Vector3 jitterPos = new Vector3(
            UnityEngine.Random.Range(-posJitter, posJitter),
            0f,
            UnityEngine.Random.Range(-posJitter, posJitter)
        );

        Vector3 localPos = new Vector3(offset2D.x, yPos, offset2D.y) + jitterPos;
        Vector3 worldPos = basePos + baseRot * localPos;

        // Rotation jitter
        Quaternion jitterRot = Quaternion.Euler(
            UnityEngine.Random.Range(-tiltJitter, tiltJitter),
            UnityEngine.Random.Range(-yawJitter, yawJitter),
            UnityEngine.Random.Range(-tiltJitter, tiltJitter)
        );

        Quaternion worldRot = baseRot * jitterRot;

        // 생성
        GameObject piece = Instantiate(feedbackSet.platingPiecePrefab, worldPos, worldRot);
        _platingPieces.Add(piece);
        _stackCount++;

        UnityEngine.Debug.Log($"[PlateController] Plating piece #{_stackCount} spawned at {worldPos}");
    }

    void ClearCurrentPlate()
    {
        if (_currentPlate)
        {
            UnityEngine.Debug.Log("[PlateController] Destroying current plate");
            Destroy(_currentPlate);
            _currentPlate = null;
        }
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
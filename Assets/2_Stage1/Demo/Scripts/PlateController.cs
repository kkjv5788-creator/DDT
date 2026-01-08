using UnityEngine;

public class PlateController : MonoBehaviour
{
    [Header("Slots")]
    public Transform[] pieceSlots;          // 조각 놓일 자리(최대 N)
    public GameObject piecePrefab;          // 연출용 조각 프리팹

    [Header("Result Visuals (Optional)")]
    public GameObject cleanPlatePrefab;     // 성공 완성 접시(대체)
    public GameObject explodeVfxPrefab;     // 실패 폭발 VFX

    [Header("Audio (Optional)")]
    public AudioSource sfxSource;
    public AudioClip addPieceSfx;
    public AudioClip successPlateSfx;
    public AudioClip failPlateSfx;

    int _nextSlot;
    GameObject[] _spawnedPieces;

    void Awake()
    {
        _spawnedPieces = new GameObject[pieceSlots != null ? pieceSlots.Length : 0];
        ResetPlateVisual();
    }

    public void ResetPlateVisual()
    {
        _nextSlot = 0;

        if (_spawnedPieces != null)
        {
            for (int i = 0; i < _spawnedPieces.Length; i++)
            {
                if (_spawnedPieces[i]) Destroy(_spawnedPieces[i]);
                _spawnedPieces[i] = null;
            }
        }

        if (cleanPlatePrefab) cleanPlatePrefab.SetActive(false);
    }

    public void AddPiece()
    {
        if (pieceSlots == null || pieceSlots.Length == 0 || !piecePrefab) return;
        if (_nextSlot >= pieceSlots.Length) return;

        var slot = pieceSlots[_nextSlot];
        var go = Instantiate(piecePrefab, slot.position, slot.rotation, slot);
        _spawnedPieces[_nextSlot] = go;
        _nextSlot++;

        if (sfxSource && addPieceSfx) sfxSource.PlayOneShot(addPieceSfx);
    }

    public void ResolvePlate(bool success)
    {
        if (success)
        {
            // 정갈한 접시로 스왑(선택)
            if (cleanPlatePrefab)
            {
                // 누적 조각 숨김 or 제거
                for (int i = 0; i < _spawnedPieces.Length; i++)
                {
                    if (_spawnedPieces[i]) _spawnedPieces[i].SetActive(false);
                }
                cleanPlatePrefab.SetActive(true);
            }

            if (sfxSource && successPlateSfx) sfxSource.PlayOneShot(successPlateSfx);
        }
        else
        {
            if (explodeVfxPrefab)
            {
                Instantiate(explodeVfxPrefab, transform.position, transform.rotation);
            }

            // 누적 조각 파괴(또는 비활성화)
            for (int i = 0; i < _spawnedPieces.Length; i++)
            {
                if (_spawnedPieces[i]) Destroy(_spawnedPieces[i]);
                _spawnedPieces[i] = null;
            }

            if (cleanPlatePrefab) cleanPlatePrefab.SetActive(false);
            if (sfxSource && failPlateSfx) sfxSource.PlayOneShot(failPlateSfx);
        }
    }
}

using System.Diagnostics;
using UnityEngine;

public class KimbapSpawner : MonoBehaviour
{
    public KimbapController kimbapPrefab;
    public Transform spawnPoint;

    public KimbapController CurrentKimbap { get; private set; }

    public void EnsureKimbapExists()
    {
        if (CurrentKimbap) return;
        SpawnNew();
    }

    public void SpawnNew()
    {
        if (!kimbapPrefab)
        {
            UnityEngine.Debug.LogError("[KimbapSpawner] Missing kimbapPrefab.");
            return;
        }

        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : transform.rotation;

        CurrentKimbap = Instantiate(kimbapPrefab, pos, rot);
    }

    // 🔥 새로 추가: 기존 김밥 제거
    public void DestroyCurrentKimbap()
    {
        if (CurrentKimbap)
        {
            Destroy(CurrentKimbap.gameObject);
            CurrentKimbap = null;
        }
    }
}
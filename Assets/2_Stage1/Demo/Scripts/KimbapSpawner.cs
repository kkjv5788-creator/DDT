using System;
using System.Diagnostics;
using UnityEngine;

public class KimbapSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public Transform spawnPoint;
    public GameObject[] kimbapPrefabs;

    [Header("Runtime")]
    public GameObject currentKimbap;
    public GameObject preparedKimbap;

    public void CleanupCurrent()
    {
        if (currentKimbap) Destroy(currentKimbap);
        currentKimbap = null;
    }

    public void CleanupPrepared()
    {
        if (preparedKimbap) Destroy(preparedKimbap);
        preparedKimbap = null;
    }

    /// <summary>
    /// preparedKimbap이 비어있을 때만 Prepare 수행(중복 생성 방지)
    /// </summary>
    public void EnsurePreparedKimbap()
    {
        if (preparedKimbap != null) return;
        PrepareNextKimbap();
    }

    public void PrepareNextKimbap()
    {
        // ✅ 중복 생성 방지
        if (preparedKimbap != null) return;

        if (!spawnPoint || kimbapPrefabs == null || kimbapPrefabs.Length == 0)
        {
            UnityEngine.Debug.LogWarning("[KimbapSpawner] Missing spawnPoint or kimbapPrefabs.");
            return;
        }

        var prefab = kimbapPrefabs[UnityEngine.Random.Range(0, kimbapPrefabs.Length)];
        preparedKimbap = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        preparedKimbap.SetActive(false);
    }

    public void ActivatePreparedKimbap()
    {
        if (!preparedKimbap)
        {
            UnityEngine.Debug.LogWarning("[KimbapSpawner] No prepared kimbap to activate.");
            return;
        }

        CleanupCurrent();

        currentKimbap = preparedKimbap;
        preparedKimbap = null;

        currentKimbap.SetActive(true);
    }

    public KimbapController GetCurrentKimbapController()
    {
        if (!currentKimbap) return null;
        return currentKimbap.GetComponentInChildren<KimbapController>(true);
    }
}

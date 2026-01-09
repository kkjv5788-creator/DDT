using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class KimbapSpawner : MonoBehaviour
{
    public Transform spawnPoint;
    public List<GameObject> kimbapPrefabs = new List<GameObject>();

    public GameObject currentKimbap;
    public GameObject preparedKimbap;

    public void SpawnOrPrepare()
    {
        if (currentKimbap) return;

        if (preparedKimbap)
        {
            currentKimbap = preparedKimbap;
            preparedKimbap = null;
            currentKimbap.SetActive(true);
            return;
        }

        Spawn();
    }

    public GameObject Spawn(int index = 0)
    {
        if (!spawnPoint) spawnPoint = transform;
        if (kimbapPrefabs == null || kimbapPrefabs.Count == 0)
        {
            Debug.LogWarning("[KimbapSpawner] No prefabs.");
            return null;
        }

        index = Mathf.Clamp(index, 0, kimbapPrefabs.Count - 1);
        var prefab = kimbapPrefabs[index];
        if (!prefab) return null;

        var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        currentKimbap = go;
        return go;
    }

    public void ClearCurrent()
    {
        if (currentKimbap) Destroy(currentKimbap);
        currentKimbap = null;
    }
}

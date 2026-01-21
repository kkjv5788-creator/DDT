using System.Collections.Generic;
using UnityEngine;

public class BagView_st2 : MonoBehaviour
{
    [Header("슬롯")]
    public Transform[] slots = new Transform[3];

    [Header("기존(프리팹)")]
    public GameObject itemPrefab;

    public int itemCount = 0;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    private readonly List<FishCatchToken_st2> storedFish = new List<FishCatchToken_st2>();

    // 기존 호환
    public void AddItem()
    {
        AddItem(itemPrefab);
    }

    // 비주얼 프리팹 추가(override 지원)
    public void AddItem(GameObject overridePrefab)
    {
        if (itemCount >= slots.Length) return;

        var prefab = overridePrefab != null ? overridePrefab : itemPrefab;
        if (prefab == null) return;

        var item = Instantiate(prefab, slots[itemCount]);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.transform.localScale = Vector3.one * 0.8f;

        spawnedItems.Add(item);
        itemCount++;
    }

    // 실물 붕어빵 적재
    public void AddFish(FishCatchToken_st2 fish, float visualScale = 0.8f)
    {
        if (fish == null) return;
        if (itemCount >= slots.Length) return;

        // 봉투 안에서 튀지 않게
        var rb = fish.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 충돌 끄기
        var cols = fish.GetComponentsInChildren<Collider>(true);
        foreach (var col in cols) col.enabled = false;

        // 이동/충돌 로직 잠시 끄기(선택)
        fish.enabled = false;

        fish.transform.SetParent(slots[itemCount], false);
        fish.transform.localPosition = Vector3.zero;
        fish.transform.localRotation = Quaternion.identity;
        fish.transform.localScale = Vector3.one * visualScale;

        storedFish.Add(fish);
        itemCount++;
    }

    public void Clear()
    {
        // 비주얼 프리팹 제거
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null) Destroy(spawnedItems[i]);
        }
        spawnedItems.Clear();

        // 실물 붕어빵은 풀로 반환
        for (int i = storedFish.Count - 1; i >= 0; i--)
        {
            var fish = storedFish[i];
            if (fish == null) continue;

            // 재사용 위해 원복
            var cols = fish.GetComponentsInChildren<Collider>(true);
            foreach (var col in cols) col.enabled = true;

            fish.enabled = true;

            fish.transform.SetParent(null, true);

            if (fish.ownerMold != null) fish.ownerMold.ReleaseFish(fish);
            else Destroy(fish.gameObject);
        }
        storedFish.Clear();

        itemCount = 0;
    }

    private void OnDestroy()
    {
        // 봉투 오브젝트가 파괴될 때 누수 방지
        Clear();
    }
}

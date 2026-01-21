using System.Collections.Generic;
using System.Diagnostics;
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

    // ✅ 실물 붕어빵 적재 (개선)
    public void AddFish(FishCatchToken_st2 fish, float visualScale = 0.8f)
    {
        if (fish == null)
        {
            UnityEngine.Debug.LogError("❌ AddFish: fish is null!");
            return;
        }

        if (itemCount >= slots.Length)
        {
            UnityEngine.Debug.LogWarning("❌ AddFish: Bag is full!");
            return;
        }

        if (slots[itemCount] == null)
        {
            UnityEngine.Debug.LogError($"❌ AddFish: Slot {itemCount} is null!");
            return;
        }

        UnityEngine.Debug.Log($"✅ AddFish: Adding {fish.gameObject.name} to slot {itemCount}");

        // ✅ 봉투 안에서 튀지 않게 (중복 체크하지만 안전하게)
        var rb = fish.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ✅ 충돌 끄기
        var cols = fish.GetComponentsInChildren<Collider>(true);
        foreach (var col in cols) col.enabled = false;

        // ✅ 이동/충돌 로직 잠시 끄기
        fish.enabled = false;

        // ✅ 슬롯으로 부모 설정 (worldPositionStays = false로 로컬 좌표 사용)
        fish.transform.SetParent(slots[itemCount], false);
        fish.transform.localPosition = Vector3.zero;
        fish.transform.localRotation = Quaternion.identity;
        fish.transform.localScale = Vector3.one * visualScale;

        storedFish.Add(fish);
        itemCount++;

        UnityEngine.Debug.Log($"✅ Fish added successfully. Total items in bag: {itemCount}");
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

            UnityEngine.Debug.Log($"✅ Returning {fish.gameObject.name} to pool");

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
        UnityEngine.Debug.Log($"✅ BagView destroyed, clearing {storedFish.Count} fish");
        Clear();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BagView_st2 : MonoBehaviour
{
    [Header("ΩΩ∑‘")]
    public Transform[] slots = new Transform[3];

    [Header("æ∆¿Ã≈€")]
    public GameObject itemPrefab;
    public int itemCount = 0;

    private List<GameObject> items = new List<GameObject>();

    public void AddItem()
    {
        if (itemCount >= 3) return;

        var item = Instantiate(itemPrefab, slots[itemCount]);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.transform.localScale = Vector3.one * 0.8f;

        items.Add(item);
        itemCount++;
    }

    public void Clear()
    {
        foreach (var item in items)
        {
            Destroy(item);
        }
        items.Clear();
        itemCount = 0;
    }
}

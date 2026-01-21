using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class BagManager_st2 : MonoBehaviour
{
    [Header("현재 봉투/스택")]
    public Transform currentBagAnchor;
    public Transform filledBagStackAnchor;
    public GameObject bagPrefab;
    public int itemsPerBag = 3;
    public int maxFilledBags = 10;

    [Header("스택 배치")]
    public float stackSpacingY = 0.12f;

    [Header("붕어빵 비주얼 프리팹(선택)")]
    public GameObject fishVisualPrefab;

    private BagView_st2 currentBag;
    private readonly List<BagView_st2> filledBags = new List<BagView_st2>();

    public event Action OnBagSwapped;
    public event Action<int> OnItemAdded;

    void Start()
    {
        CreateNewCurrentBag();
    }

    // =========================
    // 호환용 AddItem 오버로드
    // =========================

    // (1) 기존 호환: 그냥 비주얼 1개 추가
    public void AddItem()
    {
        AddItem((GameObject)null);
    }

    // (2) ✅ 비주얼 프리팹을 봉투에 추가 (이게 메인 방식)
    public void AddItem(GameObject overrideVisualPrefab)
    {
        AddFishVisual(overrideVisualPrefab);
    }

    // (3) 실물 붕어빵을 봉투에 추가 (사용 안 함, 호환용만 남김)
    public void AddItem(FishCatchToken_st2 fish)
    {
        // 비주얼 방식 사용 시 이 메서드는 호출 안 됨
        UnityEngine.Debug.LogWarning("AddItem(FishCatchToken) called but using visual mode");
        AddFishVisual(fishVisualPrefab);
    }

    // =========================
    // ✅ 비주얼 쌓기 (메인 로직)
    // =========================
    public void AddFishVisual(GameObject overrideVisualPrefab)
    {
        if (currentBag == null)
        {
            UnityEngine.Debug.LogError("❌ currentBag is null!");
            return;
        }

        var prefab = overrideVisualPrefab != null ? overrideVisualPrefab : fishVisualPrefab;

        if (prefab == null)
        {
            UnityEngine.Debug.LogError("❌ No fish visual prefab assigned!");
            return;
        }

        currentBag.AddItem(prefab);
        UnityEngine.Debug.Log($"✅ Visual added to bag. Count: {currentBag.itemCount}/{itemsPerBag}");

        OnItemAdded?.Invoke(currentBag.itemCount);

        if (currentBag.itemCount >= itemsPerBag)
        {
            UnityEngine.Debug.Log("✅ Bag full, swapping...");
            SwapBag();
        }
    }

    void SwapBag()
    {
        AddFilledBagToStack(currentBag);
        CreateNewCurrentBag();
        OnBagSwapped?.Invoke();
    }

    void CreateNewCurrentBag()
    {
        if (bagPrefab == null || currentBagAnchor == null)
        {
            UnityEngine.Debug.LogError("❌ bagPrefab or currentBagAnchor not assigned!");
            return;
        }

        var bagObj = Instantiate(bagPrefab, currentBagAnchor);
        bagObj.transform.localPosition = Vector3.zero;
        bagObj.transform.localRotation = Quaternion.identity;

        currentBag = bagObj.GetComponent<BagView_st2>();
        if (currentBag == null)
            currentBag = bagObj.AddComponent<BagView_st2>();

        UnityEngine.Debug.Log("✅ New bag created");

        StartCoroutine(FadeIn(bagObj, 0.2f));
    }

    void AddFilledBagToStack(BagView_st2 completedBag)
    {
        if (completedBag == null || filledBagStackAnchor == null) return;

        // 스택 꽉 차면 가장 오래된 것 제거
        if (filledBags.Count >= maxFilledBags)
        {
            var oldest = filledBags[0];
            filledBags.RemoveAt(0);
            if (oldest != null) StartCoroutine(FadeOutAndDestroy(oldest.gameObject, 0.2f));
        }

        filledBags.Add(completedBag);

        completedBag.transform.SetParent(filledBagStackAnchor, true);
        RepositionStack();
    }

    void RepositionStack()
    {
        for (int i = 0; i < filledBags.Count; i++)
        {
            if (filledBags[i] == null) continue;
            filledBags[i].transform.localPosition = new Vector3(0f, -stackSpacingY * i, 0f);
            filledBags[i].transform.localRotation = Quaternion.identity;
        }
    }

    IEnumerator FadeIn(GameObject obj, float duration)
    {
        if (obj == null) yield break;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
            }

            yield return null;
        }
    }

    IEnumerator FadeOutAndDestroy(GameObject obj, float duration)
    {
        if (obj == null) yield break;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / duration);

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
            }

            yield return null;
        }

        Destroy(obj);
    }

    // =========================
    // 외부에서 쓰는 유틸
    // =========================

    public void FinalizeBag()
    {
        if (currentBag != null && currentBag.itemCount > 0)
        {
            AddFilledBagToStack(currentBag);
            currentBag = null;
        }
    }

    public int GetFilledBagCount()
    {
        return filledBags.Count;
    }

    public void Reset()
    {
        for (int i = filledBags.Count - 1; i >= 0; i--)
        {
            if (filledBags[i] != null) Destroy(filledBags[i].gameObject);
        }
        filledBags.Clear();

        if (currentBag != null)
            Destroy(currentBag.gameObject);

        CreateNewCurrentBag();
    }
}
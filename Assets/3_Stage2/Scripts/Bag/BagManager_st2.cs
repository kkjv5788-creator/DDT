// BagManager_st2.cs (Assets/3_Stage2/Demo/Scripts/BagManager_st2.cs)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BagManager_st2 : MonoBehaviour
{
    [Header("현재 봉투/스택")]
    public Transform currentBagAnchor;
    public Transform filledBagStackAnchor;
    public GameObject bagPrefab;
    public int itemsPerBag = 3;
    public int maxFilledBags = 10;

    [Header("스택 배치 (Legacy)")]
    [Tooltip("예전(Y축) 스택 간격. 이제 그리드 배치를 쓰므로 기본적으로 사용하지 않음.")]
    public float stackSpacingY = 0.12f;

    [Header("스택 배치 (Grid)")]
    [Tooltip("한 줄(가로)로 몇 개 쌓을지. 요청사항: 5")]
    public int stackColumns = 5;

    [Tooltip("가로(-X) 방향 봉투 간격(로컬 기준)")]
    public float stackSpacingX = 0.12f;

    [Tooltip("세로(-Z) 방향 줄 간격(로컬 기준)")]
    public float stackSpacingZ = 0.9f;

    [Tooltip("스택의 시작 오프셋(로컬). 기준 위치 미세조정용")]
    public Vector3 stackOffsetLocal = Vector3.zero;

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
        Debug.LogWarning("AddItem(FishCatchToken) called but using visual mode");
        AddFishVisual(fishVisualPrefab);
    }

    // =========================
    // ✅ 비주얼 쌓기 (메인 로직)
    // =========================
    public void AddFishVisual(GameObject overrideVisualPrefab)
    {
        if (currentBag == null)
        {
            Debug.LogError("❌ currentBag is null!");
            return;
        }

        var prefab = overrideVisualPrefab != null ? overrideVisualPrefab : fishVisualPrefab;

        if (prefab == null)
        {
            Debug.LogError("❌ No fish visual prefab assigned!");
            return;
        }

        currentBag.AddItem(prefab);
        Debug.Log($"✅ Visual added to bag. Count: {currentBag.itemCount}/{itemsPerBag}");

        OnItemAdded?.Invoke(currentBag.itemCount);

        if (currentBag.itemCount >= itemsPerBag)
        {
            Debug.Log("✅ Bag full, swapping...");
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
            Debug.LogError("❌ bagPrefab or currentBagAnchor not assigned!");
            return;
        }

        var bagObj = Instantiate(bagPrefab, currentBagAnchor);
        bagObj.transform.localPosition = Vector3.zero;
        bagObj.transform.localRotation = Quaternion.identity;

        currentBag = bagObj.GetComponent<BagView_st2>();
        if (currentBag == null)
            currentBag = bagObj.AddComponent<BagView_st2>();

        Debug.Log("✅ New bag created");

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

    // ✅ 핵심 변경: Y축 스택 -> (-X)로 5개씩, 다음 줄은 (-Z)로 이동하는 그리드
    void RepositionStack()
    {
        int cols = Mathf.Max(1, stackColumns);
        float dx = Mathf.Abs(stackSpacingX); // 항상 양수로 받아서
        float dz = Mathf.Abs(stackSpacingZ); // 아래에서 - 부호로 방향 고정

        for (int i = 0; i < filledBags.Count; i++)
        {
            if (filledBags[i] == null) continue;

            int col = i % cols;      // 0~4
            int row = i / cols;      // 0부터 증가

            float x = -dx * col;     // 요청: -X로 진행
            float z = -dz * row;     // 요청: -Z로 줄바꿈

            filledBags[i].transform.localPosition = stackOffsetLocal + new Vector3(x, 0f, z);
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

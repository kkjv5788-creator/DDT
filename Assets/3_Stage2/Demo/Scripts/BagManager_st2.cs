using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BagManager_st2 : MonoBehaviour
{
    [Header("봉투 설정")]
    public Transform currentBagAnchor;
    public Transform filledBagStackAnchor;
    public GameObject bagPrefab;
    public int itemsPerBag = 3;
    public int maxFilledBags = 10;

    [Header("스택 설정")]
    public int columns = 2;
    public Vector3 colStep = new Vector3(0.08f, 0, 0);
    public Vector3 rowStep = new Vector3(0, 0, -0.09f);
    public float[] yawPattern = { -6f, 6f };
    public float jitterPos = 0.004f;
    public float jitterYaw = 1f;

    [Header("현재 상태")]
    private BagView_st2 currentBag;
    private List<BagView_st2> filledBags = new List<BagView_st2>();

    public event Action OnBagSwapped;
    public event Action<int> OnItemAdded;

    void Start()
    {
        CreateNewCurrentBag();
    }

    public void AddItem()
    {
        if (currentBag == null) return;

        currentBag.AddItem();
        OnItemAdded?.Invoke(currentBag.itemCount);

        if (currentBag.itemCount >= itemsPerBag)
        {
            SwapBag();
        }
    }

    void SwapBag()
    {
        // 현재 봉투를 스택으로 이동
        AddFilledBagToStack(currentBag);

        // 새 봉투 생성
        CreateNewCurrentBag();

        OnBagSwapped?.Invoke();
    }

    void CreateNewCurrentBag()
    {
        var bagObj = Instantiate(bagPrefab, currentBagAnchor);
        bagObj.transform.localPosition = Vector3.zero;
        bagObj.transform.localRotation = Quaternion.identity;

        currentBag = bagObj.GetComponent<BagView_st2>();

        // 페이드인 효과 (선택)
        StartCoroutine(FadeIn(bagObj, 0.2f));
    }

    void AddFilledBagToStack(BagView_st2 completedBag)
    {
        // 스택 초과 시 가장 오래된 봉투 제거 (FIFO)
        if (filledBags.Count >= maxFilledBags)
        {
            var oldestBag = filledBags[0];
            filledBags.RemoveAt(0);
            StartCoroutine(FadeOutAndDestroy(oldestBag.gameObject, 0.3f));
        }

        filledBags.Add(completedBag);

        // 스택 위치로 이동 (비동기)
        int index = filledBags.Count - 1;
        Vector3 targetPos = CalculateStackPosition(index);
        Quaternion targetRot = CalculateStackRotation(index);

        completedBag.transform.SetParent(filledBagStackAnchor);
        StartCoroutine(LerpToStack(completedBag.transform, targetPos, targetRot, 0.4f));
    }

    Vector3 CalculateStackPosition(int index)
    {
        int row = index / columns;
        int col = index % columns;

        Vector3 pos = col * colStep + row * rowStep;

        // 지터 추가
        pos += new Vector3(
            UnityEngine.Random.Range(-jitterPos, jitterPos),
            0,
            UnityEngine.Random.Range(-jitterPos, jitterPos)
        );

        return pos;
    }

    Quaternion CalculateStackRotation(int index)
    {
        float yaw = yawPattern[index % yawPattern.Length];
        yaw += UnityEngine.Random.Range(-jitterYaw, jitterYaw);
        return Quaternion.Euler(0, yaw, 0);
    }

    IEnumerator LerpToStack(Transform bag, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = bag.localPosition;
        Quaternion startRot = bag.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            bag.localPosition = Vector3.Lerp(startPos, targetPos, t);
            bag.localRotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        bag.localPosition = targetPos;
        bag.localRotation = targetRot;
    }

    IEnumerator FadeIn(GameObject obj, float duration)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / duration;

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
        // 모든 봉투 제거
        foreach (var bag in filledBags)
        {
            Destroy(bag.gameObject);
        }
        filledBags.Clear();

        if (currentBag != null)
        {
            Destroy(currentBag.gameObject);
        }

        CreateNewCurrentBag();
    }
}

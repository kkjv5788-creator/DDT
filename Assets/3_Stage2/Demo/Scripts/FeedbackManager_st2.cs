using Oculus.Interaction.Feedback;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class FeedbackManager_st2 : MonoBehaviour
{
    public static FeedbackManager_st2 Instance { get; private set; }

    [Header("피드백 프리팹")]
    public GameObject feedbackTextPrefab;

    private ObjectPool<GameObject> feedbackPool;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 피드백 풀 생성
        feedbackPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(feedbackTextPrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: 10
        );
    }

    public void ShowJudgeFeedback(Vector3 position, string text)
    {
        var feedbackObj = feedbackPool.Get();
        feedbackObj.transform.position = position + Vector3.up * 0.1f;

        var tmp = feedbackObj.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.fontSize = 8f;
        }

        StartCoroutine(AnimateFeedback(feedbackObj, 0.4f));
    }

    IEnumerator AnimateFeedback(GameObject obj, float duration)
    {
        Vector3 startPos = obj.transform.position;
        Vector3 endPos = startPos + Vector3.up * 0.1f;

        var tmp = obj.GetComponent<TextMeshPro>();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            obj.transform.position = Vector3.Lerp(startPos, endPos, t);

            if (tmp != null)
            {
                Color color = tmp.color;
                color.a = 1f - t;
                tmp.color = color;
            }

            yield return null;
        }

        feedbackPool.Release(obj);
    }
}

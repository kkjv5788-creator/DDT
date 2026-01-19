using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CrowdMember_st2;

public class CrowdManager_st2 : MonoBehaviour
{
    [Header("설정")]
    public List<CrowdMember> crowdMembers = new List<CrowdMember>();
    public int startVisible = 3;
    public int maxVisible = 20;

    [Header("진행도")]
    public float revealStartProgress = 0.1f;
    public float revealEndProgress = 0.9f;
    public AnimationCurve revealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int visibleCount = 0;
    private AudioSource audioSource;
    private AudioClip currentClip;

    public void Initialize(AudioSource source, AudioClip clip)
    {
        audioSource = source;
        currentClip = clip;

        // 컴포넌트 캐싱
        foreach (var member in crowdMembers)
        {
            member.cachedRenderer = member.gameObject.GetComponent<Renderer>();
            member.cachedAnimator = member.gameObject.GetComponent<Animator>();
        }

        // 초기 활성화
        visibleCount = startVisible;
        for (int i = 0; i < crowdMembers.Count; i++)
        {
            crowdMembers[i].gameObject.SetActive(i < startVisible);
        }
    }

    void Update()
    {
        if (audioSource == null || currentClip == null) return;

        float progress = audioSource.time / currentClip.length;
        int targetVisible = CalculateTargetVisible(progress);

        while (visibleCount < targetVisible && visibleCount < crowdMembers.Count)
        {
            crowdMembers[visibleCount].gameObject.SetActive(true);
            visibleCount++;
        }
    }

    int CalculateTargetVisible(float progress)
    {
        if (progress < revealStartProgress)
        {
            return startVisible;
        }
        else if (progress > revealEndProgress)
        {
            return maxVisible;
        }
        else
        {
            float t = (progress - revealStartProgress) / (revealEndProgress - revealStartProgress);
            float curveT = revealCurve.Evaluate(t);
            return Mathf.RoundToInt(Mathf.Lerp(startVisible, maxVisible, curveT));
        }
    }

    public void Reset()
    {
        visibleCount = startVisible;
        for (int i = 0; i < crowdMembers.Count; i++)
        {
            crowdMembers[i].gameObject.SetActive(i < startVisible);
        }
    }
}

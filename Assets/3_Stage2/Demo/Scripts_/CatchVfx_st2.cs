using System.Collections;
using UnityEngine;

public class CatchVfx_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Timing")]
    [SerializeField] private float snapHoldSeconds = 0.2f;
    [SerializeField] private float fadeSeconds = 0.25f;

    public void Construct(Stage2EventHub_st2 eventHub)
    {
        hub = eventHub;
    }

    private void OnEnable()
    {
        if (hub != null) hub.JudgeResolved += OnJudgeResolved;
    }

    private void OnDisable()
    {
        if (hub != null) hub.JudgeResolved -= OnJudgeResolved;
    }

    private void OnJudgeResolved(FishCatchToken_st2 fish, JudgeResult_st2 result, OVRInput.Controller hand, float delta)
    {
        if (fish == null) return;

        bool success = (result == JudgeResult_st2.Perfect || result == JudgeResult_st2.Good);
        if (!success) return;

        // ✅ CatchInput에 있던 “성공 연출/풀 반환” 로직을 여기로 이동
        StartCoroutine(CoSuccessConsume(fish, hand));
    }

    private IEnumerator CoSuccessConsume(FishCatchToken_st2 fish, OVRInput.Controller hand)
    {
        // 1) 한 프레임 스냅(요구사항)
        fish.SnapToHandOneFrame(hand); // 너 기존 OnCaught 경로의 스냅 코드를 함수로 빼서 호출 권장
        yield return null;

        // 2) 홀드 + 페이드 (기존 연출 유지)
        yield return fish.PlayHoldAndFade(snapHoldSeconds, fadeSeconds);

        // 3) 소비 요청 발행 (여기서 BagPolicy가 봉투 비주얼을 추가하도록 만들면 타이밍 버그가 줄어듦)
        if (hub != null) hub.PublishFishConsumeRequested(fish, FishConsumeReason_st2.Success);
    }
}

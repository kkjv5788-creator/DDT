using UnityEngine;

public class BlockHitDetector : MonoBehaviour
{
    public RhythmConductor conductor;
    public DebugHUD hud;

    void OnCollisionEnter(Collision collision)
    {
        if (!conductor) return;

        // 🔥 Judging 중에도 WrongCut 감지 (기존: IsJudging이면 return)
        // PDF에서는 Non-Judging일 때만 WrongCut이라고 했지만,
        // Blocker는 항상 WrongCut으로 처리하는 게 더 합리적

        // WrongCut feedback (debug only)
        if (hud)
        {
            string msg = conductor.IsJudgingWindow()
                ? "WrongCut: Hit blocked area during Judging"
                : "WrongCut: Hit blocked Kimbap (Non-Judging)";
            hud.Log(msg);
        }

        // 🔥 WrongCut 이벤트 발행
        if (conductor && collision.contacts.Length > 0)
        {
            conductor.NotifyWrongCut(collision.contacts[0].point);
        }
    }
}
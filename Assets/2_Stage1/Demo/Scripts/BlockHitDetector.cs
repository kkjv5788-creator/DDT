using UnityEngine;

public class BlockHitDetector : MonoBehaviour
{
    public RhythmConductor conductor;
    public DebugHUD hud;

    void OnCollisionEnter(Collision collision)
    {
        if (!conductor) return;
        if (conductor.IsJudgingWindow()) return; // judging이면 block collider가 꺼져있는 구성이 일반적

        // WrongCut feedback (debug only)
        if (hud) hud.Log("WrongCut: Hit blocked Kimbap (Non-Judging)");
    }
}

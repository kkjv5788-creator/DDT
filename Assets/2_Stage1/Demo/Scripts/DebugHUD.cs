using UnityEngine;

[DisallowMultipleComponent]
public class DebugHUD : MonoBehaviour
{
    public KnifeVelocityEstimator knifeSpeedSource;

    float _lastBlockedTime;

    void Update()
    {
        if (knifeSpeedSource)
        {
            // 필요하면 여기에 TextMeshPro 연동해서 화면 표시하면 됨
            // 지금은 최소 디버그 로그만 (너무 스팸 안 나게)
        }
    }

    public void NotifyBlocked(Collider c)
    {
        if (Time.time - _lastBlockedTime < 0.2f) return;
        _lastBlockedTime = Time.time;
        Debug.Log($"[HUD] Blocked by: {c.name}");
    }

    public void NotifySlice(SliceResult r)
    {
        Debug.Log($"[HUD] Slice: quality={r.quality} speed={r.knifeSpeed:F2}");
    }
}

using UnityEngine;

public class SmartHeightManager : MonoBehaviour
{
    [Header("상황별 눈높이 설정")]
    public float titleEyeHeight = 0.0f; // 타이틀에서는 0 (또는 앉은 키)
    public float gameEyeHeight = 1.7f;  // 게임에서는 1.7m

    [Header("상태 모니터링")]
    public bool isGameMode = false; // 체크되면 게임 높이 적용

    private Transform trackingSpace;

    void Start()
    {
        trackingSpace = transform.Find("TrackingSpace");
        
        // [핵심] OVR이 제멋대로 바닥 모드(Floor Level)로 바꾸는 걸 막고
        // '눈 높이(Eye Level)' 모드로 강제 고정합니다.
        // 그래야 우리가 스크립트로 Y값을 마음대로 조작할 수 있습니다.
        if (OVRManager.instance != null)
        {
            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
        }

        // 시작은 타이틀 높이로
        ApplyHeight(titleEyeHeight);
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        // 에디터 테스트용: 모드에 따라 높이 계속 갱신
        if (trackingSpace != null)
        {
            float targetHeight = isGameMode ? gameEyeHeight : titleEyeHeight;
            ApplyHeight(targetHeight);
        }
#endif
    }

    private void ApplyHeight(float yHeight)
    {
        if (trackingSpace != null)
        {
            Vector3 pos = trackingSpace.localPosition;
            pos.y = yHeight;
            trackingSpace.localPosition = pos;
        }
    }

    // 버튼에서 호출할 함수
    public void SwitchToGameHeight()
    {
        isGameMode = true;
    }
}
using UnityEngine;

public class SmartHeightManager : MonoBehaviour
{
    [Header("상황별 눈높이 설정")]
    public float titleEyeHeight = 0.0f; // 타이틀 높이 (기본)
    public float gameEyeHeight = 1.7f;  // 게임 높이 (1.7m)

    [Header("상태 확인용")]
    public bool isGameMode = false; // 현재 게임 모드인지 체크

    private Transform trackingSpace;

    void Start()
    {
        // OVRCameraRig의 TrackingSpace 찾기
        trackingSpace = transform.Find("TrackingSpace");
        if (trackingSpace == null) Debug.LogWarning("TrackingSpace를 찾지 못했습니다.");
    }

    void LateUpdate()
    {
        // 에디터에서만 작동하도록 제한 (실제 기기에서는 센서값 사용)
#if UNITY_EDITOR
        if (trackingSpace != null)
        {
            // 현재 모드에 따라 목표 높이 결정
            float targetHeight = isGameMode ? gameEyeHeight : titleEyeHeight;
            
            // 높이 강제 적용
            Vector3 pos = trackingSpace.localPosition;
            pos.y = targetHeight;
            trackingSpace.localPosition = pos;
        }
#endif
    }

    // 외부(버튼)에서 호출할 함수: "게임 시작했으니 키 키워라!"
    public void SwitchToGameHeight()
    {
        isGameMode = true;
    }
}
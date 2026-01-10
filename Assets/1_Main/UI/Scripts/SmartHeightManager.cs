using UnityEngine;

public class SmartHeightManager : MonoBehaviour
{
    [Header("상황별 눈높이 설정")]
    public float titleEyeHeight = 0.0f; // 타이틀에서는 0
    public float gameEyeHeight = 1.7f;  // 게임에서는 1.7m

    [Header("설정")]
    // [추가된 기능] 체크하면 이 씬이 시작될 때 강제로 게임 모드(1.7m)로 설정합니다.
    public bool forceGameModeOnStart = false; 

    // 전역 변수 (씬이 바뀌어도 값 유지)
    public static bool isGameMode = false; 

    private Transform trackingSpace;

    void Awake()
    {
        // 스테이지1처럼 바로 게임이 시작되는 씬에서는 이 옵션을 켜두면 됩니다.
        if (forceGameModeOnStart)
        {
            isGameMode = true;
        }
    }

    void Start()
    {
        trackingSpace = transform.Find("TrackingSpace");

        if (OVRManager.instance != null)
        {
            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
        }

        // 현재 모드에 맞춰 높이 적용
        float currentTargetHeight = isGameMode ? gameEyeHeight : titleEyeHeight;
        ApplyHeight(currentTargetHeight);
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
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
            // 미세한 떨림 방지
            if (Mathf.Abs(trackingSpace.localPosition.y - yHeight) < 0.001f) return;

            Vector3 pos = trackingSpace.localPosition;
            pos.y = yHeight;
            trackingSpace.localPosition = pos;
        }
    }

    public void SwitchToGameHeight()
    {
        isGameMode = true;
        ApplyHeight(gameEyeHeight);
    }
}
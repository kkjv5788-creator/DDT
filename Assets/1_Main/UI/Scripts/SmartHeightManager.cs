using UnityEngine;

public class SmartHeightManager : MonoBehaviour
{
    [Header("상황별 눈높이 설정")]
    public float titleEyeHeight = 0.0f; // 타이틀에서는 0 (바닥)
    public float gameEyeHeight = 1.7f;  // 게임/로비에서는 1.7m

    [Header("설정")]
    public bool forceGameModeOnStart = false; // 체크하면 시작하자마자 키가 커짐 (Stage1용)

    // 전역 변수
    public static bool isGameMode = false; 

    private Transform trackingSpace;

    void Awake()
    {
        if (forceGameModeOnStart)
        {
            isGameMode = true;
        }
    }

    void Start()
    {
        // Building Blocks 구조에 맞춰 TrackingSpace 찾기
        trackingSpace = transform.Find("TrackingSpace");
        
        // 못 찾으면 자식들을 다 뒤져서라도 찾음
        if (trackingSpace == null)
            trackingSpace = GetComponentInChildren<OVRCameraRig>()?.transform.Find("TrackingSpace");

        if (OVRManager.instance != null)
        {
            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
        }

        ApplyHeight();
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        ApplyHeight();
#endif
    }

    private void ApplyHeight()
    {
        if (trackingSpace != null)
        {
            float targetHeight = isGameMode ? gameEyeHeight : titleEyeHeight;

            // 미세한 떨림 방지
            if (Mathf.Abs(trackingSpace.localPosition.y - targetHeight) < 0.001f) return;

            Vector3 pos = trackingSpace.localPosition;
            pos.y = targetHeight;
            trackingSpace.localPosition = pos;
        }
    }

    // 외부에서 호출하는 함수
    public void SwitchToGameHeight()
    {
        isGameMode = true;
        ApplyHeight();
    }
}
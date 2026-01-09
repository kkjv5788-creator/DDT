using UnityEngine;

public class EditorSimulateHeight : MonoBehaviour
{
    [Header("시뮬레이션 눈높이")]
    public float simulatedHeight = 1.7f;

    void Start()
    {
#if UNITY_EDITOR
        if (OVRManager.instance == null || !OVRManager.isHmdPresent)
        {
            // 1. 내 자식들 중에서 "TrackingSpace"라는 애를 찾는다.
            Transform trackingSpace = transform.Find("TrackingSpace");

            if (trackingSpace != null)
            {
                // 2. 몸통은 놔두고, "눈(TrackingSpace)"만 위로 올린다.
                trackingSpace.localPosition += Vector3.up * simulatedHeight;
                Debug.Log($"[테스트 모드] 카메라(TrackingSpace)만 {simulatedHeight}m 높였습니다.");
            }
            else
            {
                Debug.LogWarning("TrackingSpace를 찾을 수 없습니다! OVRCameraRig 구조를 확인하세요.");
            }
        }
#endif
    }
}
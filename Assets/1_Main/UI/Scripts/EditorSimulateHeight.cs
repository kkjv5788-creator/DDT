using UnityEngine;

public class EditorSimulateHeight : MonoBehaviour
{
    [Header("시뮬레이션 눈높이 (미터)")]
    public float simulatedHeight = 1.7f; // 성인 평균 눈높이

    void Start()
    {
        // 1. 유니티 에디터이면서 & VR 기기가 연결 안 되어 있을 때만 실행
#if UNITY_EDITOR
        if (OVRManager.instance == null || !OVRManager.isHmdPresent)
        {
            // OVRCameraRig 자체를 위로 들어 올림
            transform.position += Vector3.up * simulatedHeight;
            Debug.Log($"[테스트 모드] 키를 {simulatedHeight}m 높였습니다.");
        }
#endif
    }
}
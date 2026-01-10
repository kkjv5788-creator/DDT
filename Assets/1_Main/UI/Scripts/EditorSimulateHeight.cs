using UnityEngine;

public class EditorSimulateHeight : MonoBehaviour
{
    [Header("시뮬레이션 눈높이")]
    public float simulatedHeight = 1.7f;
    
    [Header("강제 적용 여부")]
    [Tooltip("체크하면 헤드셋이 연결되어 있어도 강제로 키를 높입니다.")]
    public bool forceSimulation = true; // 기본값을 true로 설정

    private Transform trackingSpace;

    void Start()
    {
        trackingSpace = transform.Find("TrackingSpace");
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        // 조건 변경: forceSimulation이 켜져 있거나, 헤드셋이 없을 때 작동
        if (forceSimulation || OVRManager.instance == null || !OVRManager.isHmdPresent)
        {
            if (trackingSpace != null)
            {
                Vector3 targetPos = trackingSpace.localPosition;
                targetPos.y = simulatedHeight;
                trackingSpace.localPosition = targetPos;
            }
        }
#endif
    }
}
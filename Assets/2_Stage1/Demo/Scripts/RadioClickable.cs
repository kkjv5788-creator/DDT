using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;

public class RadioClickable : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnRadioClicked;

    [Header("VR Setup")]
    public Transform rightHandAnchor; // OVRCameraRig의 RightHandAnchor 할당

    [Header("Visual Feedback (Optional)")]
    public Renderer radioRenderer;
    public Material activeMaterial;   // 클릭 가능할 때 머티리얼
    public Material inactiveMaterial; // 클릭 불가능할 때 머티리얼

    bool _tutorialCompleted = false;
    bool _clickable = false; // 🔥 클릭 가능 여부

    void Start()
    {
        // 초기 상태: 클릭 불가
        UpdateVisuals();
    }

    public void SetTutorialCompleted(bool completed)
    {
        _tutorialCompleted = completed;

        if (completed)
        {
            UnityEngine.Debug.Log("[RadioClickable] Tutorial completed - Radio can now be enabled");
        }
    }

    // 🔥 클릭 가능 여부 설정
    public void SetClickable(bool clickable)
    {
        _clickable = clickable;
        UnityEngine.Debug.Log($"[RadioClickable] Clickable set to: {clickable}");
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        // 비주얼 피드백 (선택사항)
        if (radioRenderer && activeMaterial && inactiveMaterial)
        {
            radioRenderer.material = _clickable ? activeMaterial : inactiveMaterial;
        }
    }

    void OnMouseDown()
    {
        // 🔥 클릭 가능 상태가 아니면 무시
        if (!_clickable)
        {
            UnityEngine.Debug.Log("[RadioClickable] Radio not clickable yet.");
            return;
        }

        UnityEngine.Debug.Log("[RadioClickable] Radio clicked! Starting main game...");

        // 🔥 클릭 후 즉시 비활성화 (1회만 클릭)
        _clickable = false;
        UpdateVisuals();

        OnRadioClicked?.Invoke();
    }

    // VR용 레이캐스트 처리
    void Update()
    {
        // 🔥 클릭 가능 상태가 아니면 무시
        if (!_clickable) return;

        // A 버튼 (Primary Index Trigger) 눌렀을 때
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            // 🔥 방법 1: RightHandAnchor 사용 (권장)
            if (rightHandAnchor)
            {
                Ray ray = new Ray(rightHandAnchor.position, rightHandAnchor.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, 2f))
                {
                    UnityEngine.Debug.Log($"[RadioClickable] VR Raycast hit: {hit.collider.gameObject.name}");

                    if (hit.collider.gameObject == gameObject)
                    {
                        UnityEngine.Debug.Log("[RadioClickable] Radio clicked via VR!");

                        // 🔥 클릭 후 즉시 비활성화 (1회만 클릭)
                        _clickable = false;
                        UpdateVisuals();

                        OnRadioClicked?.Invoke();
                    }
                }
            }
            // 🔥 방법 2: OVRInput 사용 (fallback)
            else
            {
                Vector3 pos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
                Quaternion rot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

                // ⚠️ 로컬 좌표를 월드 좌표로 변환 필요
                // OVRCameraRig의 TrackingSpace를 찾아서 변환
                Transform trackingSpace = FindObjectOfType<OVRCameraRig>()?.trackingSpace;
                if (trackingSpace)
                {
                    pos = trackingSpace.TransformPoint(pos);
                    rot = trackingSpace.rotation * rot;

                    Ray ray = new Ray(pos, rot * Vector3.forward);

                    if (Physics.Raycast(ray, out RaycastHit hit, 2f))
                    {
                        UnityEngine.Debug.Log($"[RadioClickable] VR Raycast hit: {hit.collider.gameObject.name}");

                        if (hit.collider.gameObject == gameObject)
                        {
                            UnityEngine.Debug.Log("[RadioClickable] Radio clicked via VR!");

                            // 🔥 클릭 후 즉시 비활성화 (1회만 클릭)
                            _clickable = false;
                            UpdateVisuals();

                            OnRadioClicked?.Invoke();
                        }
                    }
                }
            }
        }
    }
}
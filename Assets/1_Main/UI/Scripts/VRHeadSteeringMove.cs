using UnityEngine;

// [변경점] 이제 이동 기능은 없고, 현실의 몸 움직임(충돌체)만 따라갑니다.
[RequireComponent(typeof(CharacterController))]
public class VRHeadSteeringMove : MonoBehaviour
{
    [Header("필수 참조")]
    public Transform cameraTransform; // CenterEyeAnchor

    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            // OVRCameraRig가 있다면 자동으로 찾기
            var rig = GetComponent<OVRCameraRig>();
            if (rig != null)
                cameraTransform = rig.centerEyeAnchor;
        }
    }

    void Update()
    {
        // 핵심: 조이스틱 이동 코드(Move)를 모두 삭제했습니다.
        // 오직 현실 몸 위치 동기화만 수행합니다.
        SyncCharacterController();
    }

    private void SyncCharacterController()
    {
        if (cameraTransform == null) return;

        // 현실의 내 머리 위치에 맞춰 충돌체(몸통)만 따라오게 함 (벽 뚫기 방지용)
        Vector3 centerEyeLocalPos = cameraTransform.localPosition;
        characterController.center = new Vector3(centerEyeLocalPos.x, characterController.center.y, centerEyeLocalPos.z);
    }
}
using UnityEngine;

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
            // 메인 카메라(눈)를 자동으로 찾음
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        SyncCharacterController();
    }

    private void SyncCharacterController()
    {
        if (cameraTransform == null) return;

        // 내 머리 위치(x, z)에 맞춰 몸통(Collider)을 이동시킴
        Vector3 centerEyeLocalPos = cameraTransform.localPosition;
        characterController.center = new Vector3(centerEyeLocalPos.x, characterController.center.y, centerEyeLocalPos.z);
    }
}
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class VRHeadSteeringMove : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 3.0f;
    public float gravity = -9.81f;

    [Header("필수 참조")]
    public Transform cameraTransform; // CenterEyeAnchor (내 눈)

    private CharacterController characterController;
    private Vector3 playerVelocity;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // 카메라가 할당 안 되어 있으면 자동으로 찾기
        if (cameraTransform == null)
        {
            cameraTransform = GetComponent<OVRCameraRig>().centerEyeAnchor;
        }
    }

    void Update()
    {
        // 1. 캐릭터 컨트롤러 위치 동기화 (현실 몸 움직임 반영)
        SyncCharacterController();

        // 2. 오른손 스틱 입력 받기 (이동 전용)
        Vector2 moveInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick); 

        // 3. 이동 방향 계산 (항상 카메라가 보는 방향이 '앞'이 됨)
        Vector3 moveDirection = CalculateMoveDirection(moveInput);

        // 4. 이동 실행
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // 5. 중력 적용
        ApplyGravity();
    }

    // 카메라가 보는 방향을 기준으로 '앞/뒤/좌/우'를 결정함
    private Vector3 CalculateMoveDirection(Vector2 input)
    {
        // 입력이 없으면 계산 중지
        if (input.magnitude < 0.1f) return Vector3.zero;

        // 카메라의 정면과 오른쪽 방향을 가져옴
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // 하늘을 봐도 위로 뜨지 않게 Y축을 0으로 고정 (평지 이동)
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // 스틱을 앞으로 밀면(y) 카메라 정면(forward)으로, 
        // 스틱을 옆으로 밀면(x) 카메라 오른쪽(right)으로 이동
        return forward * input.y + right * input.x;
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    private void SyncCharacterController()
    {
        // 현실의 내 머리 위치에 맞춰 충돌체(몸통)도 따라오게 함
        Vector3 centerEyeLocalPos = cameraTransform.localPosition;
        characterController.center = new Vector3(centerEyeLocalPos.x, characterController.center.y, centerEyeLocalPos.z);
    }
}
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// 이 스크립트를 추가하면 'Outline' 컴포넌트가 자동으로 같이 추가됩니다.
[RequireComponent(typeof(Outline))]
public class Simple3DClicker : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Click Event")]
    public UnityEvent onClick; // 클릭 시 실행할 함수

    [Header("Highlight Settings")]
    [Tooltip("이 거리 안으로 플레이어가 들어오면 하이라이트가 켜집니다.")]
    public float highlightDistance = 3.0f; 
    [Range(0f, 10f)] public float outlineWidth = 4f; // 아웃라인 두께

    [Header("Colors")]
    public Color hoverColor = Color.yellow;      // 레이저를 올렸을 때 색상
    public Color proximityColor = Color.white;   // 가까이 다가갔을 때 색상

    private Outline outline;
    private Transform playerCameraTransform;
    private bool isHovered = false; // 현재 레이저가 올라와 있는지 여부

    void Start()
    {
        // Outline 컴포넌트 가져오기 및 초기 설정
        outline = GetComponent<Outline>();
        outline.enabled = false; // 처음엔 끔
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineWidth = outlineWidth;

        // 플레이어의 눈(카메라) 위치 찾기
        if (Camera.main != null)
        {
            playerCameraTransform = Camera.main.transform;
        }
        else
        {
            // OVR 사용 시 간혹 Camera.main을 못 찾으면 태그를 확인해주세요.
            // 보통 CenterEyeAnchor에 MainCamera 태그가 붙어있어야 합니다.
            Debug.LogWarning("[Simple3DClicker] 메인 카메라를 찾을 수 없습니다. 거리 감지 기능이 제한됩니다.");
        }
    }

    void Update()
    {
        // 플레이어 카메라가 없거나, 이미 레이저가 올라와 있다면 거리 체크를 건너뜁니다.
        if (playerCameraTransform == null || isHovered) return;

        // 플레이어와의 거리 계산 (성능을 위해 제곱 거리 사용)
        float sqrDistance = (playerCameraTransform.position - transform.position).sqrMagnitude;
        float sqrHighlightDist = highlightDistance * highlightDistance;

        if (sqrDistance <= sqrHighlightDist)
        {
            // 거리 안에 들어옴 -> 근접 색상으로 켜기
            if (!outline.enabled || outline.OutlineColor != proximityColor)
            {
                outline.OutlineColor = proximityColor;
                outline.enabled = true;
            }
        }
        else
        {
            // 거리 밖으로 나감 -> 끄기
            if (outline.enabled)
            {
                outline.enabled = false;
            }
        }
    }

    // === 포인터 이벤트 ===

    // 클릭했을 때
    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }

    // 레이저가 올라왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        outline.OutlineColor = hoverColor; // 호버 색상 적용
        outline.enabled = true;            // 무조건 켬
    }

    // 레이저가 나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        // 여기서 바로 끄지 않습니다.
        // 만약 플레이어가 가까이 있다면 Update문에서 계속 켜진 상태를 유지해줄 것입니다.
    }

    // 인스펙터에서 값을 바꾸면 실시간으로 두께 반영 (편의 기능)
    void OnValidate()
    {
        if (outline != null)
        {
            outline.OutlineWidth = outlineWidth;
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 문 오브젝트 인터랙션 전용:
/// - 포인터 진입/이탈 시 시각 피드백(Outline/Material)
/// - 클릭 시 AppSceneFlow로 위임 (씬 로드 금지)
/// </summary>
public class DoorInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Flow (Required)")]
    [Tooltip("씬 전환은 반드시 AppSceneFlow로 위임")]
    public AppSceneFlow flow;

    [Header("Visual Feedback (optional)")]
    [Tooltip("호버 시 적용할 머티리얼(옵션)")]
    public Material hoverMaterial;

    private Material _defaultMaterial;
    private Renderer _renderer;
    private Behaviour _outline; // Outline 타입이 프로젝트마다 다를 수 있어 Behaviour로 받음

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null) _defaultMaterial = _renderer.material;

        // Outline 컴포넌트가 있으면 비활성화로 시작
        _outline = GetComponent<Behaviour>(); // 가장 단순 안전 방식(Outline이 Behaviour 상속)
        // 만약 Outline이 아닌 다른 Behaviour가 잡힐 수 있으면 아래처럼 정확히 바꿔:
        // _outline = GetComponent<Outline>();
        if (_outline != null) _outline.enabled = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_outline != null) _outline.enabled = true;

        if (_renderer != null && hoverMaterial != null)
            _renderer.material = hoverMaterial;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_outline != null) _outline.enabled = false;

        if (_renderer != null && _defaultMaterial != null)
            _renderer.material = _defaultMaterial;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (flow == null)
        {
            Debug.LogError("[DoorInteractable] flow(AppSceneFlow) not assigned.");
            return;
        }

        flow.GoToStage();
    }
}

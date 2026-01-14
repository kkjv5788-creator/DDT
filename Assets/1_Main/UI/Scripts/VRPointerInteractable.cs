using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class VRPointerInteractable : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Visual Feedback (optional)")]
    [Tooltip("호버 시 적용할 머티리얼(옵션)")]
    public Material hoverMaterial;

    [Tooltip("Outline 같은 컴포넌트가 있으면 켜짐(옵션)")]
    public Behaviour outlineBehaviour;

    [Header("Events")]
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;
    public UnityEvent onClick;

    private Renderer _renderer;
    private Material _defaultMaterial;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null) _defaultMaterial = _renderer.material;

        if (outlineBehaviour == null)
        {
            // 필요하면 여기서 GetComponent<Outline>()로 명시적으로 바꿔도 됨
            outlineBehaviour = null;
        }

        if (outlineBehaviour != null) outlineBehaviour.enabled = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outlineBehaviour != null) outlineBehaviour.enabled = true;

        if (_renderer != null && hoverMaterial != null)
            _renderer.material = hoverMaterial;

        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (outlineBehaviour != null) outlineBehaviour.enabled = false;

        if (_renderer != null && _defaultMaterial != null)
            _renderer.material = _defaultMaterial;

        onHoverExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }
}

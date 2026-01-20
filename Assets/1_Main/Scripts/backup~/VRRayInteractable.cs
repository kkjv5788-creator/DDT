using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class VRRayInteractable : MonoBehaviour, IVRRayInteractable
{
    [Header("Interactable")]
    [SerializeField] private bool interactable = true;
    public bool IsInteractable => interactable;

    [Header("Visual Feedback (optional)")]
    [Tooltip("호버 시 적용할 머티리얼(옵션)")]
    [SerializeField] private Material hoverMaterial;

    [Tooltip("Outline(또는 enabled 토글 가능한 Behaviour)")]
    [SerializeField] private Behaviour outlineBehaviour;

    [Header("Events")]
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;
    public UnityEvent onClick;

    private Renderer _renderer;
    private Material _defaultMaterial;
    private bool _hovering;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>(true);
        if (_renderer != null) _defaultMaterial = _renderer.material;

        if (outlineBehaviour != null) outlineBehaviour.enabled = false;
    }

    public void OnRayHoverEnter()
    {
        if (!interactable) return;
        if (_hovering) return;
        _hovering = true;

        if (outlineBehaviour != null) outlineBehaviour.enabled = true;

        if (_renderer != null && hoverMaterial != null)
            _renderer.material = hoverMaterial;

        onHoverEnter?.Invoke();
    }

    public void OnRayHoverExit()
    {
        if (!_hovering) return;
        _hovering = false;

        if (outlineBehaviour != null) outlineBehaviour.enabled = false;

        if (_renderer != null && _defaultMaterial != null)
            _renderer.material = _defaultMaterial;

        onHoverExit?.Invoke();
    }

    public void OnRayClick()
    {
        if (!interactable) return;
        onClick?.Invoke();
    }

    public void SetInteractable(bool value)
{
    interactable = value;
    if (!interactable)
        OnRayHoverExit(); // 꺼질 때 피드백(Outline/Material) 정리
}

}

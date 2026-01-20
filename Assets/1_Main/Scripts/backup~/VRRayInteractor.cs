using UnityEngine;

[DisallowMultipleComponent]
public class VRRayInteractor : MonoBehaviour
{
    [Header("Ray Origin")]
    [Tooltip("추천: 컨트롤러 모델의 *_laser_begin. 없으면 HandAnchor")]
    public Transform rayOrigin;

    [Header("Raycast")]
    public float maxDistance = 8f;
    public LayerMask interactableMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Laser Visual (optional)")]
    public LaserLineDriver laserVisual;

    [Header("Input")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    public OVRInput.Button clickButton = OVRInput.Button.PrimaryIndexTrigger;

    private IVRRayInteractable _current;
    private float _currentDistance;
    private bool _hit;

    private void Reset()
    {
        rayOrigin = transform;
    }

    private void Update()
    {
        if (!rayOrigin) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        _hit = Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableMask, triggerInteraction);

        IVRRayInteractable next = null;
        float nextDist = maxDistance;

        if (_hit)
        {
            nextDist = hit.distance;
            // Collider에 붙어있든, 부모에 붙어있든 다 찾음
            next = hit.collider.GetComponentInParent<IVRRayInteractable>();
            if (next != null && !next.IsInteractable) next = null;
        }

        if (!ReferenceEquals(next, _current))
        {
            _current?.OnRayHoverExit();
            _current = next;
            _current?.OnRayHoverEnter();
        }

        _currentDistance = nextDist;

        if (_current != null && OVRInput.GetDown(clickButton, controller))
        {
            _current.OnRayClick();
        }

        if (laserVisual != null)
        {
            var state = (_current != null) ? LaserLineDriver.VisualState.Hover : LaserLineDriver.VisualState.Idle;
            laserVisual.Apply(rayOrigin, _currentDistance, state);
        }
    }
}

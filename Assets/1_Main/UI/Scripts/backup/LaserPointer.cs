using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserPointer : MonoBehaviour
{
    [Header("Ray")]
    public float rayDistance = 20f;
    public LayerMask interactableLayer = ~0; // 기본: Everything

    [Header("Input")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    public OVRInput.Button clickButton = OVRInput.Button.PrimaryIndexTrigger;

    private LineRenderer line;
    private DoorTrigger currentTrigger;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    void Update()
    {
        Vector3 start = transform.position;
        Vector3 dir   = transform.forward;

        line.SetPosition(0, start);

        if (Physics.Raycast(start, dir, out RaycastHit hit, rayDistance, interactableLayer))
        {
            line.SetPosition(1, hit.point);

            DoorTrigger trigger = hit.collider.GetComponent<DoorTrigger>();

            if (trigger != currentTrigger)
            {
                ClearCurrent();
                currentTrigger = trigger;
                if (currentTrigger != null)
                    currentTrigger.OnPointerEnter();
            }

            if (currentTrigger != null &&
                OVRInput.GetDown(clickButton, controller))
            {
                currentTrigger.OnPointerClick();
            }
        }
        else
        {
            line.SetPosition(1, start + dir * rayDistance);
            ClearCurrent();
        }
    }

    private void ClearCurrent()
    {
        if (currentTrigger != null)
        {
            currentTrigger.OnPointerExit();
            currentTrigger = null;
        }
    }
}

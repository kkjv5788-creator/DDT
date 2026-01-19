using UnityEngine;
using UnityEngine.Events;

public class RadioClickable : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnRadioClicked;

    [Header("Pointer Settings")]
    public Transform knifePointerSource;
    public float pointerMaxDistance = 3f;
    public LayerMask radioLayer;

    [Tooltip("VR에서 클릭 안정성을 위해 Ray 대신 SphereCast 사용")]
    public bool useSphereCast = true;
    public float sphereRadius = 0.04f;
    public float originForwardOffset = 0.02f;

    [Header("Visual Feedback")]
    public Renderer radioRenderer;
    public Material normalMaterial;      // 클릭 불가 상태 (튜토리얼 중)
    public Material outlineMaterial;     // 클릭 가능 상태 기본 (아웃라인만)

    [Header("Highlight (포인터 겨냥 시)")]
    public Color highlightEmission = new Color(1f, 0.9f, 0.5f);
    public float highlightIntensity = 2f;

    [Header("Pointer Line")]
    public LineRenderer pointerLine;
    public Color pointerColorNormal = Color.yellow;
    public Color pointerColorOnTarget = Color.green;

    [Header("Sounds (Optional)")]
    public AudioSource hoverSound;

    bool _clickable = false;
    bool _isPointing = false;
    bool _wasPointing = false;
    bool _tutorialCompleted = false;

    Material _runtimeMaterial;

    void Start()
    {
        // 런타임 머티리얼 생성
        if (radioRenderer && normalMaterial)
        {
            _runtimeMaterial = new Material(normalMaterial);
            radioRenderer.material = _runtimeMaterial;
        }

        UpdateVisuals(false, false);

        if (pointerLine)
            pointerLine.enabled = false;
    }

    void Update()
    {
        if (!_clickable || !_tutorialCompleted)
        {
            _isPointing = false;
            UpdateVisuals(false, false);
            if (pointerLine) pointerLine.enabled = false;
            return;
        }

        CheckPointer();

        // 🔥 디버그: 오른손 인덱스 트리거 입력 확인
        bool triggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        if (triggerDown)
        {
            Debug.Log($"[RadioClickable] Right trigger down. _isPointing={_isPointing}");
        }

        // 포인터가 라디오를 가리킬 때만 인덱스 트리거로 클릭
        if (_isPointing && triggerDown)
        {
            Debug.Log("[RadioClickable] Trigger on radio target - calling OnRadioClick");
            OnRadioClick();
        }
    }

    void CheckPointer()
    {
        _isPointing = false;
        if (!knifePointerSource) return;

        Vector3 origin = knifePointerSource.position + knifePointerSource.forward * originForwardOffset;
        Vector3 dir = knifePointerSource.forward;

        bool hitSomething = false;
        RaycastHit hit;

        if (useSphereCast)
        {
            hitSomething = Physics.SphereCast(
                origin, sphereRadius, dir,
                out hit, pointerMaxDistance, radioLayer,
                QueryTriggerInteraction.Collide
            );
        }
        else
        {
            hitSomething = Physics.Raycast(
                origin, dir,
                out hit, pointerMaxDistance, radioLayer,
                QueryTriggerInteraction.Collide
            );
        }

        if (hitSomething)
        {
            var rc = hit.collider.GetComponentInParent<RadioClickable>();
            if (rc == this)
            {
                _isPointing = true;
                if (!_wasPointing)
                {
                    Debug.Log("[RadioClickable] Pointer entered radio");
                }

                if (pointerLine)
                {
                    pointerLine.enabled = true;
                    pointerLine.SetPosition(0, origin);
                    pointerLine.SetPosition(1, hit.point);
                    pointerLine.startColor = pointerColorOnTarget;
                    pointerLine.endColor = pointerColorOnTarget;
                }

                if (!_wasPointing && hoverSound)
                    hoverSound.PlayOneShot(hoverSound.clip);
            }
        }
        else
        {
            if (_wasPointing)
            {
                Debug.Log("[RadioClickable] Pointer left radio");
            }
        }

        // 빗나갔을 때 라인 표시
        if (!_isPointing && pointerLine && _clickable)
        {
            pointerLine.enabled = true;
            pointerLine.SetPosition(0, origin);
            pointerLine.SetPosition(1, origin + dir * pointerMaxDistance);
            pointerLine.startColor = pointerColorNormal;
            pointerLine.endColor = pointerColorNormal;
        }
        else if (!_isPointing && pointerLine)
        {
            pointerLine.enabled = false;
        }

        UpdateVisuals(_clickable, _isPointing);
        _wasPointing = _isPointing;
    }

    void UpdateVisuals(bool clickable, bool pointing)
    {
        if (!radioRenderer || !_runtimeMaterial) return;

        if (!clickable)
        {
            // 🔴 클릭 불가 (튜토리얼 중): normalMaterial, Emission OFF
            if (normalMaterial && radioRenderer.sharedMaterial != normalMaterial)
            {
                radioRenderer.material = new Material(normalMaterial);
                _runtimeMaterial = radioRenderer.material;
            }

            _runtimeMaterial.DisableKeyword("_EMISSION");
            _runtimeMaterial.SetColor("_EmissionColor", Color.black);
        }
        else if (clickable && !pointing)
        {
            // 🟡 클릭 가능 + 포인터 안 맞음: outlineMaterial, Emission OFF
            if (outlineMaterial && radioRenderer.sharedMaterial != outlineMaterial)
            {
                radioRenderer.material = new Material(outlineMaterial);
                _runtimeMaterial = radioRenderer.material;
            }

            _runtimeMaterial.DisableKeyword("_EMISSION");
            _runtimeMaterial.SetColor("_EmissionColor", Color.black);
        }
        else if (clickable && pointing)
        {
            // 🟢 클릭 가능 + 포인터 맞음: outlineMaterial + Emission ON
            if (outlineMaterial && radioRenderer.sharedMaterial != outlineMaterial)
            {
                radioRenderer.material = new Material(outlineMaterial);
                _runtimeMaterial = radioRenderer.material;
            }

            _runtimeMaterial.EnableKeyword("_EMISSION");
            _runtimeMaterial.SetColor("_EmissionColor", highlightEmission * highlightIntensity);
        }
    }

    void OnRadioClick()
    {
        Debug.Log("[RadioClickable] Radio clicked via pointer!");

        _clickable = false;
        _isPointing = false;

        if (pointerLine) pointerLine.enabled = false;
        UpdateVisuals(false, false);

        OnRadioClicked?.Invoke();
    }

    public void SetClickable(bool clickable)
    {
        if (_clickable == clickable) return;

        _clickable = clickable;
        Debug.Log($"[RadioClickable] Clickable: {clickable}");

        if (!clickable)
        {
            _isPointing = false;
            if (pointerLine) pointerLine.enabled = false;
        }

        UpdateVisuals(_clickable, false);
    }

    public void SetTutorialCompleted(bool completed)
    {
        _tutorialCompleted = completed;
        Debug.Log($"[RadioClickable] Tutorial Completed: {completed}");

        if (!_tutorialCompleted)
        {
            _isPointing = false;
            _wasPointing = false;
            if (pointerLine) pointerLine.enabled = false;
        }

        UpdateVisuals(_clickable, false);
    }

    void OnDestroy()
    {
        if (_runtimeMaterial)
        {
            Destroy(_runtimeMaterial);
        }
    }

    void OnDrawGizmos()
    {
        if (!knifePointerSource) return;

        Gizmos.color = _isPointing ? Color.green : Color.yellow;
        Vector3 origin = knifePointerSource.position + knifePointerSource.forward * originForwardOffset;
        Gizmos.DrawRay(origin, knifePointerSource.forward * pointerMaxDistance);

        if (useSphereCast)
        {
            Gizmos.DrawWireSphere(origin + knifePointerSource.forward * 0.15f, sphereRadius);
        }
    }
}
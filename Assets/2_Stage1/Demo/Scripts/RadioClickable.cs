using System.Diagnostics;
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

    [Tooltip("VR에서 클릭 안정성을 위해 Ray 대신 SphereCast 사용(추천)")]
    public bool useSphereCast = true;
    public float sphereRadius = 0.04f;
    public float originForwardOffset = 0.02f;

    [Header("QuickOutline 설정")]
    public Outline outline; // QuickOutline 컴포넌트 참조

    [Tooltip("클릭 전 아웃라인 색상 (항상 표시)")]
    public Color outlineColorNormal = new Color(1f, 0.9f, 0.3f); // 부드러운 노란색

    [Tooltip("클릭 가능할 때 아웃라인 색상")]
    public Color outlineColorClickable = new Color(0.3f, 0.9f, 1f); // 부드러운 청록색

    [Tooltip("포인터가 가리킬 때 아웃라인 색상")]
    public Color outlineColorPointing = new Color(0.3f, 1f, 0.3f); // 부드러운 초록색

    [Tooltip("아웃라인 두께 (2-4 추천)")]
    public float outlineWidth = 3f;

    [Header("Emission 하이라이트")]
    public Renderer radioRenderer;
    public Color highlightEmission = new Color(1f, 0.9f, 0.5f);

    [Tooltip("포인터 겨냥 시 하이라이트 강도 (0.5-2.0 추천)")]
    public float highlightIntensity = 1.2f;

    [Tooltip("클릭 가능 상태일 때 은은하게 빛나게 할지")]
    public bool glowWhenClickable = true;

    [Tooltip("클릭 가능 시 하이라이트 배율 (0.1-0.3 추천)")]
    public float clickableGlowMultiplier = 0.15f;

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
        // QuickOutline 초기 설정
        if (outline)
        {
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = outlineColorNormal;
            outline.OutlineWidth = outlineWidth;
            outline.enabled = true;
        }

        // Emission용 런타임 머티리얼 생성
        if (radioRenderer)
        {
            _runtimeMaterial = new Material(radioRenderer.sharedMaterial);
            radioRenderer.material = _runtimeMaterial;

            // 초기 Emission 끄기
            if (_runtimeMaterial.HasProperty("_EmissionColor"))
            {
                _runtimeMaterial.DisableKeyword("_EMISSION");
                _runtimeMaterial.SetColor("_EmissionColor", Color.black);
            }
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

        if (_isPointing && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
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

        if (!_isPointing && pointerLine)
        {
            pointerLine.enabled = true;
            pointerLine.SetPosition(0, origin);
            pointerLine.SetPosition(1, origin + dir * pointerMaxDistance);
            pointerLine.startColor = pointerColorNormal;
            pointerLine.endColor = pointerColorNormal;
        }

        UpdateVisuals(_clickable, _isPointing);
        _wasPointing = _isPointing;
    }

    void UpdateVisuals(bool clickable, bool pointing)
    {
        // 1. QuickOutline 색상 업데이트
        if (outline)
        {
            if (pointing)
            {
                // 포인터가 가리킬 때: 부드러운 초록색
                outline.OutlineColor = outlineColorPointing;
            }
            else if (clickable)
            {
                // 클릭 가능할 때: 부드러운 청록색
                outline.OutlineColor = outlineColorClickable;
            }
            else
            {
                // 기본 상태: 부드러운 노란색
                outline.OutlineColor = outlineColorNormal;
            }

            outline.enabled = true;
        }

        // 2. Emission 하이라이트 업데이트 (매우 은은하게)
        if (!radioRenderer || !_runtimeMaterial) return;
        if (!_runtimeMaterial.HasProperty("_EmissionColor")) return;

        // 기본: emission off
        _runtimeMaterial.DisableKeyword("_EMISSION");
        _runtimeMaterial.SetColor("_EmissionColor", Color.black);

        if (!clickable)
        {
            // 클릭 불가: 하이라이트 없음
            return;
        }

        // 클릭 가능
        if (!pointing)
        {
            // 클릭 가능하지만 겨냥 안 함: 매우 은은한 빛
            if (glowWhenClickable && clickableGlowMultiplier > 0f)
            {
                _runtimeMaterial.EnableKeyword("_EMISSION");
                _runtimeMaterial.SetColor("_EmissionColor",
                    highlightEmission * (highlightIntensity * clickableGlowMultiplier));
            }
            return;
        }

        // 클릭 가능 + 겨냥함: 조금 더 강한 하이라이트 (하지만 과하지 않게)
        _runtimeMaterial.EnableKeyword("_EMISSION");
        _runtimeMaterial.SetColor("_EmissionColor", highlightEmission * highlightIntensity);
    }

    void OnRadioClick()
    {
        UnityEngine.Debug.Log("[RadioClickable] Radio clicked via pointer!");

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

        if (!clickable)
        {
            _isPointing = false;
            if (pointerLine) pointerLine.enabled = false;
            UpdateVisuals(false, false);
        }
        else
        {
            UpdateVisuals(true, false);
        }
    }

    public void SetTutorialCompleted(bool completed)
    {
        _tutorialCompleted = completed;

        if (!_tutorialCompleted)
        {
            _isPointing = false;
            _wasPointing = false;
            if (pointerLine) pointerLine.enabled = false;
            UpdateVisuals(false, false);
        }
        else
        {
            UpdateVisuals(_clickable, false);
        }
    }

    void OnDestroy()
    {
        if (_runtimeMaterial) Destroy(_runtimeMaterial);
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
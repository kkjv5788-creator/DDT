using UnityEngine;
using UnityEngine.Events;

public class RadioClickable : MonoBehaviour

{
    [Header("Events")]
    public UnityEvent OnRadioClicked;

    [Header("Pointer Settings")]
    public Transform knifePointerSource; // 실제로는 RightHandAnchor 아래 'RadioPointer' 추천
    public float pointerMaxDistance = 3f;
    public LayerMask radioLayer;

    [Tooltip("VR에서 클릭 안정성을 위해 Ray 대신 SphereCast 사용(추천)")]
    public bool useSphereCast = true;
    public float sphereRadius = 0.04f;         // 4cm 정도(필요시 0.03~0.06 조절)
    public float originForwardOffset = 0.02f;   // 시작점이 손/칼 콜라이더에 묻히는 것 방지

    [Header("Visual Feedback")]
    public Renderer radioRenderer;
    public Material normalMaterial;
    public Material outlineMaterial;

    [Header("Highlight")]
    public Color highlightEmission = new Color(1f, 0.9f, 0.5f);
    public float highlightIntensity = 2f;

    [Tooltip("클릭 가능 상태일 때(포인터 안 맞아도) 은은하게 빛나게 할지")]
    public bool glowWhenClickable = true;
    public float clickableGlowMultiplier = 0.35f; // 0이면 '클릭 가능해도 아웃라인만'

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

    Material _runtimeNormal;
    Material _runtimeOutline;

    void Start()
    {
        // 런타임 인스턴스 생성(머티리얼 누수/하이라이트 적용 문제 해결)
        if (radioRenderer)
        {
            if (normalMaterial) _runtimeNormal = new Material(normalMaterial);
            if (outlineMaterial) _runtimeOutline = new Material(outlineMaterial);

            // "클릭 전엔 아웃라인만"이므로 초기엔 아웃라인 적용
            if (_runtimeOutline) radioRenderer.material = _runtimeOutline;
            else if (_runtimeNormal) radioRenderer.material = _runtimeNormal;
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

        // 포인터가 라디오를 가리킬 때만 A 버튼으로 클릭
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
            // ⭐ 핵심: 자식 콜라이더여도 "이 라디오"면 인정
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

        // 빗나갔을 때 라인 표시(원하면 유지, 싫으면 else에서 꺼도 됨)
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
        if (!radioRenderer) return;

        // "클릭 전엔 아웃라인만": clickable 여부와 무관하게 기본은 아웃라인 사용
        Material targetMat = _runtimeOutline ? _runtimeOutline : (_runtimeNormal ? _runtimeNormal : radioRenderer.material);
        if (radioRenderer.material != targetMat)
            radioRenderer.material = targetMat;

        // emission 제어는 "현재 renderer.material"에 직접 적용 (머티리얼 스왑해도 OK)
        Material mat = radioRenderer.material;
        if (!mat) return;

        // 기본: emission off
        mat.DisableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);

        if (!clickable)
        {
            // 클릭 불가: 아웃라인만 (하이라이트 없음)
            return;
        }

        // 클릭 가능
        if (!pointing)
        {
            // 클릭 가능하지만 겨냥 안 함
            if (glowWhenClickable && clickableGlowMultiplier > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", highlightEmission * (highlightIntensity * clickableGlowMultiplier));
            }
            return;
        }

        // 클릭 가능 + 겨냥함: 강한 하이라이트
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", highlightEmission * highlightIntensity);
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
        // ⭐ RadioOutlineController가 매 프레임 호출하더라도 중복 처리 방지
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
            // 클릭 가능이 된 순간에도 "클릭 가능 하이라이트"를 보여주고 싶다면 여기서 갱신
            UpdateVisuals(true, false);
        }
    }

    void OnDestroy()
    {
        if (_runtimeNormal) Destroy(_runtimeNormal);
        if (_runtimeOutline) Destroy(_runtimeOutline);
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

}

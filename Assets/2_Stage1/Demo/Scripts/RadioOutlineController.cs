using UnityEngine;

public class RadioOutlineController : MonoBehaviour
{
    [Header("Refs")]
    public GameFlowManager gameFlowManager;
    public Renderer radioRenderer;

    [Header("Outline Settings")]
    public Material outlineMaterial;      // Outline 머티리얼
    public Material normalMaterial;       // 기본 머티리얼
    public Color emissionColor = Color.yellow;
    public float emissionIntensity = 2f;

    Material _currentMaterial;
    bool _isHighlighted;

    void Start()
    {
        if (radioRenderer)
        {
            _currentMaterial = radioRenderer.material;
        }

        // 초기 상태: OFF
        SetHighlight(false);
    }

    void Update()
    {
        if (!gameFlowManager) return;

        // WaitForRadio 상태에서만 하이라이트
        bool shouldHighlight = (gameFlowManager.CurrentState == GameState.WaitForRadio);

        if (shouldHighlight != _isHighlighted)
        {
            SetHighlight(shouldHighlight);
        }
    }

    void SetHighlight(bool enabled)
    {
        _isHighlighted = enabled;

        if (!radioRenderer) return;

        if (enabled)
        {
            // Outline + Emission
            if (outlineMaterial)
            {
                radioRenderer.material = outlineMaterial;
            }

            // Emission 설정
            if (radioRenderer.material.HasProperty("_EmissionColor"))
            {
                radioRenderer.material.EnableKeyword("_EMISSION");
                radioRenderer.material.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            }

            Debug.Log("[RadioOutline] Highlight ON");
        }
        else
        {
            // 기본 머티리얼
            if (normalMaterial)
            {
                radioRenderer.material = normalMaterial;
            }

            // Emission 끄기
            if (radioRenderer.material.HasProperty("_EmissionColor"))
            {
                radioRenderer.material.DisableKeyword("_EMISSION");
            }

            Debug.Log("[RadioOutline] Highlight OFF");
        }
    }
}
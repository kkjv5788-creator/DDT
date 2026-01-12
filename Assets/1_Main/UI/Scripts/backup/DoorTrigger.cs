using UnityEngine;
using UnityEngine.EventSystems; // 필수: 마우스/레이저 감지 인터페이스
using UnityEngine.SceneManagement; // 필수: 씬 이동

public class DoorTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Material Settings")]
    [Tooltip("레이저가 닿았을 때 변경될 밝은 머티리얼")]
    public Material activeMaterial;
    
    // 내부 변수
    private Material defaultMaterial; // 원래 입혀져 있던 머티리얼
    private Renderer myRenderer;
    private Outline myOutline; 

    void Start()
    {
        // 1. 컴포넌트 가져오기
        myOutline = GetComponent<Outline>();
        myRenderer = GetComponent<Renderer>();

        // 2. 렌더러가 있다면 원래 머티리얼을 기억해둡니다 (나중에 되돌리기 위해)
        if (myRenderer != null)
        {
            defaultMaterial = myRenderer.material;
        }

        // 3. 아웃라인 초기화
        // (요청하신 대로 '항상 켜져있게' 하려면 아래 if문을 지우거나 enabled = true로 두세요)
        // 현재 코드는 제공해주신 로직(평소엔 꺼짐)을 따릅니다.
        if (myOutline != null)
        {
            myOutline.enabled = true; // "아웃라인은 항상 켜져있고" 요청 반영 시 true, 아니면 false
        }
    }

    // 👉 레이저가 문에 닿았을 때 (Hover)
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 아웃라인 켜기 (필요 시)
        // if (myOutline != null) myOutline.enabled = true; 

        // 머티리얼 교체 -> Active
        if (myRenderer != null && activeMaterial != null)
        {
            myRenderer.material = activeMaterial;
        }
    }

    // 👉 레이저가 문에서 벗어났을 때 (Exit)
    public void OnPointerExit(PointerEventData eventData)
    {
        // 아웃라인 끄기 (필요 시)
        // if (myOutline != null) myOutline.enabled = false;

        // 머티리얼 복구 -> Default
        if (myRenderer != null && defaultMaterial != null)
        {
            myRenderer.material = defaultMaterial;
        }
    }

    // 👉 문을 클릭했을 때 (Click)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("문 클릭! Stage1으로 이동합니다.");
        SceneManager.LoadScene("Stage1");
    }
}
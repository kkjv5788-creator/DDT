using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("설정")]
    public Transform playerTransform; // 꼭 CenterEyeAnchor를 넣으세요
    public float interactionDistance = 5.0f; // 인식 거리

    [Header("머티리얼")]
    public Material activeMaterial;
    
    private Material defaultMaterial;
    private Renderer myRenderer;
    private Outline myOutline;

    void Start()
    {
        myRenderer = GetComponent<Renderer>();
        myOutline = GetComponent<Outline>();

        // 1. 시작할 때 렌더러가 가지고 있는 재질을 '기본'으로 저장함
        // (주의: 인스펙터에서 미리 '어두운 재질'을 넣어놔야 함!)
        if (myRenderer != null)
        {
            defaultMaterial = myRenderer.material;
        }

        // 플레이어 없으면 메인카메라 자동 찾기
        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 실시간 거리 디버깅 (콘솔창을 확인하세요!)
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        
        // 거리가 가까우면 아웃라인 켜기
        if (dist <= interactionDistance)
        {
            if (myOutline != null) myOutline.enabled = true;
        }
        else
        {
            if (myOutline != null) myOutline.enabled = false;
            // 멀어지면 머티리얼도 강제로 끄기
            if (myRenderer != null) myRenderer.material = defaultMaterial;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 가까울 때만 반응
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist > interactionDistance) return;

        if (myRenderer != null) myRenderer.material = activeMaterial;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (myRenderer != null) myRenderer.material = defaultMaterial;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 디버깅을 위해 거리 제한 없이 무조건 클릭되면 로그 뜨게 함
        Debug.Log("클릭 감지됨! 씬 이동 시도..."); 
        SceneManager.LoadScene("Stage1");
    }
}
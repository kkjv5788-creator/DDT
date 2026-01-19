using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]
public class SimpleLaserPointer : MonoBehaviour
{
    [Header("1. 레이저 설정")]
    public float maxDistance = 5.0f;       
    public float laserWidth = 0.005f;      

    [Header("2. 색상 알림")]
    public Color hoverColor = Color.green; // 닿았을 때 색상

    [Header("3. 사운드 & 진동")]
    public AudioClip hoverSound;    
    public AudioClip clickSound;    
    [Range(0, 1)] public float vibrationStrength = 0.2f; 

    private LineRenderer lr;
    private AudioSource audioSource;
    private OVRInput.Controller controllerType = OVRInput.Controller.None;
    private GameFlowManager gameFlowManager; // [추가] 게임 상태 확인용

    // --- 상태 관리 ---
    private GameObject currentHitObject;      
    private Renderer currentRenderer;         
    private Color originalObjectColor;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        audioSource = GetComponent<AudioSource>();

        // GameFlowManager 찾기
        gameFlowManager = FindObjectOfType<GameFlowManager>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = laserWidth;
        lr.endWidth = laserWidth;
        if (!lr.material) lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f; 

        // 시작 시 끄기
        DisableLaser();

        if (transform.name.Contains("Left") || transform.parent.name.Contains("Left"))
            controllerType = OVRInput.Controller.LTouch;
        else
            controllerType = OVRInput.Controller.RTouch;
    }

    private void OnDisable()
    {
        DisableLaser();
    }

    void LateUpdate()
    {
        if (!lr) return;

        // 🔥 [1] 게임 상태 체크 (핵심 기능)
        // 튜토리얼 중이거나 메인 게임 중일 때는 레이저를 아예 꺼버립니다.
        if (gameFlowManager != null)
        {
            bool isMenuState = 
                gameFlowManager.CurrentState == GameState.WaitForRadio || // 튜토리얼 끝나고 라디오 켤 때
                gameFlowManager.CurrentState == GameState.Paused ||       // 일시정지 메뉴
                gameFlowManager.CurrentState == GameState.FinalResult;    // 결과 화면

            if (!isMenuState)
            {
                DisableLaser();
                return; // 여기서 코드 종료 (레이저 안 나감)
            }
        }

        // --- 아래는 기존의 "스마트 레이저" 로직 (메뉴 상태일 때만 실행됨) ---

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        // 레이저 충돌 감지
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            GameObject hitObj = hit.collider.gameObject;
            
            // 상호작용 가능한 물체일 때만 레이저 표시
            if (IsInteractable(hitObj))
            {
                if (!lr.enabled) lr.enabled = true; // 레이저 켜기

                lr.SetPosition(0, transform.position); 
                lr.SetPosition(1, hit.point); 
                SetLaserColor(hoverColor);

                if (currentHitObject != hitObj)
                {
                    ResetObjectColor();
                    PlayFeedback(); 
                    currentHitObject = hitObj;
                    ChangeObjectColor(currentHitObject, hoverColor);
                }
            }
            else
            {
                DisableLaser(); // 버튼/라디오 아니면 숨김
            }
        }
        else
        {
            DisableLaser(); // 허공이면 숨김
        }

        // 트리거 클릭 입력
        if (lr.enabled && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controllerType))
        {
            if (clickSound) audioSource.PlayOneShot(clickSound);

            if (currentHitObject != null)
            {
                // UI 버튼 클릭
                var button = currentHitObject.GetComponentInParent<Button>();
                if (button != null) button.onClick.Invoke();

                // 일반 물체 클릭
                var clickHandler = currentHitObject.GetComponentInParent<IPointerClickHandler>();
                if (clickHandler != null)
                {
                    PointerEventData data = new PointerEventData(EventSystem.current);
                    clickHandler.OnPointerClick(data);
                }
                
                // 라디오 클릭 호환
                var radio = currentHitObject.GetComponentInParent<RadioClickable>();
                // (RadioClickable은 보통 IPointerClickHandler나 자체 로직으로 처리됨)
            }
        }
    }

    void DisableLaser()
    {
        if (lr.enabled)
        {
            lr.enabled = false;
            ResetObjectColor();
        }
    }

    // --- 기능 함수들 ---

    bool IsInteractable(GameObject obj)
    {
        if (obj.GetComponentInParent<Button>() != null) return true;
        if (obj.GetComponentInParent<IPointerClickHandler>() != null) return true;
        if (obj.GetComponentInParent<RadioClickable>() != null) return true;
        return false;
    }

    void ChangeObjectColor(GameObject obj, Color color)
    {
        currentRenderer = obj.GetComponent<Renderer>();
        if (currentRenderer == null) currentRenderer = obj.GetComponentInChildren<Renderer>();
        if (currentRenderer == null) currentRenderer = obj.GetComponentInParent<Renderer>();

        if (currentRenderer != null)
        {
            originalObjectColor = currentRenderer.material.color;
            currentRenderer.material.color = color;
        }
    }

    void ResetObjectColor()
    {
        if (currentHitObject != null && currentRenderer != null)
        {
            currentRenderer.material.color = originalObjectColor;
        }
        currentHitObject = null;
        currentRenderer = null;
    }

    void SetLaserColor(Color c)
    {
        lr.startColor = c;
        lr.endColor = c;
    }

    void PlayFeedback()
    {
        if (hoverSound) 
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f); 
            audioSource.PlayOneShot(hoverSound);
        }

        if (controllerType != OVRInput.Controller.None)
        {
            OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, controllerType);
            Invoke("StopVibration", 0.1f);
        }
    }

    void StopVibration()
    {
        OVRInput.SetControllerVibration(0, 0, controllerType);
    }
}
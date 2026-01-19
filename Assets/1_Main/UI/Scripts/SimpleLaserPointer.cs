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
    public Color normalColor = new Color(1, 1, 1, 0.5f); 
    public Color hoverColor = Color.green;               

    [Header("3. 사운드 & 진동")]
    public AudioClip hoverSound;    
    public AudioClip clickSound;    
    [Range(0, 1)] public float vibrationStrength = 0.2f; 

    private LineRenderer lr;
    private AudioSource audioSource;
    private OVRInput.Controller controllerType = OVRInput.Controller.None;

    // --- 색상 및 클릭 대상 관리 ---
    private GameObject currentHitObject;      
    private Renderer currentRenderer;         
    private Color originalObjectColor;        

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        audioSource = GetComponent<AudioSource>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = laserWidth;
        lr.endWidth = laserWidth;
        if (!lr.material) lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f; 

        if (transform.name.Contains("Left") || transform.parent.name.Contains("Left"))
            controllerType = OVRInput.Controller.LTouch;
        else
            controllerType = OVRInput.Controller.RTouch;
    }

    private void OnDisable()
    {
        ResetObjectColor();
    }

    void LateUpdate()
    {
        if (!lr) return;

        lr.SetPosition(0, transform.position); 

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        // 1. 레이저 충돌 감지
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            lr.SetPosition(1, hit.point); 
            GameObject hitObj = hit.collider.gameObject;
            
            if (IsInteractable(hitObj))
            {
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
                SetLaserColor(normalColor);
                ResetObjectColor();
            }
        }
        else
        {
            lr.SetPosition(1, transform.position + transform.forward * maxDistance);
            SetLaserColor(normalColor);
            ResetObjectColor();
        }

        // 2. 트리거 입력 (클릭 시도)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controllerType))
        {
            // 소리 재생
            if (clickSound) audioSource.PlayOneShot(clickSound);

            // 🔥 [핵심 기능 추가] 보고 있는 물체에게 "너 클릭됐어!"라고 전달
            if (currentHitObject != null)
            {
                // (1) 문, 라디오 같은 일반 물체 (IPointerClickHandler)
                var clickHandler = currentHitObject.GetComponentInParent<IPointerClickHandler>();
                if (clickHandler != null)
                {
                    // 가짜 클릭 데이터 생성 후 전달
                    PointerEventData data = new PointerEventData(EventSystem.current);
                    clickHandler.OnPointerClick(data);
                }

                // (2) UI 버튼 (Button)
                var button = currentHitObject.GetComponentInParent<Button>();
                if (button != null)
                {
                    button.onClick.Invoke();
                }
            }
        }
    }

    // --- 기능 함수들 ---

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

    bool IsInteractable(GameObject obj)
    {
        if (obj.GetComponentInParent<Button>() != null) return true;
        if (obj.GetComponentInParent<IPointerClickHandler>() != null) return true;
        return false;
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
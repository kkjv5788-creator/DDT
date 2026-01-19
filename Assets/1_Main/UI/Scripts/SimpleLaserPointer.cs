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
    public Color normalColor = new Color(1, 1, 1, 0.5f); // 평소 (흰색 반투명)
    public Color hoverColor = Color.green;               // 닿았을 때 (초록색)

    [Header("3. 사운드 & 진동 (피드백)")]
    public AudioClip hoverSound;    // 틱! 소리
    public AudioClip clickSound;    // 딸깍! 소리
    [Range(0, 1)] public float vibrationStrength = 0.2f; 

    private LineRenderer lr;
    private AudioSource audioSource;
    private OVRInput.Controller controllerType = OVRInput.Controller.None;

    // --- 색상 변경을 위한 변수들 ---
    private GameObject currentHitObject;      // 현재 닿아있는 물체
    private Renderer currentRenderer;         // 그 물체의 렌더러 (색칠 담당)
    private Color originalObjectColor;        // 물체의 원래 색깔 기억용

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

    void LateUpdate()
    {
        if (!lr) return;

        lr.SetPosition(0, transform.position); // 시작점

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        // 레이저 발사
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            lr.SetPosition(1, hit.point); 

            GameObject hitObj = hit.collider.gameObject;
            
            // 상호작용 가능한 물체인지 확인
            if (IsInteractable(hitObj))
            {
                // 1. 레이저 색상 변경
                SetLaserColor(hoverColor);

                // 2. 새로운 물체에 닿았을 때 (소리 & 색상 변경)
                if (currentHitObject != hitObj)
                {
                    // 이전에 잡고 있던 물체가 있다면 원래대로 돌려놓기
                    ResetObjectColor();

                    PlayFeedback(); // 틱 소리 + 진동
                    
                    // 새 물체 등록 및 색상 변경
                    currentHitObject = hitObj;
                    ChangeObjectColor(currentHitObject, hoverColor);
                }
            }
            else
            {
                // 상호작용 불가능한 벽/바닥 등
                SetLaserColor(normalColor);
                ResetObjectColor(); // 물체 색상 복구
            }
        }
        else
        {
            // 허공
            lr.SetPosition(1, transform.position + transform.forward * maxDistance);
            SetLaserColor(normalColor);
            ResetObjectColor(); // 물체 색상 복구
        }

        // --- 🔥 [여기가 클릭 소리 코드입니다] ---
        // 검지 트리거(IndexTrigger)를 당기면 클릭 소리 재생
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controllerType))
        {
            if (clickSound) audioSource.PlayOneShot(clickSound);
        }
    }

    // --- 기능 함수들 ---

    // 물체 색상을 호버 색상으로 변경
    void ChangeObjectColor(GameObject obj, Color color)
    {
        // 부모나 자신에게서 Renderer 찾기
        currentRenderer = obj.GetComponent<Renderer>();
        if (currentRenderer == null) currentRenderer = obj.GetComponentInChildren<Renderer>();

        if (currentRenderer != null)
        {
            // 원래 색 기억해두기
            originalObjectColor = currentRenderer.material.color;
            // 색 바꾸기
            currentRenderer.material.color = color;
        }
    }

    // 물체 색상을 원래대로 복구
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
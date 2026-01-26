using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]
public class SimpleLaserPointer : MonoBehaviour
{
    [Header("1. 레이저 설정")]
    public float maxDistance = 10.0f;
    public float laserWidth = 0.005f;

    public LayerMask layerMask = ~0;

    [Header("2. 색상 설정")]
    public Color normalLaserColor = Color.white; // 평소 레이저 색 (흰색)
    public Color hoverLaserColor = Color.cyan;  // 닿았을 때 레이저/아웃라인 색 (초록)

    [Header("3. 사운드 & 진동")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    [Range(0, 1)] public float vibrationStrength = 0.2f;

    private LineRenderer lr;
    private AudioSource audioSource;
    private OVRInput.Controller controllerType = OVRInput.Controller.None;
    private GameFlowManager gameFlowManager;

    // --- 상태 관리 변수 ---
    private GameObject currentHitObject; // 현재 가리키고 있는 오브젝트
    private Outline currentOutline;      // 현재 켜진 아웃라인
    private CanvasGroup currentCanvas;   // 현재 켜진 정보창

    private SimpleLaserPointer otherPointer;

    

void Start()
    {
        lr = GetComponent<LineRenderer>();
        audioSource = GetComponent<AudioSource>();
        gameFlowManager = FindObjectOfType<GameFlowManager>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = laserWidth;
        lr.endWidth = laserWidth;
        if (!lr.material) lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f;

        DisableLaser(); 

        // 왼손/오른손 구분
        if (transform.name.Contains("Left") || transform.parent.name.Contains("Left"))
            controllerType = OVRInput.Controller.LTouch;
        else
            controllerType = OVRInput.Controller.RTouch;

        // ▼▼▼ [추가] 나 말고 다른(반대쪽) 레이저 포인터를 찾아서 기억해둠
        SimpleLaserPointer[] allPointers = FindObjectsOfType<SimpleLaserPointer>();
        foreach (var p in allPointers)
        {
            if (p != this) // 내가 아니면 -> 반대쪽 손이다!
            {
                otherPointer = p;
                break;
            }
        }
    }

    private void OnDisable()
    {
        DisableLaser();
    }

    void LateUpdate()
    {
        if (!lr) return;

        // // [1] 게임 상태 체크 (메뉴/대기 상태가 아니면 레이저 끄기)
        // if (gameFlowManager != null)
        // {
        //     bool isMenuState =
        //         gameFlowManager.CurrentState == GameState.WaitForRadio ||
        //         gameFlowManager.CurrentState == GameState.Paused ||
        //         gameFlowManager.CurrentState == GameState.FinalResult;

        //     if (!isMenuState)
        //     {
        //         DisableLaser();
        //         return;
        //     }
        // }

        // [2] 레이저 로직 시작
        if (!lr.enabled) lr.enabled = true; // 레이저 켜기
        lr.SetPosition(0, transform.position); // 시작점

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // 충돌 감지
        if (Physics.Raycast(ray, out hit, maxDistance, layerMask))
        {
            lr.SetPosition(1, hit.point); // 끝점은 닿은 곳
            GameObject hitObj = hit.collider.gameObject;

            // [A] 상호작용 가능한 물체인가? (버튼, 문 등)
            if (IsInteractable(hitObj))
            {
                // 레이저 색상 -> 초록색
                SetLaserColor(hoverLaserColor);

                // 새로운 물체에 닿았을 때만 처리 (최적화)
                if (currentHitObject != hitObj)
                {
                    ResetPreviousEffects(); // 이전 효과 끄기
                    
                    currentHitObject = hitObj;
                    PlayFeedback(); // 소리/진동

                    // 1. 아웃라인 켜기
                    var outline = hitObj.GetComponentInParent<Outline>();
                    if (outline != null)
                    {
                        // ▼▼▼ [수정] Door 레이어인지 확인 (맞은 놈 or 아웃라인 붙은 놈)
                        int doorLayer = LayerMask.NameToLayer("Door");
                        bool isDoor = (hitObj.layer == doorLayer || outline.gameObject.layer == doorLayer);

                        // Door가 "아닐 때만" 아웃라인을 켭니다.
                        if (!isDoor)
                        {
                            outline.enabled = true;
                            outline.OutlineColor = hoverLaserColor;
                            outline.OutlineWidth = 5f; 
                            currentOutline = outline;
                        }
                    }
                    // 2. 정보창(Canvas) 켜기 (자식 오브젝트 검색)
                    var canvasGroup = hitObj.GetComponentInChildren<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = 1f; // 보이게 설정
                        currentCanvas = canvasGroup;
                    }
                }
            }
            else
            {
                // [B] 벽이나 바닥 (상호작용 불가)
                SetLaserColor(normalLaserColor); // 레이저 흰색
                ResetPreviousEffects();          // 아웃라인/정보창 끄기
            }
        }
        else
        {
            // [C] 허공
            lr.SetPosition(1, transform.position + (transform.forward * maxDistance)); // 길게 뻗기
            SetLaserColor(normalLaserColor); // 레이저 흰색
            ResetPreviousEffects();          // 아웃라인/정보창 끄기
        }

        // 트리거 클릭 입력
        HandleInput();
    }

    // --- 기능 함수들 ---

void ResetPreviousEffects()
    {
        // 아웃라인 끄기
        if (currentOutline != null)
        {
            // [추가] 반대쪽 손도 같은 아웃라인을 보고 있다면, 끄지 않음
            bool otherIsLooking = (otherPointer != null && otherPointer.currentOutline == currentOutline);
            
            if (!otherIsLooking)
            {
                currentOutline.enabled = false;
            }
            currentOutline = null;
        }

        // 정보창 숨기기
        if (currentCanvas != null)
        {
            // ▼▼▼ [핵심 수정] 반대쪽 손이 같은 UI를 보고 있는지 확인!
            bool otherIsLooking = (otherPointer != null && otherPointer.currentCanvas == currentCanvas);

            // 반대쪽 손도 안 보고 있을 때만 끈다 (Alpha = 0)
            if (!otherIsLooking)
            {
                currentCanvas.alpha = 0f; 
            }
            
            // 내 참조는 끊음
            currentCanvas = null;
        }

        currentHitObject = null;
    }

    void SetLaserColor(Color c)
    {
        lr.startColor = c;
        lr.endColor = c;
    }

    void HandleInput()
    {
        if (lr.enabled && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controllerType))
        {
            if (currentHitObject != null)
            {
                if (clickSound) audioSource.PlayOneShot(clickSound);

                // UI 버튼 클릭
                var button = currentHitObject.GetComponentInParent<Button>();
                if (button != null) button.onClick.Invoke();

                // 일반 상호작용
                var clickHandler = currentHitObject.GetComponentInParent<IPointerClickHandler>();
                if (clickHandler != null)
                {
                    PointerEventData data = new PointerEventData(EventSystem.current);
                    clickHandler.OnPointerClick(data);
                }
            }
        }
    }
void DisableLaser()
    {
        // lr이 존재하는지 먼저 확인 (안전장치 추가)
        if (lr != null && lr.enabled)
        {
            lr.enabled = false;
            ResetPreviousEffects();
        }
    }
    bool IsInteractable(GameObject obj)
    {
        if (obj.GetComponentInParent<Button>() != null) return true;
        if (obj.GetComponentInParent<IPointerClickHandler>() != null) return true;
        if (obj.GetComponentInParent<RadioClickable>() != null) return true;
        
        // Outline이 있는 오브젝트도 상호작용 대상으로 간주 (문 등)
        if (obj.GetComponentInParent<Outline>() != null) return true; 
        
        return false;
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
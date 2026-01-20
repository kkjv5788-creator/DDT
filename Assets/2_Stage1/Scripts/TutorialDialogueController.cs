using UnityEngine;

/// <summary>
/// 튜토리얼 대사 진행 조건 타입
/// </summary>
public enum DialogueConditionType
{
    None,                           // 조건 없음 (즉시 진행)
    RightTriggerButtonClick,        // 오른손 트리거 버튼 클릭
    GazeAtKimbap,                   // kimbap 프리팹 시선 감지
    HandMovement,                   // 손 움직임 감지
    TimingBefore,                   // 타이밍 직전 (Judging 상태)
    RoundSuccess,                   // 라운드 성공
    RoundFail,                      // 라운드 실패
}

/// <summary>
/// 튜토리얼 대사 데이터 구조체
/// </summary>
[System.Serializable]
public class TutorialDialogue
{
    [TextArea(2, 5)]
    public string dialogueText;     // 대사 텍스트
    
    public DialogueConditionType conditionType; // 진행 조건
    
    [Tooltip("조건이 충족된 후 다음 대사로 넘어가기까지 대기 시간 (초)")]
    public float waitAfterCondition = 0.5f;
    
    [Tooltip("조건 없을 때 자동 진행 시간 (초, 0이면 수동 진행)")]
    public float autoAdvanceTime = 2f;
}

/// <summary>
/// 튜토리얼 대사 컨트롤러 (수정됨: MissionBoardUI 연동 포함)
/// </summary>
[DefaultExecutionOrder(-100)] // 다른 스크립트보다 먼저 실행
public class TutorialDialogueController : MonoBehaviour
{
    [Header("References")]
    public TutorialDialogueUIController dialogueUI; // 대사 전용 UI
    
    // 🔥 MissionBoardUI 추가 (주문서 제어용)
    public MissionBoardUI missionBoard;
    
    public TutorialController tutorialController;
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;
    
    [Header("Target Objects")]
    public GameObject kimbapPrefab;      // 시선 감지 대상
    public GameObject kimbap010Prefab;   // 특정 대사 후 비활성화할 객체
    
    [Header("Dialogue Data")]
    public TutorialDialogue[] dialogues;
    
    [Header("Settings")]
    public float gazeDetectionDistance = 5f;
    public float handMovementThreshold = 0.1f;
    public float gazeAngleThreshold = 30f;
    public string triggerButtonHintText = "오른손 트리거 버튼을 눌러주세요";
    
    // 내부 상태 변수
    private int _currentDialogueIndex = 0;
    private bool _isWaitingForCondition = false;
    private bool _conditionMet = false;
    private float _conditionMetTime = 0f;
    private float _autoAdvanceTimer = 0f;
    private float _lastSkipInput = -999f;
    
    // 상태 추적용
    private bool _lastRoundResult = false;
    private bool _roundResultProcessed = false;
    private Vector3 _previousControllerPosition;
    private bool _hasDetectedHandMovement = false;
    private bool _hasDetectedGaze = false;
    private bool _controllerPositionInitialized = false;
    
    // 시스템 복구용
    private bool _originalTutorialMode = false;
    private GameObject _tutorialControllerGameObject;
    private GameObject _conductorGameObject;
    
    void Awake()
    {
        DisableExistingSystems();
    }
    
    void Start()
    {
        InitializeControllerPosition();
        
        // 🔥 시작하자마자 주문서에 "면접 중" 표시
        if (missionBoard)
        {
            missionBoard.InitializeUI();
            missionBoard.UpdateHeader("< 면 접 중 >");
            missionBoard.UpdateMission("장비 착용하기", 0, 1); // 0/1 진행도
        }

        if (dialogues != null && dialogues.Length > 0)
        {
            ShowDialogue(0);
        }
        else
        {
            StartMainTutorial();
        }
    }
    
    void Update()
    {
        if (_currentDialogueIndex >= dialogues.Length) return;
        
        // A 버튼으로 스킵
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (Time.time - _lastSkipInput > (feedbackSet ? feedbackSet.skipInputCooldown : 0.5f))
            {
                _lastSkipInput = Time.time;
                SkipAllDialogues();
                return;
            }
        }
        
        var currentDialogue = dialogues[_currentDialogueIndex];
        
        // 조건 체크
        if (_isWaitingForCondition)
        {
            if (CheckCondition(currentDialogue.conditionType))
            {
                _conditionMet = true;
                _conditionMetTime = Time.time;
                _isWaitingForCondition = false;

                Debug.Log($"[TutorialDialogueController] Condition met: {currentDialogue.conditionType}");

                // 🔥 조건 충족 시 주문서에 "완료" 도장 찍기
                if (missionBoard)
                {
                    missionBoard.UpdateMission("장비 착용하기", 1, 1); // 체크박스 채움
                    missionBoard.ShowSuccessStamp(true);
                }
            }
        }
        
        // 자동 진행 및 대기 로직
        if (currentDialogue.conditionType == DialogueConditionType.None)
        {
            _autoAdvanceTimer += Time.deltaTime;
            if (_autoAdvanceTimer >= currentDialogue.autoAdvanceTime)
            {
                AdvanceToNextDialogue();
            }
        }
        else if (_conditionMet && Time.time - _conditionMetTime >= currentDialogue.waitAfterCondition)
        {
            _conditionMet = false;
            AdvanceToNextDialogue();
        }
    }
    
    void ShowDialogue(int index)
    {
        if (index < 0 || index >= dialogues.Length)
        {
            StartMainTutorial();
            return;
        }
        
        var dialogue = dialogues[index];
        
        // 힌트 텍스트 결정
        string hint = "";
        if (dialogue.conditionType == DialogueConditionType.RightTriggerButtonClick)
        {
            hint = triggerButtonHintText;
        }
        
        // UI 표시
        if (dialogueUI != null)
        {
            dialogueUI.ShowDialogue(dialogue.dialogueText, hint);
        }
        
        _autoAdvanceTimer = 0f;
        _conditionMet = false;
        _isWaitingForCondition = (dialogue.conditionType != DialogueConditionType.None);
        
        // 🔥 새 대사가 나오면 도장은 다시 숨김 (다음 미션을 위해)
        if (_isWaitingForCondition && missionBoard)
        {
            missionBoard.ShowSuccessStamp(false);
        }
        
        Debug.Log($"[TutorialDialogueController] Dialogue {index}: {dialogue.dialogueText}");
    }
    
    void SkipAllDialogues()
    {
        if (dialogueUI != null) dialogueUI.Hide();
        StartMainTutorial();
    }
    
    bool CheckCondition(DialogueConditionType conditionType)
    {
        switch (conditionType)
        {
            case DialogueConditionType.RightTriggerButtonClick:
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)) 
                    return true;
                break;
                
            case DialogueConditionType.GazeAtKimbap:
                if (!_hasDetectedGaze && kimbapPrefab != null)
                {
                    if (CheckGazeAtObject(kimbapPrefab))
                    {
                        _hasDetectedGaze = true;
                        return true;
                    }
                }
                break;
                
            case DialogueConditionType.HandMovement:
                if (!_hasDetectedHandMovement)
                {
                    if (CheckControllerMovement())
                    {
                        _hasDetectedHandMovement = true;
                        return true;
                    }
                }
                break;

            case DialogueConditionType.TimingBefore:
                if (conductor != null && conductor.State == RhythmConductor.RhythmState.Judging)
                    return true;
                break;

            case DialogueConditionType.RoundSuccess:
                if (!_roundResultProcessed && _lastRoundResult)
                {
                    _roundResultProcessed = true;
                    return true;
                }
                break;

            case DialogueConditionType.RoundFail:
                if (!_roundResultProcessed && !_lastRoundResult)
                {
                    _roundResultProcessed = true;
                    return true;
                }
                break;
        }
        return false;
    }
    
    // --- Helper Functions (누락되었던 부분 복원) ---

    bool CheckGazeAtObject(GameObject target)
    {
        GameObject centerEye = GameObject.Find("OVRCameraRig/TrackingSpace/CenterEyeAnchor");
        if (centerEye == null) centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye == null) return false;
        
        Vector3 eyePos = centerEye.transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 direction = (targetPos - eyePos).normalized;
        float distance = Vector3.Distance(eyePos, targetPos);
        
        if (distance > gazeDetectionDistance) return false;
        
        Vector3 forward = centerEye.transform.forward;
        float angle = Vector3.Angle(forward, direction);
        if (angle > gazeAngleThreshold) return false;
        
        RaycastHit hit;
        if (Physics.Raycast(eyePos, direction, out hit, gazeDetectionDistance))
        {
            Transform hitTransform = hit.transform;
            while (hitTransform != null)
            {
                if (hitTransform.gameObject == target || hitTransform.name.ToLower().Contains("kimbap"))
                    return true;
                hitTransform = hitTransform.parent;
            }
        }
        return false;
    }
    
    void InitializeControllerPosition()
    {
        GameObject controller = GetControllerObject();
        if (controller != null)
        {
            _previousControllerPosition = controller.transform.position;
            _controllerPositionInitialized = true;
        }
    }
    
    bool CheckControllerMovement()
    {
        if (!_controllerPositionInitialized)
        {
            InitializeControllerPosition();
            return false;
        }
        
        GameObject controller = GetControllerObject();
        if (controller == null) return false;
        
        Vector3 currentPos = controller.transform.position;
        float movement = Vector3.Distance(currentPos, _previousControllerPosition);
        _previousControllerPosition = currentPos;
        
        if (movement > handMovementThreshold) return true;
        return false;
    }
    
    GameObject GetControllerObject()
    {
        GameObject controller = GameObject.Find("OVRCameraRig/TrackingSpace/RightHandAnchor");
        if (controller == null) controller = GameObject.Find("RightControllerAnchor");
        if (controller == null) controller = GameObject.Find("RightHandAnchor");
        if (controller == null) controller = GameObject.Find("RightController");
        return controller;
    }

    void AdvanceToNextDialogue()
    {
        // 6번 대사 후 특정 오브젝트 끄기 (옵션)
        if (_currentDialogueIndex == 6 && kimbap010Prefab != null)
        {
            kimbap010Prefab.SetActive(false);
        }
        
        _currentDialogueIndex++;
        _roundResultProcessed = false;
        
        if (_currentDialogueIndex < dialogues.Length)
        {
            ShowDialogue(_currentDialogueIndex);
        }
        else
        {
            StartMainTutorial();
        }
    }
    
    void DisableExistingSystems()
    {
        // TutorialController 비활성화
        if (tutorialController != null)
        {
            _tutorialControllerGameObject = tutorialController.gameObject;
            tutorialController.enabled = false;
            _tutorialControllerGameObject.SetActive(false);
        }
        
        // Conductor 비활성화
        if (conductor != null)
        {
            _conductorGameObject = conductor.gameObject;
            _originalTutorialMode = conductor.isTutorialMode;
            conductor.enabled = false;
            _conductorGameObject.SetActive(false);
        }
    }
    
    void StartMainTutorial()
    {
        if (dialogueUI != null) dialogueUI.Hide();
        
        // 시스템 재활성화
        if (_conductorGameObject != null && conductor != null)
        {
            _conductorGameObject.SetActive(true);
            conductor.enabled = true;
            conductor.isTutorialMode = _originalTutorialMode;
            conductor.OnRoundResult.AddListener(OnRoundResult);
        }
        
        if (_tutorialControllerGameObject != null && tutorialController != null)
        {
            _tutorialControllerGameObject.SetActive(true);
            tutorialController.enabled = true;
            
            // 🔥 TutorialController가 MissionBoard를 이어받아 진행
            tutorialController.StartTutorial();
        }
        
        this.enabled = false; // 대사 컨트롤러 종료
    }
    
    void OnEnable()
    {
        // Conductor가 켜져있을 때만 이벤트 구독
        if (conductor != null && conductor.enabled)
             conductor.OnRoundResult.AddListener(OnRoundResult);
    }

    void OnDisable()
    {
        if (conductor != null)
             conductor.OnRoundResult.RemoveListener(OnRoundResult);
    }
    
    void OnRoundResult(bool success)
    {
        _lastRoundResult = success;
        _roundResultProcessed = false; 
    }
}
using UnityEngine;
using TMPro;

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
/// 튜토리얼 대사 데이터
/// </summary>
[System.Serializable]
public class TutorialDialogue
{
    [TextArea(2, 5)]
    public string dialogueText;     // 대사 텍스트
    
    public DialogueConditionType conditionType;  // 진행 조건
    
    [Tooltip("조건이 충족된 후 다음 대사로 넘어가기까지 대기 시간 (초)")]
    public float waitAfterCondition = 0.5f;
    
    [Tooltip("조건 없을 때 자동 진행 시간 (초, 0이면 수동 진행)")]
    public float autoAdvanceTime = 2f;
}

/// <summary>
/// 튜토리얼 대사 컨트롤러
/// 씬 시작 시 바로 대사를 표시하고, 기존 튜토리얼/메인 기능은 비활성화
/// 마지막 대사 후 기존 TutorialController.StartTutorial() 호출
/// </summary>
public class TutorialDialogueController : MonoBehaviour
{
    [Header("References")]
    public TutorialUIController tutorialUI;
    public TutorialController tutorialController;
    public RhythmConductor conductor;
    
    [Header("Target Objects")]
    public GameObject kimbapPrefab;  // 시선 감지 대상 (kimbap 프리팹)
    
    [Header("Dialogue Data")]
    [Tooltip("튜토리얼 시작 전 대사들 (3회 성공 연습 직전까지)")]
    public TutorialDialogue[] dialogues;
    
    [Header("Settings")]
    [Tooltip("시선 감지 거리")]
    public float gazeDetectionDistance = 5f;
    
    [Tooltip("손 움직임 감지 임계값 (미터)")]
    public float handMovementThreshold = 0.1f;
    
    [Tooltip("시선 감지 각도 (도)")]
    public float gazeAngleThreshold = 30f;
    
    private int _currentDialogueIndex = 0;
    private bool _isWaitingForCondition = false;
    private bool _conditionMet = false;
    private float _conditionMetTime = 0f;
    private float _autoAdvanceTimer = 0f;
    
    // 상태 추적
    private bool _lastRoundResult = false;
    private bool _roundResultProcessed = false;
    private Vector3 _lastControllerPosition;
    private bool _hasDetectedHandMovement = false;
    private bool _hasDetectedGaze = false;
    
    // 기존 시스템 비활성화 플래그
    private bool _originalTutorialMode = false;
    
    void Start()
    {
        // 기존 튜토리얼/메인 기능 비활성화
        DisableExistingSystems();
        
        // 초기 컨트롤러 위치 저장
        UpdateControllerPosition();
        
        // 첫 대사 표시
        if (dialogues != null && dialogues.Length > 0)
        {
            ShowDialogue(0);
        }
        else
        {
            Debug.LogWarning("[TutorialDialogueController] No dialogues assigned!");
            // 대사가 없으면 바로 기존 튜토리얼 시작
            StartMainTutorial();
        }
    }
    
    void Update()
    {
        if (_currentDialogueIndex >= dialogues.Length) return;
        
        var currentDialogue = dialogues[_currentDialogueIndex];
        
        // 조건 체크
        if (_isWaitingForCondition)
        {
            if (CheckCondition(currentDialogue.conditionType))
            {
                _conditionMet = true;
                _conditionMetTime = Time.time;
                _isWaitingForCondition = false;
                Debug.Log($"[TutorialDialogueController] Condition met for dialogue {_currentDialogueIndex}: {currentDialogue.conditionType}");
            }
        }
        
        // 조건 없을 때 자동 진행
        if (currentDialogue.conditionType == DialogueConditionType.None)
        {
            _autoAdvanceTimer += Time.deltaTime;
            if (_autoAdvanceTimer >= currentDialogue.autoAdvanceTime)
            {
                AdvanceToNextDialogue();
            }
        }
        // 조건 충족 후 대기 시간 경과 확인
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
            // 모든 대사 완료 - 기존 튜토리얼 시작
            StartMainTutorial();
            return;
        }
        
        var dialogue = dialogues[index];
        
        // UI에 대사 표시
        if (tutorialUI && tutorialUI.instructionText)
        {
            tutorialUI.instructionText.text = dialogue.dialogueText;
            tutorialUI.FadeIn();
        }
        
        // 상태 초기화
        _autoAdvanceTimer = 0f;
        _conditionMet = false;
        _isWaitingForCondition = false;
        
        // 조건이 없으면 자동 진행
        if (dialogue.conditionType == DialogueConditionType.None)
        {
            _autoAdvanceTimer = 0f;
        }
        else
        {
            _isWaitingForCondition = true;
        }
        
        Debug.Log($"[TutorialDialogueController] Dialogue {index}: {dialogue.dialogueText} (Condition: {dialogue.conditionType})");
    }
    
    bool CheckCondition(DialogueConditionType conditionType)
    {
        switch (conditionType)
        {
            case DialogueConditionType.RightTriggerButtonClick:
                // 오른손 트리거 버튼 클릭
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    Debug.Log("[TutorialDialogueController] Right trigger button clicked");
                    return true;
                }
                break;
                
            case DialogueConditionType.GazeAtKimbap:
                // kimbap 시선 감지 (한 번만)
                if (!_hasDetectedGaze && kimbapPrefab != null)
                {
                    if (CheckGazeAtObject(kimbapPrefab))
                    {
                        _hasDetectedGaze = true;
                        Debug.Log("[TutorialDialogueController] Gaze detected at kimbap");
                        return true;
                    }
                }
                break;
                
            case DialogueConditionType.HandMovement:
                // 손 움직임 감지 (한 번만)
                if (!_hasDetectedHandMovement)
                {
                    if (CheckHandMovement())
                    {
                        _hasDetectedHandMovement = true;
                        Debug.Log("[TutorialDialogueController] Hand movement detected");
                        return true;
                    }
                }
                break;
                
            case DialogueConditionType.TimingBefore:
                // 타이밍 직전 (Judging 상태)
                if (conductor != null && conductor.State == RhythmConductor.RhythmState.Judging)
                {
                    Debug.Log("[TutorialDialogueController] Timing before detected");
                    return true;
                }
                break;
                
            case DialogueConditionType.RoundSuccess:
                // 라운드 성공 (한 번만)
                if (!_roundResultProcessed && _lastRoundResult)
                {
                    _roundResultProcessed = true;
                    Debug.Log("[TutorialDialogueController] Round success detected");
                    return true;
                }
                break;
                
            case DialogueConditionType.RoundFail:
                // 라운드 실패 (한 번만)
                if (!_roundResultProcessed && !_lastRoundResult)
                {
                    _roundResultProcessed = true;
                    Debug.Log("[TutorialDialogueController] Round fail detected");
                    return true;
                }
                break;
        }
        
        return false;
    }
    
    bool CheckGazeAtObject(GameObject target)
    {
        // OVRCameraRig의 CenterEyeAnchor에서 레이캐스트
        GameObject centerEye = GameObject.Find("OVRCameraRig/TrackingSpace/CenterEyeAnchor");
        if (centerEye == null)
        {
            centerEye = GameObject.Find("CenterEyeAnchor");
        }
        
        if (centerEye == null) return false;
        
        Vector3 eyePos = centerEye.transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 direction = (targetPos - eyePos).normalized;
        float distance = Vector3.Distance(eyePos, targetPos);
        
        // 거리 체크
        if (distance > gazeDetectionDistance) return false;
        
        // 각도 체크 (시선 방향과 타겟 방향의 각도)
        Vector3 forward = centerEye.transform.forward;
        float angle = Vector3.Angle(forward, direction);
        if (angle > gazeAngleThreshold) return false;
        
        // 레이캐스트로 실제로 보이는지 확인
        RaycastHit hit;
        if (Physics.Raycast(eyePos, direction, out hit, gazeDetectionDistance))
        {
            // kimbap 또는 그 하위 오브젝트인지 확인
            Transform hitTransform = hit.transform;
            while (hitTransform != null)
            {
                if (hitTransform.gameObject == target || 
                    hitTransform.name.ToLower().Contains("kimbap"))
                {
                    return true;
                }
                hitTransform = hitTransform.parent;
            }
        }
        
        return false;
    }
    
    bool CheckHandMovement()
    {
        UpdateControllerPosition();
        
        Vector3 currentPos = _lastControllerPosition;
        float movement = Vector3.Distance(currentPos, _lastControllerPosition);
        
        return movement > handMovementThreshold;
    }
    
    void UpdateControllerPosition()
    {
        GameObject rightController = GameObject.Find("RightControllerAnchor");
        if (rightController == null)
        {
            rightController = GameObject.Find("RightHandAnchor");
        }
        
        if (rightController != null)
        {
            Vector3 currentPos = rightController.transform.position;
            float movement = Vector3.Distance(currentPos, _lastControllerPosition);
            
            if (movement > handMovementThreshold)
            {
                _lastControllerPosition = currentPos;
            }
        }
    }
    
    void AdvanceToNextDialogue()
    {
        _currentDialogueIndex++;
        _roundResultProcessed = false; // 라운드 결과 플래그 리셋
        
        if (_currentDialogueIndex < dialogues.Length)
        {
            ShowDialogue(_currentDialogueIndex);
        }
        else
        {
            // 모든 대사 완료 - 기존 튜토리얼 시작
            Debug.Log("[TutorialDialogueController] All dialogues completed! Starting main tutorial...");
            StartMainTutorial();
        }
    }
    
    void DisableExistingSystems()
    {
        // 기존 튜토리얼 시스템 비활성화
        if (tutorialController != null)
        {
            tutorialController.enabled = false;
        }
        
        // RhythmConductor 비활성화 (대사 진행 중에는 게임 로직 비활성화)
        if (conductor != null)
        {
            _originalTutorialMode = conductor.isTutorialMode;
            conductor.enabled = false;
        }
        
        Debug.Log("[TutorialDialogueController] Existing tutorial/main systems disabled");
    }
    
    void StartMainTutorial()
    {
        // 기존 시스템 다시 활성화
        if (conductor != null)
        {
            conductor.enabled = true;
            conductor.isTutorialMode = _originalTutorialMode;
        }
        
        if (tutorialController != null)
        {
            tutorialController.enabled = true;
            // 기존 TutorialController의 StartTutorial 호출
            Debug.Log("[TutorialDialogueController] Calling TutorialController.StartTutorial()");
            tutorialController.StartTutorial();
        }
        else
        {
            Debug.LogError("[TutorialDialogueController] TutorialController is null!");
        }
        
        // 이 스크립트는 비활성화 (대사 시스템 종료)
        this.enabled = false;
    }
    
    // RhythmConductor의 OnRoundResult 이벤트 구독
    void OnEnable()
    {
        if (conductor != null)
        {
            conductor.OnRoundResult.AddListener(OnRoundResult);
        }
    }
    
    void OnDisable()
    {
        if (conductor != null)
        {
            conductor.OnRoundResult.RemoveListener(OnRoundResult);
        }
    }
    
    void OnRoundResult(bool success)
    {
        _lastRoundResult = success;
        _roundResultProcessed = false; // 새 결과가 들어왔으므로 플래그 리셋
    }
}

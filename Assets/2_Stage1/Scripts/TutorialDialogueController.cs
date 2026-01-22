using UnityEngine;
using System.Text.RegularExpressions;

public enum DialogueConditionType
{
    None,                           // 조건 없음 (자동 진행)
    RightTriggerButtonClick,        // (사용 안 함)
    GazeAtKimbap,                   // 김밥 확인 -> 캔버스 클릭
    GazeAtNote,                     // 노트 확인 -> 캔버스 클릭
    HandMovement,                   // 손 움직임 감지
    TimingBefore,                   // 타이밍 직전
    RoundSuccess,                   // 라운드 성공
    RoundFail,                      // 라운드 실패
}

[System.Serializable]
public class TutorialDialogue
{
    [TextArea(2, 5)]
    public string dialogueText;     
    public DialogueConditionType conditionType; 
    
    [Tooltip("조건이 충족된 후 다음 대사로 넘어가기까지 대기 시간 (초)")]
    public float waitAfterCondition = 0.5f;
    
    [Tooltip("조건 없을 때 자동 진행 시간 (초, 0이면 수동 진행)")]
    public float autoAdvanceTime = 2f;
}

[DefaultExecutionOrder(-100)]
public class TutorialDialogueController : MonoBehaviour
{
    [Header("References")]
    public TutorialDialogueUIController dialogueUI; 
    public MissionBoardUI missionBoard;             
    
    public TutorialController tutorialController;
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;
    
    [Header("Target Objects")]
    public GameObject kimbapPrefab;      
    public GameObject kimbap010Prefab;   
    public GameObject monitorCanvasObject; // 모니터 캔버스 연결
    
    [Header("Dialogue Data")]
    public TutorialDialogue[] dialogues;
    
    [Header("Settings")]
    public float gazeDetectionDistance = 10f; 
    public float gazeAngleThreshold = 45f;    
    public float handMovementThreshold = 0.1f; 
    
    [Tooltip("대괄호 [ ] 안의 텍스트 색상")]
    public string highlightTextColor = "#FFD700"; 
    
    // 힌트 텍스트 모음
    private const string HINT_LOOK_KIMBAP = "김밥을 바라보세요";
    private const string HINT_LOOK_NOTE = "왼쪽 주문서를 바라보세요";
    
    // 🔥 [변경됨] 요청하신 텍스트 포맷 적용
    // {0} 위치에 "[오른손 트리거] 다음" 텍스트가 들어갑니다.
    private const string HINT_CONFIRMED_FORMAT = "<color=#00FF00>V 확인완료</color>\n{0}"; 
    private const string ACTION_TRIGGER_NEXT = "[오른손 트리거] 다음";

    // 내부 변수
    private int _currentDialogueIndex = 0;
    private bool _isWaitingForCondition = false;
    private bool _conditionMet = false;
    private float _conditionMetTime = 0f;
    private float _autoAdvanceTimer = 0f;
    private float _lastSkipInput = -999f;
    
    // 상태 추적
    private bool _hasLookedAtTarget = false; 
    
    private bool _lastRoundResult = false;
    private bool _roundResultProcessed = false;
    private Vector3 _previousControllerPosition;
    private bool _hasDetectedHandMovement = false;
    private bool _controllerPositionInitialized = false;
    
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
        
        if (kimbap010Prefab != null) kimbap010Prefab.SetActive(true);

        if (missionBoard)
        {
            missionBoard.InitializeUI(); 
            missionBoard.UpdateHeader("< 면 접 중 >");
            missionBoard.UpdateMission("사장님 말씀 듣기", 0, 1); 
            missionBoard.ShowSuccessStamp(false);
        }

        if (dialogues != null && dialogues.Length > 0) ShowDialogue(0);
        else StartMainTutorial();
    }
    
    void Update()
    {
        if (_currentDialogueIndex >= dialogues.Length) return;
        
        // 스킵 (A 버튼)
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
            bool isConditionFullfilled = false;

            // 1. 시선 감지 타입
            if (currentDialogue.conditionType == DialogueConditionType.GazeAtKimbap || 
                currentDialogue.conditionType == DialogueConditionType.GazeAtNote)
            {
                GameObject target = (currentDialogue.conditionType == DialogueConditionType.GazeAtKimbap) ? kimbapPrefab : missionBoard.gameObject;
                
                // 현재 쳐다보고 있는지 확인
                bool isCurrentlyLooking = CheckGazeAtObject(target);
                
                if (isCurrentlyLooking) 
                {
                    _hasLookedAtTarget = true;
                }

                // 봤던 적이 있으면
                if (_hasLookedAtTarget)
                {
                    // 🔥 [변경] 모니터 힌트 텍스트 업데이트 (리갈패드 건드리지 않음)
                    // 예: 
                    // V 확인완료
                    // [오른손 트리거] 다음
                    string nextAction = ApplyHighlightColor(ACTION_TRIGGER_NEXT);
                    string finalHint = string.Format(HINT_CONFIRMED_FORMAT, nextAction);
                    
                    UpdateHintText(finalHint);
                    
                    // 모니터 캔버스 클릭 감지
                    if (CheckMonitorClick())
                    {
                        isConditionFullfilled = true;
                    }
                }
                else
                {
                    // 아직 안 봤으면 "쳐다보세요" 힌트
                    string originalHint = (currentDialogue.conditionType == DialogueConditionType.GazeAtKimbap) ? HINT_LOOK_KIMBAP : HINT_LOOK_NOTE;
                    UpdateHintText(originalHint);
                }
            }
            // 2. 기타 조건들
            else
            {
                if (CheckPassiveCondition(currentDialogue.conditionType))
                    isConditionFullfilled = true;
            }

            // 조건 달성 시 처리
            if (isConditionFullfilled)
            {
                _conditionMet = true;
                _conditionMetTime = Time.time;
                _isWaitingForCondition = false;
                
                if (missionBoard) missionBoard.ShowSuccessStamp(true);
            }
        }
        
        // 다음 진행
        if (currentDialogue.conditionType == DialogueConditionType.None)
        {
            _autoAdvanceTimer += Time.deltaTime;
            if (_autoAdvanceTimer >= currentDialogue.autoAdvanceTime) AdvanceToNextDialogue();
        }
        else if (_conditionMet && Time.time - _conditionMetTime >= currentDialogue.waitAfterCondition)
        {
            _conditionMet = false;
            AdvanceToNextDialogue();
        }
    }
    
    // 모니터 캔버스 클릭 감지
    bool CheckMonitorClick()
    {
        if (monitorCanvasObject == null) return false;

        // 트리거 확인
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            return false;

        // 레이캐스트 확인
        GameObject controller = GetControllerObject();
        if (controller == null) return false;

        Vector3 startPos = controller.transform.position;
        Vector3 direction = controller.transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(startPos, direction, out hit, 10f))
        {
            Transform hitTx = hit.transform;
            while (hitTx != null)
            {
                if (hitTx.gameObject == monitorCanvasObject) return true;
                if (hitTx.name.Contains("Monitor") || hitTx.name.Contains("Canvas")) return true;
                hitTx = hitTx.parent;
            }
        }
        return false;
    }

    bool CheckGazeAtObject(GameObject target)
    {
        if (target == null) return false;
        GameObject centerEye = GameObject.Find("OVRCameraRig/TrackingSpace/CenterEyeAnchor");
        if (centerEye == null) centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye == null) return false;
        
        Vector3 eyePos = centerEye.transform.position;
        Vector3 forward = centerEye.transform.forward;
        Vector3 dirToTarget = (target.transform.position - eyePos).normalized;
        float angle = Vector3.Angle(forward, dirToTarget);
        if (angle > gazeAngleThreshold) return false;

        // SphereCast로 조금 두껍게 쏴서 인식률 높임
        RaycastHit hit;
        if (Physics.SphereCast(eyePos, 0.15f, forward, out hit, gazeDetectionDistance))
        {
            Transform hitTransform = hit.transform;
            while (hitTransform != null)
            {
                if (hitTransform.gameObject == target) return true;
                if (hitTransform.name.Contains("LegalPad") || hitTransform.name.Contains("Mission") || hitTransform.name.Contains("Kimbap")) return true;
                hitTransform = hitTransform.parent;
            }
        }
        return false;
    }

    bool CheckPassiveCondition(DialogueConditionType conditionType)
    {
        switch (conditionType)
        {
            case DialogueConditionType.HandMovement:
                if (!_hasDetectedHandMovement && CheckControllerMovement()) { _hasDetectedHandMovement = true; return true; }
                break;
            case DialogueConditionType.TimingBefore:
                if (conductor != null && conductor.State == RhythmConductor.RhythmState.Judging) return true;
                break;
            case DialogueConditionType.RoundSuccess:
                if (!_roundResultProcessed && _lastRoundResult) { _roundResultProcessed = true; return true; }
                break;
            case DialogueConditionType.RoundFail:
                if (!_roundResultProcessed && !_lastRoundResult) { _roundResultProcessed = true; return true; }
                break;
        }
        return false;
    }
    
    void ShowDialogue(int index)
    {
        if (index < 0 || index >= dialogues.Length) { StartMainTutorial(); return; }
        
        var dialogue = dialogues[index];
        string initialHint = "";

        switch (dialogue.conditionType)
        {
            case DialogueConditionType.GazeAtKimbap: initialHint = HINT_LOOK_KIMBAP; break;
            case DialogueConditionType.GazeAtNote: initialHint = HINT_LOOK_NOTE; break;
        }
        
        if (dialogueUI != null)
        {
            string coloredText = ApplyHighlightColor(dialogue.dialogueText);
            dialogueUI.ShowDialogue(coloredText, initialHint);
        }
        
        _autoAdvanceTimer = 0f;
        _conditionMet = false;
        _isWaitingForCondition = (dialogue.conditionType != DialogueConditionType.None);
        
        // 초기화
        _hasLookedAtTarget = false; 
        
        // 새 대사 나오면 리갈패드 도장 초기화
        if (_isWaitingForCondition && missionBoard) 
        {
            missionBoard.ShowSuccessStamp(false);
        }
    }

    void UpdateHintText(string newHint)
    {
        if (dialogueUI != null && dialogueUI.hintText != null)
        {
            // "V 확인완료" 텍스트가 들어오면, 이미 색상 태그가 있으므로 ApplyHighlightColor를 거치지 않고 바로 넣거나
            // ApplyHighlightColor 함수가 태그를 망치지 않게 주의해야 함.
            
            if (newHint.Contains("V 확인완료"))
            {
                 // 이미 색상 처리된 문자열 그대로 사용
                 if (dialogueUI.hintText.text != newHint)
                    dialogueUI.hintText.text = newHint;
            }
            else
            {
                 string colored = ApplyHighlightColor(newHint);
                 if (dialogueUI.hintText.text != colored)
                    dialogueUI.hintText.text = colored;
            }
            
            dialogueUI.hintText.gameObject.SetActive(true);
        }
    }

    string ApplyHighlightColor(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return Regex.Replace(input, @"\[(.*?)\]", $"<color={highlightTextColor}>$0</color>");
    }
    
    void SkipAllDialogues()
    {
        if (dialogueUI != null) dialogueUI.Hide();
        StartMainTutorial();
    }
    
    void InitializeControllerPosition()
    {
        GameObject controller = GetControllerObject();
        if (controller != null) { _previousControllerPosition = controller.transform.position; _controllerPositionInitialized = true; }
    }
    
    bool CheckControllerMovement()
    {
        if (!_controllerPositionInitialized) { InitializeControllerPosition(); return false; }
        GameObject controller = GetControllerObject();
        if (controller == null) return false;
        Vector3 currentPos = controller.transform.position;
        float movement = Vector3.Distance(currentPos, _previousControllerPosition);
        _previousControllerPosition = currentPos;
        return movement > handMovementThreshold;
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
        _currentDialogueIndex++;
        _roundResultProcessed = false;
        if (_currentDialogueIndex < dialogues.Length) ShowDialogue(_currentDialogueIndex);
        else StartMainTutorial();
    }
    
    void DisableExistingSystems()
    {
        if (tutorialController != null) { _tutorialControllerGameObject = tutorialController.gameObject; tutorialController.enabled = false; _tutorialControllerGameObject.SetActive(false); }
        if (conductor != null) { _conductorGameObject = conductor.gameObject; _originalTutorialMode = conductor.isTutorialMode; conductor.enabled = false; _conductorGameObject.SetActive(false); }
    }
    
void StartMainTutorial()
    {
        if (dialogueUI != null) dialogueUI.Hide();
        if (kimbap010Prefab != null) kimbap010Prefab.SetActive(false); 
        
        if (_conductorGameObject != null && conductor != null)
        {
            _conductorGameObject.SetActive(true);
            conductor.enabled = true;
            conductor.isTutorialMode = _originalTutorialMode;
            conductor.OnRoundResult.AddListener(OnRoundResult);
        }
        
        if (_tutorialControllerGameObject != null && tutorialController != null)
        {
            _tutorialControllerGameObject.SetActive(true); // 1. 여기서 켜지면서 자동으로 시작됨 (OnEnable)
            tutorialController.enabled = true;
            
            // 🚨 [범인 발견!] 이 줄이 한번 더 실행시켜서 1단계를 스킵해버리는 겁니다.
            // tutorialController.StartTutorial();  <-- 이 줄을 지우세요!
        }
        
        this.enabled = false; 
    }
    
    void OnEnable() { if (conductor != null && conductor.enabled) conductor.OnRoundResult.AddListener(OnRoundResult); }
    void OnDisable() { if (conductor != null) conductor.OnRoundResult.RemoveListener(OnRoundResult); }
    void OnRoundResult(bool success) { _lastRoundResult = success; _roundResultProcessed = false; }
}
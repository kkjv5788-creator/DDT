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
    [Header("Start")]
    public bool autoStart = false; 

    // ▼▼▼ [추가] 인스펙터에서 직접 음악 파일을 넣는 곳 ▼▼▼
    [Header("Audio Settings")]
    public AudioClip tutorialBgmClip; 

    [Header("References")]
    public TutorialDialogueUIController dialogueUI; 
    public MissionBoardUI missionBoard;             
    
    public TutorialController tutorialController;
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;

    // ▼▼▼ [필수] 시야 감지용 카메라 (인스펙터에서 OVRCameraRig의 CenterEyeAnchor 연결) ▼▼▼
    public Transform centerEyeAnchor; 
    
    [Header("Target Objects")]
    public GameObject kimbapPrefab;      
    public GameObject kimbap010Prefab;   
    public GameObject monitorCanvasObject; 
    
    [Header("Dialogue Data")]
    public TutorialDialogue[] dialogues;

    [Header("Settings")]
    public float gazeDetectionDistance = 20f; // 거리 넉넉하게 수정
    public float gazeRadius = 0.15f;          // 시선 판정 두께 (지름 30cm)
    public float handMovementThreshold = 0.1f;

    [Tooltip("시선 감지 시 무시할 레이어 (칼 등)")]
    public LayerMask gazeLayerMask = ~0;      // 기본값: 모든 레이어

    [Tooltip("대괄호 [ ] 안의 텍스트 색상")]
    public string highlightTextColor = "#FFD700";

    // 힌트 텍스트 모음
    private const string HINT_LOOK_KIMBAP = "김밥을 바라보세요";
    private const string HINT_LOOK_NOTE = "왼쪽 주문서를 바라보세요";
    
    private const string HINT_CONFIRMED_FORMAT = "<color=#00FF00>V 확인완료</color>\n{0}"; 
    private const string ACTION_TRIGGER_NEXT = "[오른손 트리거] 다음";

    // 내부 변수
    private int _currentDialogueIndex = 0;
    private bool _isWaitingForCondition = false;
    private bool _conditionMet = false;
    private float _conditionMetTime = 0f;
    private float _autoAdvanceTimer = 0f;

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
        // 카메라 자동 찾기 (안전장치)
        if (centerEyeAnchor == null)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null) centerEyeAnchor = rig.centerEyeAnchor;
            else if (Camera.main != null) centerEyeAnchor = Camera.main.transform;
        }

        if (autoStart)
        {
            BeginTutorialFlow();
        }
    }

    public void BeginTutorialFlow()
    {
        InitializeControllerPosition();

        // 김밥과 모니터 켜기
        if (kimbap010Prefab != null) kimbap010Prefab.SetActive(true);
        if (monitorCanvasObject != null) monitorCanvasObject.SetActive(true);

        if (dialogueUI == null)
            dialogueUI = GetComponentInChildren<TutorialDialogueUIController>(true);

        if (missionBoard)
        {
            missionBoard.InitializeUI();
            missionBoard.UpdateHeader("< 면 접 중 >");
            missionBoard.UpdateMission("모니터를 보세요", 0, 1);
        }

        // ▼▼▼ [수정] BGM 재생 로직 ▼▼▼
        if (conductor && conductor.bgmSource)
        {
            // 1순위: 인스펙터에 넣은 클립
            if (tutorialBgmClip != null)
            {
                conductor.bgmSource.clip = tutorialBgmClip;
                conductor.bgmSource.loop = true;
                conductor.bgmSource.Play();
            }
            // 2순위: 데이터에 있는 클립
            else if (tutorialController && tutorialController.tutorialTriggerList)
            {
                var bgm = tutorialController.tutorialTriggerList.bgm;
                if (bgm)
                {
                    conductor.bgmSource.clip = bgm;
                    conductor.bgmSource.loop = true;
                    conductor.bgmSource.Play();
                }
            }
        }

        // 대사 시작
        if (dialogues != null && dialogues.Length > 0)
        {
            if (dialogueUI == null)
            {
                Debug.LogError("[TutorialDialogueController] dialogueUI missing. Skip to main tutorial.");
                StartMainTutorial();
                return;
            }

            ShowDialogue(0);
        }
        else
        {
            StartMainTutorial();
        }
    }
    
    void Update()
    {
        if (dialogues == null || dialogues.Length == 0) return;
        if (_currentDialogueIndex >= dialogues.Length) return;

        var currentDialogue = dialogues[_currentDialogueIndex];
        
        // 조건 체크
        if (_isWaitingForCondition)
        {
            bool isConditionFullfilled = false;

            // 1. 시선 감지 타입
            if (currentDialogue.conditionType == DialogueConditionType.GazeAtKimbap || 
                currentDialogue.conditionType == DialogueConditionType.GazeAtNote)
            {
                GameObject target = null;
                if (currentDialogue.conditionType == DialogueConditionType.GazeAtKimbap)
                    target = kimbapPrefab; // 혹은 kimbap010Prefab (상황에 맞춰 사용)
                else if (missionBoard)
                    target = missionBoard.gameObject;

                if (target == null && currentDialogue.conditionType == DialogueConditionType.GazeAtKimbap)
                     target = kimbap010Prefab; // 예비용

                if (!target) return;
                
                // 현재 쳐다보고 있는지 확인 (개선된 함수 사용)
                bool isCurrentlyLooking = CheckGazeAtObject(target);
                if (isCurrentlyLooking) 
                {
                    _hasLookedAtTarget = true;
                }

                // 봤던 적이 있으면
                if (_hasLookedAtTarget)
                {
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
                    string originalHint = (currentDialogue.conditionType == DialogueConditionType.GazeAtKimbap) ?
                        HINT_LOOK_KIMBAP : HINT_LOOK_NOTE;
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
    
    // [수정됨] 모니터 클릭 감지 (SphereCast 적용으로 판정 개선)
    bool CheckMonitorClick()
    {
        if (monitorCanvasObject == null) return false;

        // 오른쪽 트리거 버튼 클릭
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            return false;

        GameObject controller = GetControllerObject();
        if (controller == null) return false;

        Vector3 startPos = controller.transform.position;
        Vector3 direction = controller.transform.forward;

        RaycastHit hit;
        // Raycast -> SphereCast (5cm 두께)
        if (Physics.SphereCast(startPos, 0.05f, direction, out hit, 10f, gazeLayerMask))
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

    // [핵심 수정됨] 시선 감지 (각도 계산 제거 + SphereCast + 계층 구조 확인)
    bool CheckGazeAtObject(GameObject target)
    {
        if (target == null) return false;
        if (centerEyeAnchor == null) return false;

        Vector3 eyePos = centerEyeAnchor.position;
        Vector3 forward = centerEyeAnchor.forward;

        // 디버깅용 시선 표시
        Debug.DrawRay(eyePos, forward * gazeDetectionDistance, Color.green);

        RaycastHit hit;
        // SphereCast: 두꺼운 빔 발사 (gazeRadius: 기본 0.15f)
        if (Physics.SphereCast(eyePos, gazeRadius, forward, out hit, gazeDetectionDistance, gazeLayerMask))
        {
            Transform hitTransform = hit.transform;
            
            // 맞은 물체의 부모를 거슬러 올라가며 타겟 확인
            while (hitTransform != null)
            {
                // 1. 타겟 오브젝트와 일치하는가?
                if (hitTransform.gameObject == target) return true;
                
                // 2. 이름에 키워드가 포함되어 있는가? (유연한 처리)
                if (hitTransform.name.Contains("LegalPad") || 
                    hitTransform.name.Contains("Mission") || 
                    hitTransform.name.Contains("Kimbap")) 
                {
                    return true;
                }

                // 3. 타겟의 자식을 쳐다봤는가?
                if (hitTransform.IsChildOf(target.transform)) return true;

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
        
        _hasLookedAtTarget = false;
        if (_isWaitingForCondition && missionBoard) 
        {
            missionBoard.ShowSuccessStamp(false);
        }
    }

    void UpdateHintText(string newHint)
    {
        if (dialogueUI != null && dialogueUI.hintText != null)
        {
            if (newHint.Contains("V 확인완료"))
            {
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
        if (tutorialController != null) 
        { 
            _tutorialControllerGameObject = tutorialController.gameObject;
            tutorialController.enabled = false; 
            _tutorialControllerGameObject.SetActive(false); 
        }
        
        if (conductor != null) 
        { 
            _conductorGameObject = conductor.gameObject;
            _originalTutorialMode = conductor.isTutorialMode; 
            
            conductor.enabled = false; 
            // 🔥 [중요 수정] 오디오 재생을 위해 Conductor 오브젝트는 끄지 않습니다.
            // _conductorGameObject.SetActive(false); 
        }
    }
    
    void StartMainTutorial()
    {
        if (dialogueUI != null) dialogueUI.Hide();
        // 실습 단계로 넘어가면 대화용 김밥은 끕니다.
        if (kimbap010Prefab != null) kimbap010Prefab.SetActive(false); 
        
        if (_conductorGameObject != null && conductor != null)
        {
            // _conductorGameObject.SetActive(true); // 이미 켜져 있음
            conductor.enabled = true; // Update 다시 시작
            conductor.isTutorialMode = _originalTutorialMode;
            conductor.OnRoundResult.AddListener(OnRoundResult);
        }
        
        if (_tutorialControllerGameObject != null && tutorialController != null)
        {
            _tutorialControllerGameObject.SetActive(true);
            tutorialController.enabled = true;
            tutorialController.StartTutorial();
        }
        
        this.enabled = false;
    }
    
    void OnEnable() { if (conductor != null && conductor.enabled) conductor.OnRoundResult.AddListener(OnRoundResult); }
    void OnDisable() { if (conductor != null) conductor.OnRoundResult.RemoveListener(OnRoundResult); }
    void OnRoundResult(bool success) { _lastRoundResult = success; _roundResultProcessed = false; }
}
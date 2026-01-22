using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class TutorialController : MonoBehaviour
{
    [Header("Refs")]
    public RhythmConductor conductor;
    public FeedbackSetSO feedbackSet;
    public RadioClickable radio;
    
    public MissionBoardUI missionBoard; 

    [Header("UI Groups")]
    public GameObject salesUIGroup; // "면접 실습 / 진행 중" UI 그룹

    [Header("Monitor UI")]
    public TextMeshProUGUI monitorText; // 화면 중앙 텍스트 (피드백용)
    public TextMeshProUGUI salesText;   // "진행 중" 텍스트 (필요 시 제어)

    [Header("Final Result UI")]
    public GameObject finalResultPanel;        // 합격 결과창 패널
    public TextMeshProUGUI finalTotalText;     // 제목 ("** 합 격 **")
    public TextMeshProUGUI finalCommentText;   // 설명 ("영업 시작!")

    [Header("Tutorial Settings")]
    public RhythmTriggerListSO tutorialTriggerList;
    public int requiredSuccessCount = 1; 

    [Header("Wage Settings")]
    public int maxTotalWage = 100000; // (내부 계산용으로만 남겨둠)
    
    [Tooltip("대괄호 [ ] 안의 텍스트 색상")]
    public string highlightTextColor = "#FF3333"; 

    // 내부 변수
    int _currentStepIndex = 0;
    int _successCountThisStep = 0; 
    bool _tutorialCompleted = false;
    bool _isProcessingResult = false;
    float _lastSkipInput = -999f;
    
    // 시작 시 설정
    void Start()
    {
        if (conductor) 
        {
            conductor.isTutorialMode = true; 
            if (tutorialTriggerList) conductor.data = tutorialTriggerList;
            
            // 이벤트 연결 (중복 방지)
            conductor.OnSliceSuccess.RemoveListener(OnSliceSuccess);
            conductor.OnSliceSuccess.AddListener(OnSliceSuccess);
            
            conductor.OnRoundResult.RemoveListener(OnRoundResult);
            conductor.OnRoundResult.AddListener(OnRoundResult);
        }

        if (radio) 
        {
            radio.SetTutorialCompleted(false);
            radio.SetClickable(false);
        }

        // 시작할 때 "면접 실습" UI 켜기
        if (salesUIGroup) salesUIGroup.SetActive(true);
        if (salesText) salesText.text = "진행 중"; // 텍스트 초기화

        // 결과창 끄기
        if (finalResultPanel) finalResultPanel.SetActive(false);

        // 첫 미션 시작
        StartStep(0);
    }

    void OnDisable()
    {
        if (conductor)
        {
            conductor.OnSliceSuccess.RemoveListener(OnSliceSuccess);
            conductor.OnRoundResult.RemoveListener(OnRoundResult);
        }
    }

    // 트리거 버튼으로 스킵 및 게임 시작 처리
    void Update()
    {
        // 튜토리얼 완료 상태에서 트리거 누르면 본 게임 시작
        if (_tutorialCompleted && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            FinalizeAndStartGame();
        }

        // Y 버튼 누르면 강제 스킵 (테스트용)
        if (OVRInput.GetDown(OVRInput.Button.Two)) 
        {
            if (Time.time - _lastSkipInput > 1.0f)
            {
                _lastSkipInput = Time.time;
                SkipTutorial();
            }
        }
    }

    void StartStep(int stepIndex)
    {
        _currentStepIndex = stepIndex;
        _successCountThisStep = 0;
        
        // 리갈패드 업데이트
        if (missionBoard)
        {
            missionBoard.UpdateHeader("< 면접 실습 >");
            missionBoard.UpdateMission("김밥을 정확하게 썰어보세요.", 0, requiredSuccessCount);
        }

        // 모니터 텍스트 업데이트
        ShowMonitorText("실습 시작", Color.yellow);
        Invoke(nameof(ClearMonitorText), 2f);
    }

    // 김밥 썰기 성공 시
    void OnSliceSuccess(Vector3 pos, Vector3 normal, float speed)
    {
        if (_tutorialCompleted) return;

        // 화면에 피드백 표시
        ShowMonitorText("잘했습니다!", Color.green);
        Invoke(nameof(ClearMonitorText), 1f);
    }

    // 라운드 결과 (김밥 한 줄 끝)
    void OnRoundResult(bool isSuccess)
    {
        if (_tutorialCompleted || _isProcessingResult) return;

        if (isSuccess)
        {
            _successCountThisStep++;
            
            // 리갈패드 갱신
            if (missionBoard) 
            {
                missionBoard.UpdateMission("김밥을 정확하게 썰어보세요.", _successCountThisStep, requiredSuccessCount);
            }

            // 목표 달성 시 합격 처리
            if (_successCountThisStep >= requiredSuccessCount)
            {
                _isProcessingResult = true;
                Invoke(nameof(CompleteTutorial), 1.0f);
            }
        }
    }

    // 🔥 [수정 1] 합격 화면 띄울 때, 뒤에 있는 UI 그룹 통째로 숨기기 (겹침 방지)
    void CompleteTutorial()
    {
        _tutorialCompleted = true;

        string bigTitle = "** 합 격 **"; 
        string finalMessage = "<color=#505050>합격이네.\n자 이제 영업을 시작하자.</color>";
        string startHint = "\n<color=#FF8000><b>[오른손 트리거] 영업 시작!</b></color>";

        // ★ 핵심: 겹침 방지를 위해 UI 그룹을 통째로 끕니다.
        if (salesUIGroup) salesUIGroup.SetActive(false); 
        
        // 혹시 모르니 개별 텍스트도 끔
        if (monitorText) monitorText.gameObject.SetActive(false);

        // 1. 리갈패드 도장 쾅
        if (missionBoard)
        {
            missionBoard.ShowSuccessStamp(true);
            missionBoard.UpdateHeader("< 채용 결과 >");
            // 체크박스 없이 합격 문구만 깔끔하게
            missionBoard.UpdateText("< 채용 결과 >", "<size=180%><color=red>합격!</color></size>", ""); 
        }
        
        // 2. 모니터 결과창 켜기
        if (finalResultPanel)
        {
            finalResultPanel.SetActive(true);
            if (finalTotalText) finalTotalText.text = bigTitle;
            if (finalCommentText) 
            { 
                finalCommentText.text = finalMessage + startHint; 
                finalCommentText.color = Color.white; 
            }
        }
        
        // 3. 모니터 중앙 텍스트 (합격 메시지)
        ShowMonitorText(finalMessage, Color.cyan);
    }

    void FinalizeAndStartGame()
    {
        Cleanup();
        // 본 게임 전환 이벤트 발생 (StageManager가 받음)
        if (conductor.OnTutorialCompleted != null) conductor.OnTutorialCompleted.Invoke();
    }

    // 🔥 [수정 2] 본 게임 넘어갈 때 결과창 치우고, UI 초기화
    void Cleanup()
    {
        // 튜토리얼 모드 해제
        if (conductor) { conductor.isTutorialMode = false; conductor.StopAllGuideBeats(); }
        
        // 1. 결과창(합격 화면) 닫기
        if (finalResultPanel) finalResultPanel.SetActive(false);
        if (monitorText) monitorText.gameObject.SetActive(false); // 중앙 텍스트도 끄기

        // 2. Sales UI 그룹 다시 켜기 (본 게임용)
        if (salesUIGroup) salesUIGroup.SetActive(true);
        
        // 라디오 원상복구
        if (radio) { radio.SetTutorialCompleted(true); radio.SetClickable(true); }
    }

    void SkipTutorial()
    {
        _tutorialCompleted = true;
        FinalizeAndStartGame();
    }

    void ShowMonitorText(string text, Color color)
    {
        if (monitorText) 
        { 
            monitorText.text = text; 
            monitorText.color = color; 
            monitorText.gameObject.SetActive(true); 
        }
    }

    void ClearMonitorText()
    {
        if (monitorText) monitorText.text = "";
    }
}
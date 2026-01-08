using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public enum GameMode { Tutorial, MainGame }

    [Header("Refs")]
    public RhythmConductor conductor;
    public RadioClickable radio;

    [Header("Data")]
    public RhythmTriggerListSO tutorialData;
    public RhythmTriggerListSO mainData;

    [Header("Tutorial Progress")]
    public int tutorialSuccessToAdvance = 3;        // "단일 트리거 성공" 누적 N회
    public int failStreakToAssist = 3;              // 이 이상 실패 연속이면 Assist 켬
    public float assistJudgeMultiplier = 1.3f;      // 제한시간 증가

    [Header("Runtime")]
    public GameMode mode = GameMode.Tutorial;
    public int tutorialSuccessCount = 0;
    public int failStreak = 0;
    public bool assistOn = false;

    void Awake()
    {
        if (radio)
        {
            radio.onSingleClick.AddListener(OnRadioSingleClick);
            radio.onDoubleClick.AddListener(OnRadioDoubleClick);
        }

        if (conductor)
        {
            conductor.OnTriggerResolved += HandleTriggerResolved;
        }
    }

    void Start()
    {
        EnterTutorial();
    }

    public void EnterTutorial()
    {
        mode = GameMode.Tutorial;
        tutorialSuccessCount = 0;
        failStreak = 0;
        assistOn = false;

        conductor.judgeDurationMultiplier = 1f;
        conductor.SetData(tutorialData);
        conductor.PlayFromStart();
    }

    public void EnterMainGame()
    {
        mode = GameMode.MainGame;

        conductor.judgeDurationMultiplier = 1f;
        conductor.SetData(mainData);
        conductor.PlayFromStart();
    }

    void HandleTriggerResolved(int idx, bool success, int sliceCount, int required)
    {
        if (mode != GameMode.Tutorial) return;

        if (success)
        {
            tutorialSuccessCount++;
            failStreak = 0;

            // 목표 달성 → 라디오로 메인 시작(유저 설계)
            // 여기선 자동 진입하지 않고 "라디오 클릭"을 기다림.
            // (원하면 여기서 바로 EnterMainGame() 호출로 바꿀 수 있음)
        }
        else
        {
            failStreak++;

            // Assist는 “제한시간 증가”만 적용(확정)
            if (!assistOn && failStreak >= failStreakToAssist)
            {
                assistOn = true;
                conductor.judgeDurationMultiplier = assistJudgeMultiplier;
            }
        }
    }

    void OnRadioSingleClick()
    {
        // 튜토리얼을 "충분히 성공했을 때만" 단일 클릭으로 메인 진입
        if (mode == GameMode.Tutorial)
        {
            if (tutorialSuccessCount >= tutorialSuccessToAdvance)
            {
                EnterMainGame();
            }
            // 아직 부족하면 단일 클릭은 무시(또는 안내 SFX만)
        }
    }

    void OnRadioDoubleClick()
    {
        // 튜토리얼 중 2연타 = 스킵(요구사항)
        if (mode == GameMode.Tutorial)
        {
            EnterMainGame();
        }
    }
}

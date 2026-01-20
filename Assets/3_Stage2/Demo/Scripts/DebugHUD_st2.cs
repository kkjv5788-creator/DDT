using UnityEngine;
using static GameState_st2;

public class DebugHUD_st2 : MonoBehaviour
{
    [Header("설정")]
    public bool alwaysOn = true; // Quest 빌드에서는 true
    public KeyCode toggleKey = KeyCode.F1; // PC 디버그용

    [Header("참조")]
    public GameFlowController_st2 gameFlow;
    public JudgeSystem_st2 judgeSystem;
    public EconomySystem_st2 economySystem;
    public TutorialManager_st2 tutorialManager;
    public HandCatchSensor_st2 leftSensor;
    public HandCatchSensor_st2 rightSensor;
    public CatchInput_st2 leftInput;
    public CatchInput_st2 rightInput;
    public MoldController_st2[] molds;

    private bool isVisible = true;

    void Start()
    {
        if (alwaysOn)
        {
            isVisible = true;
        }
    }

    void Update()
    {
        // PC 디버그: F1로 토글
        if (!alwaysOn && Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }
    }

    void OnGUI()
    {
        if (!isVisible) return;

        // 기본 스타일 설정
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 16;
        labelStyle.normal.textColor = Color.white;
        labelStyle.padding = new RectOffset(5, 5, 2, 2);

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));

        float x = 10;
        float y = 10;
        float width = 350;
        float lineHeight = 22;

        // 배경 박스
        GUI.Box(new Rect(x, y, width, 280), "", boxStyle);

        y += 10;

        // === 게임 상태 ===
        GUI.Label(new Rect(x, y, width, lineHeight),
            $"<b>STATE:</b> {gameFlow.CurrentState}", labelStyle);
        y += lineHeight;

        // === 튜토리얼 진행 (Tutorial 모드일 때만) ===
        if (gameFlow.CurrentState == GameStatest2.Tutorial && tutorialManager != null)
        {
            GUI.Label(new Rect(x, y, width, lineHeight),
                $"<b>Tutorial:</b> {tutorialManager.GetCurrentStageInfo()}", labelStyle);
            y += lineHeight;
        }

        // === 누적 카운트 ===
        GUI.Label(new Rect(x, y, width, lineHeight),
            $"<b>Perfect:</b> {judgeSystem.perfectCount}  <b>Good:</b> {judgeSystem.goodCount}  <b>Miss:</b> {judgeSystem.missCount}",
            labelStyle);
        y += lineHeight;

        // === 마지막 판정 + 손 ===
        string lastJudge = "None";
        string lastHand = "-";

        if (leftInput != null && leftInput.lastJudgeResult != JudgeResult_st2.Miss)
        {
            lastJudge = leftInput.lastJudgeResult.ToString();
            lastHand = leftInput.lastHandName;
        }
        else if (rightInput != null && rightInput.lastJudgeResult != JudgeResult_st2.Miss)
        {
            lastJudge = rightInput.lastJudgeResult.ToString();
            lastHand = rightInput.lastHandName;
        }

        GUI.Label(new Rect(x, y, width, lineHeight),
            $"<b>Last Judge:</b> {lastJudge} ({lastHand})", labelStyle);
        y += lineHeight;

        // === 일급 ===
        GUI.Label(new Rect(x, y, width, lineHeight),
            $"<b>Wage:</b> {economySystem.currentWage:N0}원", labelStyle);
        y += lineHeight;

        // === 타겟 여부 ===
        bool leftHasTarget = leftSensor != null && leftSensor.currentTarget != null;
        bool rightHasTarget = rightSensor != null && rightSensor.currentTarget != null;

        string leftStatus = leftHasTarget ? "TARGET" : "-";
        string rightStatus = rightHasTarget ? "TARGET" : "-";

        GUI.Label(new Rect(x, y, width, lineHeight),
            $"<b>L Hand:</b> {leftStatus}  <b>R Hand:</b> {rightStatus}", labelStyle);
        y += lineHeight;

        // === ActiveFish 수 ===
        int totalActiveFish = 0;
        if (molds != null)
        {
            foreach (var mold in molds)
            {
                if (mold != null)
                {
                    totalActiveFish += mold.GetActiveFishCount();
                }
            }
        }

        GUI.Label(new Rect(x, y, width, lineHeight),
            $"<b>Active Fish:</b> {totalActiveFish}", labelStyle);
        y += lineHeight;

        // === 시간 정보 (Playing 중일 때만) ===
        if (gameFlow.CurrentState == GameStatest2.Playing && gameFlow.bgmSource != null)
        {
            float currentTime = gameFlow.bgmSource.time;
            float totalTime = gameFlow.bgmSource.clip != null ? gameFlow.bgmSource.clip.length : 0;

            int currMin = (int)(currentTime / 60);
            int currSec = (int)(currentTime % 60);
            int totalMin = (int)(totalTime / 60);
            int totalSec = (int)(totalTime % 60);

            GUI.Label(new Rect(x, y, width, lineHeight),
                $"<b>Time:</b> {currMin:00}:{currSec:00} / {totalMin:00}:{totalSec:00}", labelStyle);
            y += lineHeight;
        }

        // === 토글 안내 (alwaysOn이 아닐 때만) ===
        if (!alwaysOn)
        {
            y += 10;
            GUI.Label(new Rect(x, y, width, lineHeight),
                $"[{toggleKey}] Toggle HUD", labelStyle);
        }
    }

    // 단색 텍스처 생성 헬퍼
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
using System;
using System.Collections;
using UnityEngine;
using static GameState_st2;

/// <summary>
/// Stage2 전체 흐름 제어:
/// - Panel_SelectMode에서 튜토리얼/메인 선택
/// - Panel_Pause(A버튼) 일시정지: 결과 화면이 아닐 때(튜토리얼/메인) 호출
/// - Panel_ClosingResult / Panel_Pause의 다시하기/메인으로: SelectMode로 복귀 + 완전 초기화
/// </summary>
public class GameFlowController_st2 : MonoBehaviour
{
    public static GameFlowController_st2 Instance { get; private set; }

    [Header("게임 상태")]
    public GameStatest2 CurrentState = GameStatest2.MainMenu;
    public bool isEndingTriggered = false;

    public event Action<GameStatest2> OnStateChanged;

    [Header("오디오(메인 BGM)")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;

    [Header("BGM 루프 설정")]
    public bool bgmLoopEnabled = true;
    [Min(1)] public int bgmLoopCount = 4;   // 18초 클립을 4번 재생 후 종료

    [Header("시스템 참조")]
    public JudgeSystem_st2 judgeSystem;
    public EconomySystem_st2 economySystem;
    public BagManager_st2 bagManager;
    public PatternDirector_st2 patternDirector;

    [Tooltip("MenuMonitorUI는 제거 예정. 대신 Stage2UIController_st2를 사용.")]
    public Stage2UIController_st2 uiController;

    [Header("메인 시스템 오브젝트")]
    public MoldController_st2[] molds;
    public CatchInput_st2[] catchInputs;

    [Header("튜토리얼(옵션)")]
    public StandaloneTutorialController_st2 tutorialController;

    [Header("Pause 시 상호작용 차단(선택)")]
    [Tooltip("Pause 시 SetActive(false)로 꺼버릴 루트/오브젝트들(예: MainRoot, TutorialRoot, 상호작용 오브젝트들). PauseMenu는 넣지 마세요.")]
    public GameObject[] pauseBlockObjects;

    // 타이밍 (DSP 기반)
    private double songStartDspTime;
    private double songEndDspTime;
    private double pausedDspAccum = 0.0;
    private double pauseStartDspTime = 0.0;

    private GameStatest2 stateBeforePause = GameStatest2.MainMenu;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // 시스템 초기화
        if (molds != null)
        {
            foreach (var mold in molds)
                if (mold != null) mold.Initialize(judgeSystem);
        }

        if (uiController != null)
            uiController.Initialize(economySystem, bagManager, patternDirector, judgeSystem);

        // 중앙 이벤트 연결
        if (judgeSystem != null)
            judgeSystem.OnJudgeResult += OnJudgeResult;

        // 시작은 무조건 SelectMode(사용자 선택)
        TransitionToMainMenu();
    }

    void Update()
    {
        // ✅ A 버튼(오큘러스 오른손 A = Button.One) → 결과 화면이 아닐 때 Pause 토글
        // (튜토리얼 스킵은 B로 변경됨)
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            if (CurrentState == GameStatest2.Playing || CurrentState == GameStatest2.Tutorial || CurrentState == GameStatest2.Paused)
            {
                TogglePause();
            }
        }

        // 곡 종료 체크 (총 루프 횟수 기준 / Pause 보정 포함)
        if (CurrentState == GameStatest2.Playing && !isEndingTriggered)
        {
            if (GetSongProgress01(out double elapsed, out double total, out double _))
            {
                if (elapsed >= total)
                    TriggerEnding();
            }
        }
    }

    // =========================
    // External UI Entry Points
    // =========================

    public void StartTutorialFromMenu()
    {
        TransitionToTutorial();
    }

    public void StartMainGameFromMenu()
    {
        TransitionToPlaying();
    }

    public void BackToSelectMode()
    {
        TransitionToMainMenu();
    }

    public void RestartToSelectMode()
    {
        // 게임을 "다시하기" 요구사항: SelectMode로 복귀 + 완전 초기화 + BGM도 초기화
        TransitionToMainMenu();
    }

    // =========================
    // State Transitions
    // =========================

    public void TransitionToMainMenu()
    {
        // 예약 이벤트 정리
        if (patternDirector != null) patternDirector.ClearAllScheduledEvents();

        // Mold 스폰 취소
        if (molds != null)
        {
            foreach (var mold in molds)
                if (mold != null) mold.CancelAllScheduledSpawns();
        }

        // 오디오 정지
        if (bgmSource != null) bgmSource.Stop();

        // 시간/오디오/플래그 초기화
        Time.timeScale = 1f;
        AudioListener.pause = false;

        ResetGameState();

        // 메인/튜토리얼 비활성화
        SetMainGameActive(false);
        if (tutorialController != null) tutorialController.gameObject.SetActive(false);

        // UI는 SelectMode
        SetState(GameStatest2.MainMenu);
    }

    public void TransitionToTutorial()
    {
        // 완전 초기화 후 튜토리얼 진입
        ResetGameState();

        // 메인 비활성화
        SetMainGameActive(false);

        // 튜토리얼 활성화
        if (tutorialController != null)
            tutorialController.gameObject.SetActive(true);

        // BGM(메인)은 끔
        if (bgmSource != null) bgmSource.Stop();

        SetState(GameStatest2.Tutorial);
    }

    public void TransitionToPlaying()
    {
        ResetGameState();

        // 튜토리얼 완전 비활성화
        if (tutorialController != null)
            tutorialController.gameObject.SetActive(false);

        // 메인 활성화 + 초기화
        SetMainGameActive(true);

        if (judgeSystem != null) judgeSystem.ResetStats();
        if (economySystem != null) economySystem.Reset();
        if (bagManager != null) bagManager.Reset();

        // ===== BGM 시작 (루프 N회 후 종료) =====
        songStartDspTime = AudioSettings.dspTime;
        pausedDspAccum = 0.0;
        pauseStartDspTime = 0.0;

        if (bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = bgmLoopEnabled;

            int loops = (bgmLoopEnabled ? Mathf.Max(1, bgmLoopCount) : 1);
            double totalDuration = (bgmClip != null ? bgmClip.length : 0.0) * loops;

            songEndDspTime = songStartDspTime + totalDuration;
            bgmSource.Play();
        }
        else
        {
            songEndDspTime = songStartDspTime + 0.1;
        }

        if (patternDirector != null)
            patternDirector.StartPatternSystem(songStartDspTime);

        SetState(GameStatest2.Playing);
    }

    // =========================
    // Pause
    // =========================

    public void TogglePause()
    {
        if (CurrentState == GameStatest2.Paused) ResumeGame();
        else PauseGame();
    }

    void PauseGame()
    {
        if (CurrentState == GameStatest2.Result || CurrentState == GameStatest2.MainMenu) return;

        stateBeforePause = CurrentState;
        SetState(GameStatest2.Paused);

        Time.timeScale = 0f;
        AudioListener.pause = true;

        // 메인 BGM pause
        if (bgmSource != null) bgmSource.Pause();

        // 튜토리얼 BGM pause
        if (tutorialController != null && tutorialController.tutorialBGMSource != null)
            tutorialController.tutorialBGMSource.Pause();

        // DSP 보정용
        pauseStartDspTime = AudioSettings.dspTime;

        // 상호작용 차단(옵션): 루트 꺼버리기
        SetPauseBlockObjectsActive(false);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameStatest2.Paused) return;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        // BGM resume
        if (bgmSource != null) bgmSource.UnPause();
        if (tutorialController != null && tutorialController.tutorialBGMSource != null)
            tutorialController.tutorialBGMSource.UnPause();

        // Pause 동안 흐른 dspTime 누적 보정
        pausedDspAccum += (AudioSettings.dspTime - pauseStartDspTime);

        // 루트 복구
        SetPauseBlockObjectsActive(true);

        // 원래 상태로 복귀
        SetState(stateBeforePause);
    }

    void SetPauseBlockObjectsActive(bool active)
    {
        if (pauseBlockObjects == null) return;
        for (int i = 0; i < pauseBlockObjects.Length; i++)
        {
            var go = pauseBlockObjects[i];
            if (go == null) continue;
            go.SetActive(active);
        }
    }

    // =========================
    // Ending / Result
    // =========================

    void TriggerEnding()
    {
        isEndingTriggered = true;

        // Mold 스폰 취소
        if (molds != null)
        {
            foreach (var mold in molds)
                if (mold != null) mold.CancelAllScheduledSpawns();
        }

        // 남은 fish 강제 Miss 처리
        if (judgeSystem != null) judgeSystem.ForceResolveAll();

        // 현재 봉투 완성
        if (bagManager != null) bagManager.FinalizeBag();

        StartCoroutine(TransitionToResultDelayed(0.5f));
    }

    IEnumerator TransitionToResultDelayed(float delay)
    {
        // TimeScale 0에 멈추지 않게 WaitForSecondsRealtime 사용
        yield return new WaitForSecondsRealtime(delay);
        TransitionToResult();
    }

    void TransitionToResult()
    {
        // Result 진입 전 Pause 해제
        if (CurrentState == GameStatest2.Paused)
            ResumeGame();

        SetState(GameStatest2.Result);
    }

    // =========================
    // Helpers
    // =========================

    void SetMainGameActive(bool active)
    {
        if (molds != null)
        {
            foreach (var mold in molds)
                if (mold != null) mold.gameObject.SetActive(active);
        }

        if (catchInputs != null)
        {
            foreach (var input in catchInputs)
            {
                if (input == null) continue;
                input.enabled = active;
                if (active) input.lastJudgeResult = JudgeResult_st2.Miss;
            }
        }
    }

    void ResetGameState()
    {
        isEndingTriggered = false;

        // 타이밍 초기화
        songStartDspTime = 0.0;
        songEndDspTime = 0.0;
        pausedDspAccum = 0.0;
        pauseStartDspTime = 0.0;
    }

    void SetState(GameStatest2 newState)
    {
        CurrentState = newState;

        if (uiController != null)
            uiController.UpdateState(newState);

        OnStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// 메인 BGM의 "Pause 보정 포함" 진행시간/총시간/정규화 값을 얻는다.
    /// </summary>
    public bool GetSongProgress01(out double effectiveElapsed, out double totalDuration, out double normalized01)
    {
        effectiveElapsed = 0;
        totalDuration = 0;
        normalized01 = 0;

        if (songEndDspTime <= songStartDspTime) return false;

        totalDuration = (songEndDspTime - songStartDspTime);
        effectiveElapsed = (AudioSettings.dspTime - songStartDspTime) - pausedDspAccum;
        normalized01 = Mathf.Clamp01((float)(effectiveElapsed / totalDuration));
        return true;
    }

    // 중앙 이벤트 핸들러
    void OnJudgeResult(JudgeResult_st2 result, float delta)
    {
        if (economySystem != null)
            economySystem.ApplyJudgeResult(result);
    }
}

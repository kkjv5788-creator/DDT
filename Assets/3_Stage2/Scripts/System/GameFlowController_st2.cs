using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("오디오(SelectMode BGM)")]
    public AudioSource menuBgmSource;
    public AudioClip menuBgmClip;
    public bool menuBgmLoop = true;
    [Range(0f, 1f)] public float menuBgmVolume = 0.6f;

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

    [Header("Pause: Disable these behaviours instead of SetActive")]
    public Behaviour[] pauseDisableBehaviours;


    [Header("Scene Load")]
    public string mainSceneName = "MainScene"; // 너 메인 씬 이름으로 바꿔


    // 타이밍 (DSP 기반)
    private double songStartDspTime;
    private double songEndDspTime;
    private double pausedDspAccum = 0.0;
    private double pauseStartDspTime = 0.0;

    private GameStatest2 stateBeforePause = GameStatest2.MainMenu;
    private bool pausedTimeScale = false; // ✅ 실제 게임 진행중일 때만 timeScale/audio를 멈출지

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
            if (CurrentState == GameStatest2.Playing || CurrentState == GameStatest2.Tutorial || CurrentState == GameStatest2.Paused || CurrentState == GameStatest2.MainMenu)
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
    void PlayMenuBgm()
    {
        if (menuBgmSource == null || menuBgmClip == null) return;

        if (menuBgmSource.isPlaying && menuBgmSource.clip == menuBgmClip) return;

        menuBgmSource.Stop();
        menuBgmSource.clip = menuBgmClip;
        menuBgmSource.loop = menuBgmLoop;
        menuBgmSource.volume = menuBgmVolume;
        menuBgmSource.Play();
    }

    void StopMenuBgm()
    {
        if (menuBgmSource == null) return;
        if (menuBgmSource.isPlaying) menuBgmSource.Stop();
    }
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
        ForceUnpauseIfNeeded();
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

        PlayMenuBgm();
        // UI는 SelectMode
        SetState(GameStatest2.MainMenu);
    }

    public void TransitionToTutorial()
    {
        ForceUnpauseIfNeeded();
        // 완전 초기화 후 튜토리얼 진입
        ResetGameState();

        // 메인 비활성화
        SetMainGameActive(false);

        StopMenuBgm();
        // 튜토리얼 활성화
        if (tutorialController != null)
            tutorialController.gameObject.SetActive(true);

        // BGM(메인)은 끔
        if (bgmSource != null) bgmSource.Stop();

        SetState(GameStatest2.Tutorial);
    }

    public void TransitionToPlaying()
    {
        ForceUnpauseIfNeeded();
        ResetGameState();

        // 튜토리얼 완전 비활성화
        if (tutorialController != null)
            tutorialController.gameObject.SetActive(false);
        StopMenuBgm();

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
        if (CurrentState == GameStatest2.Result) return;

        // ✅ 지금 상태 저장
        stateBeforePause = CurrentState;

        // ✅ Playing/Tutorial일 때만 '진짜 정지' (메뉴에서는 UI 애니메이션 멈출 수 있어서)
        pausedTimeScale = (stateBeforePause == GameStatest2.Playing || stateBeforePause == GameStatest2.Tutorial);

        // ✅ Pause 보정 시작점 기록 (Playing일 때만 의미있긴 하지만, 있어도 무해)
        pauseStartDspTime = AudioSettings.dspTime;

        // ✅ 상태 먼저 Paused로
        SetState(GameStatest2.Paused);

        if (pausedTimeScale)
        {
            // (선택) 예약 이벤트/스폰 정리
            if (patternDirector != null) patternDirector.ClearAllScheduledEvents();
            if (molds != null)
            {
                foreach (var mold in molds)
                    if (mold != null) mold.CancelAllScheduledSpawns();
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;

            if (bgmSource != null) bgmSource.Pause();
            if (tutorialController != null && tutorialController.tutorialBGMSource != null)
                tutorialController.tutorialBGMSource.Pause();

            SetPauseBehavioursEnabled(false);
        }
        else
        {
            // ✅ MainMenu에서 Pause 걸면: 메뉴 BGM만 멈추고 싶으면 이거 켜
            if (menuBgmSource != null) menuBgmSource.Pause();

            // TimeScale/AudioListener는 건드리지 않음
        }
    }

    public void ResumeGame()
    {
        if (CurrentState != GameStatest2.Paused) return;

        // ✅ Pause 보정 누적 (Playing/Tutorial에서만 실질적으로 의미)
        double now = AudioSettings.dspTime;
        if (pauseStartDspTime > 0.0)
            pausedDspAccum += (now - pauseStartDspTime);
        pauseStartDspTime = 0.0;

        if (pausedTimeScale)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (bgmSource != null) bgmSource.UnPause();
            if (tutorialController != null && tutorialController.tutorialBGMSource != null)
                tutorialController.tutorialBGMSource.UnPause();

            SetPauseBehavioursEnabled(true);
        }
        else
        {
            // ✅ MainMenu였다면 메뉴 BGM 다시 재생
            if (menuBgmSource != null) menuBgmSource.UnPause();
        }

        SetState(stateBeforePause);
    }


    void SetPauseBehavioursEnabled(bool enabled)
    {
        if (pauseDisableBehaviours == null) return;
        foreach (var b in pauseDisableBehaviours)
        {
            if (b == null) continue;
            b.enabled = enabled;
        }
    }




    void ForceUnpauseIfNeeded()
    {
        // timeScale / 오디오 복구
        Time.timeScale = 1f;
        AudioListener.pause = false;

       


        // BGM도 혹시 pause 걸려있으면 풀어줌(선택)
        if (bgmSource != null) bgmSource.UnPause();
        if (tutorialController != null && tutorialController.tutorialBGMSource != null)
            tutorialController.tutorialBGMSource.UnPause();

        // 상태가 Paused였다면 이전 상태로 복귀까지는 필요 없고,
        // 여기서는 "오브젝트 복구"만 보장하면 됨.
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

    public void LoadMainScene()
    {
        ForceUnpauseIfNeeded();
        // 혹시 Pause/Result 상태에서 나갈 때 안전 정리
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (bgmSource != null) bgmSource.Stop();

        // 튜토리얼 BGM도 정리
        if (tutorialController != null && tutorialController.tutorialBGMSource != null)
            tutorialController.tutorialBGMSource.Stop();
        StopMenuBgm();

        // 예약 이벤트/스폰 정리(선택이지만 추천)
        if (patternDirector != null) patternDirector.ClearAllScheduledEvents();
        if (molds != null)
        {
            foreach (var mold in molds)
                if (mold != null) mold.CancelAllScheduledSpawns();
        }

        SceneManager.LoadScene(mainSceneName);
    }
}

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
    [Min(1)] public int bgmLoopCount = 4;

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
    public string mainSceneName = "MainScene";

    // 타이밍 (DSP 기반)
    private double songStartDspTime;
    private double songEndDspTime;
    private double pausedDspAccum = 0.0;
    private double pauseStartDspTime = 0.0;

    private GameStatest2 stateBeforePause = GameStatest2.MainMenu;
    private bool pausedTimeScale = false;

    // ✅ Stage2 최고기록 저장 중복 방지
    private bool _bestSavedThisRun = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (molds != null)
        {
            foreach (var mold in molds)
                if (mold != null) mold.Initialize(judgeSystem);
        }

        if (uiController != null)
            uiController.Initialize(economySystem, bagManager, patternDirector, judgeSystem);

        if (judgeSystem != null)
            judgeSystem.OnJudgeResult += OnJudgeResult;

        TransitionToMainMenu();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            if (CurrentState == GameStatest2.Playing || CurrentState == GameStatest2.Tutorial ||
                CurrentState == GameStatest2.Paused || CurrentState == GameStatest2.MainMenu)
            {
                TogglePause();
            }
        }

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

    public void StartTutorialFromMenu() => TransitionToTutorial();
    public void StartMainGameFromMenu() => TransitionToPlaying();
    public void BackToSelectMode() => TransitionToMainMenu();
    public void RestartToSelectMode() => TransitionToMainMenu();

    // =========================
    // State Transitions
    // =========================
    public void TransitionToMainMenu()
    {
        ForceUnpauseIfNeeded();

        if (patternDirector != null) patternDirector.ClearAllScheduledEvents();

        if (molds != null)
        {
            foreach (var mold in molds)
                if (mold != null) mold.CancelAllScheduledSpawns();
        }

        if (bgmSource != null) bgmSource.Stop();

        Time.timeScale = 1f;
        AudioListener.pause = false;

        ResetGameState();

        SetMainGameActive(false);
        if (tutorialController != null) tutorialController.gameObject.SetActive(false);

        PlayMenuBgm();
        SetState(GameStatest2.MainMenu);
    }

    public void TransitionToTutorial()
    {
        ForceUnpauseIfNeeded();
        ResetGameState();

        SetMainGameActive(false);

        StopMenuBgm();
        if (tutorialController != null)
            tutorialController.gameObject.SetActive(true);

        if (bgmSource != null) bgmSource.Stop();

        SetState(GameStatest2.Tutorial);
    }

    public void TransitionToPlaying()
    {
        ForceUnpauseIfNeeded();
        ResetGameState();

        if (tutorialController != null)
            tutorialController.gameObject.SetActive(false);
        StopMenuBgm();

        SetMainGameActive(true);

        if (judgeSystem != null) judgeSystem.ResetStats();
        if (economySystem != null) economySystem.Reset();
        if (bagManager != null) bagManager.Reset();

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

        stateBeforePause = CurrentState;
        pausedTimeScale = (stateBeforePause == GameStatest2.Playing || stateBeforePause == GameStatest2.Tutorial);

        pauseStartDspTime = AudioSettings.dspTime;
        SetState(GameStatest2.Paused);

        if (pausedTimeScale)
        {
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
            if (menuBgmSource != null) menuBgmSource.Pause();
        }
    }

    public void ResumeGame()
    {
        if (CurrentState != GameStatest2.Paused) return;

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
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (bgmSource != null) bgmSource.UnPause();
        if (tutorialController != null && tutorialController.tutorialBGMSource != null)
            tutorialController.tutorialBGMSource.UnPause();
    }

    // =========================
    // Ending / Result
    // =========================
    void TriggerEnding()
    {
        isEndingTriggered = true;

        if (molds != null)
        {
            foreach (var mold in molds)
                if (mold != null) mold.CancelAllScheduledSpawns();
        }

        if (judgeSystem != null) judgeSystem.ForceResolveAll();
        if (bagManager != null) bagManager.FinalizeBag();

        StartCoroutine(TransitionToResultDelayed(0.5f));
    }

    IEnumerator TransitionToResultDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        TransitionToResult();
    }

    void TransitionToResult()
    {
        if (CurrentState == GameStatest2.Paused)
            ResumeGame();

        // ✅ Stage2 최고기록(원) 갱신: 결과 진입 시 1회만
        if (!_bestSavedThisRun && economySystem != null)
        {
            BestWageStore_st2.TryUpdateBest("stage2", economySystem.currentWage);
            _bestSavedThisRun = true;
        }

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

        // ✅ 최고기록 저장 플래그 초기화
        _bestSavedThisRun = false;

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

    void OnJudgeResult(JudgeResult_st2 result, float delta)
    {
        if (economySystem != null)
            economySystem.ApplyJudgeResult(result);
    }

    // ✅ Stage2에서 Main 씬으로 돌아가기
    public void LoadMainScene()
    {
        ForceUnpauseIfNeeded();

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (bgmSource != null) bgmSource.Stop();

        if (tutorialController != null && tutorialController.tutorialBGMSource != null)
            tutorialController.tutorialBGMSource.Stop();
        StopMenuBgm();

        if (patternDirector != null) patternDirector.ClearAllScheduledEvents();
        if (molds != null)
        {
            foreach (var mold in molds)
                if (mold != null) mold.CancelAllScheduledSpawns();
        }

        // ✅ Stage2 → Main 복귀 표시 (다음 Main은 Lobby 스폰)
        if (MainEntryState_st2.Instance != null)
            MainEntryState_st2.Instance.MarkReturnToMain();

        SceneManager.LoadScene(mainSceneName);
    }
}


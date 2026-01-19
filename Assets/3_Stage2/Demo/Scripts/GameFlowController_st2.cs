using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameState_st2;

public class GameFlowController_st2 : MonoBehaviour
{
    public static GameFlowController_st2 Instance { get; private set; }

    [Header("게임 상태")]
    public GameStatest2 CurrentState = GameStatest2.MainMenu;
    public bool isEndingTriggered = false;

    [Header("오디오")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;

    [Header("시스템 참조")]
    public JudgeSystem_st2 judgeSystem;
    public EconomySystem_st2 economySystem;
    public BagManager_st2 bagManager;
    public CrowdManager_st2 crowdManager;
    public PatternDirector_st2 patternDirector;
    public MenuMonitorUI_st2 menuMonitorUI;
    public TutorialManager_st2 tutorialManager;
    public MoldController_st2[] molds;
    public CatchInput_st2[] catchInputs;

    [Header("타이밍")]
    private double songStartDspTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 시스템 초기화
        foreach (var mold in molds)
        {
            mold.Initialize(judgeSystem);
        }

        foreach (var input in catchInputs)
        {
            input.Initialize(judgeSystem, bagManager, economySystem, molds);
        }

        menuMonitorUI.Initialize(economySystem, bagManager, patternDirector, judgeSystem);

        // ✅ 수정: 중앙 이벤트 연결
        judgeSystem.OnJudgeResult += OnJudgeResult;
        bagManager.OnBagSwapped += OnBagSwapped;
        patternDirector.OnPatternQueued += OnPatternQueued;

        // ✅ 추가: 튜토리얼 연결 (각 Mold 이벤트)
        foreach (var mold in molds)
        {
            mold.OnTelegraphPlayed += () =>
            {
                if (tutorialManager != null && tutorialManager.gameObject.activeInHierarchy)
                {
                    tutorialManager.OnTelegraphPlayed();
                }
            };

            mold.OnPopPlayed += () =>
            {
                if (tutorialManager != null && tutorialManager.gameObject.activeInHierarchy)
                {
                    tutorialManager.OnPopPlayed();
                }
            };
        }

        // 튜토리얼 체크
        bool hasCompletedTutorial = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

        if (!hasCompletedTutorial)
        {
            TransitionToTutorial();
        }
        else
        {
            TransitionToPlaying();
        }
    }

    void Update()
    {
        // PAUSE 입력
        if (CurrentState == GameStatest2.Playing)
        {
            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                if (bgmSource.time > 0.1f)
                {
                    PauseGame();
                }
            }
        }

        // 곡 종료 체크
        if (CurrentState == GameStatest2.Playing && !isEndingTriggered)
        {
            if (bgmSource.time >= bgmClip.length)
            {
                TriggerEnding();
            }
        }

        // UI 업데이트
        if (CurrentState == GameStatest2.Playing)
        {
            menuMonitorUI.UpdateSongTime(bgmSource.time, bgmClip.length);
            menuMonitorUI.UpdateFilledBags(bagManager.GetFilledBagCount());
        }
    }

    public void TransitionToTutorial()
    {
        CurrentState = GameStatest2.Tutorial;
        menuMonitorUI.UpdateState(CurrentState);

        // 튜토리얼 시작
        tutorialManager.gameObject.SetActive(true);
    }

    public void TransitionToPlaying()
    {
        CurrentState = GameStatest2.Playing;
        isEndingTriggered = false;
        menuMonitorUI.UpdateState(CurrentState);

        // 튜토리얼 비활성화
        if (tutorialManager != null)
        {
            tutorialManager.gameObject.SetActive(false);
        }

        // 곡 시작
        songStartDspTime = AudioSettings.dspTime;
        bgmSource.clip = bgmClip;
        bgmSource.Play();

        // 시스템 시작
        crowdManager.Initialize(bgmSource, bgmClip);
        patternDirector.StartPatternSystem(songStartDspTime);
    }

    void PauseGame()
    {
        CurrentState = GameStatest2.Paused;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        bgmSource.Pause();

        menuMonitorUI.UpdateState(CurrentState);

        // PAUSE 메뉴 표시 (구현 필요)
    }

    public void ResumeGame()
    {
        CurrentState = GameStatest2.Playing;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        bgmSource.UnPause();

        menuMonitorUI.UpdateState(CurrentState);
    }

    public void RestartGame()
    {
        // 예약 이벤트 정리
        patternDirector.ClearAllScheduledEvents();

        // ✅ 추가: Mold 스폰 취소
        foreach (var mold in molds)
        {
            mold.CancelAllScheduledSpawns();
        }

        // PAUSE 해제
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 오디오 정지
        bgmSource.Stop();

        // 게임 상태 초기화
        ResetGameState();

        // 재시작
        TransitionToPlaying();
    }


    void ResetGameState()
    {
        judgeSystem.ResetStats();
        economySystem.Reset();
        bagManager.Reset();
        crowdManager.Reset();
        isEndingTriggered = false;
    }

    void TriggerEnding()
    {
        isEndingTriggered = true;

        // ✅ 추가: Mold 스폰 취소
        foreach (var mold in molds)
        {
            mold.CancelAllScheduledSpawns();
        }

        // ✅ 수정: 남은 fish 강제 Miss 처리
        judgeSystem.ForceResolveAll();

        // 현재 봉투 완성
        bagManager.FinalizeBag();

        // Result로 전환
        StartCoroutine(TransitionToResultDelayed(0.5f));
    }

    IEnumerator TransitionToResultDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        TransitionToResult();
    }

    void TransitionToResult()
    {
        CurrentState = GameStatest2.Result;
        menuMonitorUI.UpdateState(CurrentState);

        // Result 화면 표시 (구현 필요)
    }

    // ============================================================
    // ✅ 수정: 중앙 이벤트 핸들러 (Economy 자동 반영)
    // ============================================================

    void OnJudgeResult(JudgeResult_st2 result, float delta)
    {
        // ✅ Economy에 자동 반영 (Timeout Miss 포함)
        economySystem.ApplyJudgeResult(result);

        // 튜토리얼 모드일 때만 전달
        if (tutorialManager != null && tutorialManager.gameObject.activeInHierarchy)
        {
            tutorialManager.OnCatchSuccess(result);
        }
    }

    void OnBagSwapped()
    {
        // 튜토리얼 모드일 때만 전달
        if (tutorialManager != null && tutorialManager.gameObject.activeInHierarchy)
        {
            tutorialManager.OnBagSwapped();
        }
    }

    void OnPatternQueued(PatternType_st2 type)
    {
        // 튜토리얼 모드일 때만 전달
        if (tutorialManager != null && tutorialManager.gameObject.activeInHierarchy)
        {
            tutorialManager.OnPatternQueued(type);
        }
    }
}
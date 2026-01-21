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
    public MoldController_st2[] molds;
    public CatchInput_st2[] catchInputs;

    [Header("튜토리얼 (옵션)")]
    public StandaloneTutorialController_st2 tutorialController; // ✅ 추가

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

        // ✅ 중앙 이벤트 연결
        judgeSystem.OnJudgeResult += OnJudgeResult;
        bagManager.OnBagSwapped += OnBagSwapped;
        patternDirector.OnPatternQueued += OnPatternQueued;

        // ✅ 개발용: 매 실행마다 튜토리얼 강제
        PlayerPrefs.SetInt("TutorialCompleted", 0);
        PlayerPrefs.Save();

        // ✅ 튜토리얼 체크
        bool hasCompletedTutorial = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

        if (!hasCompletedTutorial && tutorialController != null)
        {
            // 튜토리얼 활성화, 메인 게임 비활성화
            TransitionToTutorial();
        }
        else
        {
            // 튜토리얼 비활성화, 메인 게임 시작
            if (tutorialController != null)
            {
                tutorialController.gameObject.SetActive(false);
            }
            TransitionToPlaying();
        }
    }

    void Update()
    {
        // ✅ Tutorial 상태일 때는 메인 게임 로직 실행 안 함
        if (CurrentState == GameStatest2.Tutorial) return;

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

    // ✅ 추가: 튜토리얼 전환
    public void TransitionToTutorial()
    {
        CurrentState = GameStatest2.Tutorial;
        menuMonitorUI.UpdateState(CurrentState);

        // 메인 게임 몰드 비활성화
        foreach (var mold in molds)
        {
            mold.gameObject.SetActive(false);
        }

        // 메인 게임 입력 비활성화
        foreach (var input in catchInputs)
        {
            input.enabled = false;
        }

        // 튜토리얼 활성화
        if (tutorialController != null)
        {
            tutorialController.gameObject.SetActive(true);
        }
    }

    public void TransitionToPlaying()
    {
        CurrentState = GameStatest2.Playing;
        isEndingTriggered = false;
        menuMonitorUI.UpdateState(CurrentState);

        // ✅ 튜토리얼 완전 비활성화
        if (tutorialController != null)
        {
            tutorialController.gameObject.SetActive(false);
        }

        // ✅ 메인 게임 몰드 활성화 및 초기화
        foreach (var mold in molds)
        {
            mold.gameObject.SetActive(true);
            mold.CancelAllScheduledSpawns(); // 혹시 모를 이전 스폰 정리
        }

        // ✅ 메인 게임 입력 활성화 및 초기화
        foreach (var input in catchInputs)
        {
            input.enabled = true;
            // 혹시 이전 판정 기록 초기화
            input.lastJudgeResult = JudgeResult_st2.Miss;
        }

        // ✅ 메인 게임 시스템 초기화
        judgeSystem.ResetStats();
        economySystem.Reset();
        bagManager.Reset();

        // 곡 시작
        songStartDspTime = AudioSettings.dspTime;
        bgmSource.clip = bgmClip;
        bgmSource.Play();

        // 시스템 시작
        crowdManager.Initialize(bgmSource, bgmClip);
        patternDirector.StartPatternSystem(songStartDspTime);

        Debug.Log("✅ Main game started - Tutorial cleanup complete");
    }

    void PauseGame()
    {
        CurrentState = GameStatest2.Paused;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        bgmSource.Pause();

        menuMonitorUI.UpdateState(CurrentState);
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

        // Mold 스폰 취소
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

        // Mold 스폰 취소
        foreach (var mold in molds)
        {
            mold.CancelAllScheduledSpawns();
        }

        // 남은 fish 강제 Miss 처리
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
    }

    // ✅ 중앙 이벤트 핸들러 (튜토리얼과 무관)
    void OnJudgeResult(JudgeResult_st2 result, float delta)
    {
        economySystem.ApplyJudgeResult(result);
    }

    void OnBagSwapped()
    {
        // 메인 게임에서만 사용
    }

    void OnPatternQueued(PatternType_st2 type)
    {
        // 메인 게임에서만 사용
    }
}
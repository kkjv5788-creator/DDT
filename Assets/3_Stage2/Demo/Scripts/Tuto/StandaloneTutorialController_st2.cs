using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameState_st2;

/// <summary>
/// 메인 게임 로직과 완전히 독립된 튜토리얼 컨트롤러
/// 자체 패턴 생성, 자체 판정, 자체 UI를 가짐
/// </summary>
public class StandaloneTutorialController_st2 : MonoBehaviour
{
    public enum TutorialStage
    {
        T0_Telegraph,
        T1_Pop,
        T2_Catch,
        T4_Sync2,
        T5_Run3,
        Complete,
        WaitBeforeMain
    }

    [Header("현재 스테이지")]
    public TutorialStage currentStage = TutorialStage.T0_Telegraph;

    [Header("튜토리얼 전용 몰드 (메인과 별도)")]
    public TutorialMoldController_st2[] tutorialMolds = new TutorialMoldController_st2[3];

    [Header("튜토리얼 전용 손 센서")]
    public TutorialHandSensor_st2 leftSensor;
    public TutorialHandSensor_st2 rightSensor;

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI skipText;
    public TextMeshProUGUI stageCompleteText; // ✅ 추가: 스테이지 완료 메시지
    public GameObject tutorialCanvas;

    [Header("오디오")]
    public AudioSource telegraphSource;
    public AudioSource popSource;
    public AudioSource cueSource;
    public AudioClip telegraphClip;
    public AudioClip popClip;
    public AudioClip sync2Cue;
    public AudioClip run3Cue;

    [Header("튜토리얼 BGM")]
    public AudioSource tutorialBGMSource; // ✅ 추가
    public AudioClip tutorialBGMClip;     // ✅ 추가
    public float bgmVolume = 0.5f;        // ✅ 추가

    [Header("타이밍 설정")]
    public float telegraphToPopDelay = 0.4f;
    public float manualPatternInterval = 2.0f;
    public float runStepInterval = 0.25f;
    public float waitBeforeMainDuration = 3.0f;

    [Header("스테이지 전환 설정")]
    public float stageTransitionDelay = 2.0f; // ✅ 추가: 스테이지 완료 후 다음으로 넘어가기 전 대기
    public bool showStageCompleteMessage = true; // ✅ 추가: 완료 메시지 표시 여부

    [Header("판정 설정")]
    public float perfectWindow = 0.04f;
    public float goodWindow = 0.075f;
    [Header("스냅 연출(튜토리얼)")]
    public float snapHoldSeconds = 0.2f;

    // 진행 카운터
    private int telegraphCount = 0;
    private int popCount = 0;
    private int goodOrBetterCount = 0;
    private int sync2SuccessCount = 0;
    private int run3SuccessCount = 0;

    // 현재 활성 패턴 타입
    private PatternType_st2 currentPatternType = PatternType_st2.Normal;

    // ✅ 추가: Sync2, Run3 성공 플래그
    private bool hasSuccessSync2 = false;
    private bool hasSuccessRun3 = false;

    // 자동 패턴 생성 제어
    private Coroutine autoPatternRoutine;

    void Start()
    {
        InitializeUI();
        InitializeSensors();
        StartTutorialBGM(); // ✅ 추가
        StartStage(currentStage);
    }

    void Update()
    {
        // A 버튼으로 스킵
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            SkipTutorial();
        }

        // 손 입력 처리 (메인과 독립)
        ProcessHandInput(leftSensor, OVRInput.Controller.LTouch);
        ProcessHandInput(rightSensor, OVRInput.Controller.RTouch);
    }

    void InitializeUI()
    {
        if (skipText != null)
            skipText.text = "[Press A to Skip Tutorial]";

        if (tutorialCanvas != null)
            tutorialCanvas.SetActive(true);

        // ✅ 완료 메시지 초기화
        if (stageCompleteText != null)
            stageCompleteText.gameObject.SetActive(false);
    }

    void InitializeSensors()
    {
        if (leftSensor != null)
            leftSensor.Initialize(OVRInput.Controller.LTouch);

        if (rightSensor != null)
            rightSensor.Initialize(OVRInput.Controller.RTouch);
    }

    // ✅ 추가: 튜토리얼 BGM 시작
    void StartTutorialBGM()
    {
        if (tutorialBGMSource != null && tutorialBGMClip != null)
        {
            tutorialBGMSource.clip = tutorialBGMClip;
            tutorialBGMSource.loop = true;
            tutorialBGMSource.volume = bgmVolume;
            tutorialBGMSource.Play();
            Debug.Log("✅ Tutorial BGM started");
        }
    }

    // ✅ 추가: 튜토리얼 BGM 중지
    void StopTutorialBGM()
    {
        if (tutorialBGMSource != null)
        {
            tutorialBGMSource.Stop();
            Debug.Log("✅ Tutorial BGM stopped");
        }
    }

    void StartStage(TutorialStage stage)
    {
        currentStage = stage;

        // 이전 자동 패턴 중지
        if (autoPatternRoutine != null)
        {
            StopCoroutine(autoPatternRoutine);
            autoPatternRoutine = null;
        }

        // ✅ 완료 메시지 숨기기
        HideStageComplete();

        // 스테이지별 시작 로직
        switch (stage)
        {
            case TutorialStage.T0_Telegraph:
                ShowInstruction("Listen to the 'Telegraph' sound!", $"Progress: {telegraphCount}/2");
                autoPatternRoutine = StartCoroutine(AutoSpawnNormalPattern());
                break;

            case TutorialStage.T1_Pop:
                ShowInstruction("Watch the fish 'Pop' out!", $"Progress: {popCount}/2");
                autoPatternRoutine = StartCoroutine(AutoSpawnNormalPattern());
                break;

            case TutorialStage.T2_Catch:
                ShowInstruction("Catch fish with Good or Perfect!", $"Good+ Catches: {goodOrBetterCount}/2");
                autoPatternRoutine = StartCoroutine(AutoSpawnNormalPattern());
                break;

            case TutorialStage.T4_Sync2:
                ShowInstruction("Try Sync2 pattern! (땡땡)", "Catch at least 1 fish!");
                autoPatternRoutine = StartCoroutine(AutoSpawnSync2Pattern());
                break;

            case TutorialStage.T5_Run3:
                ShowInstruction("Try Run3 pattern! (딸랑)", "Catch at least 1 fish!");
                autoPatternRoutine = StartCoroutine(AutoSpawnRun3Pattern());
                break;

            case TutorialStage.Complete:
                ShowInstruction("Tutorial Complete!", "Starting main game...");
                StartCoroutine(WaitThenStartMain());
                break;
        }
    }

    void ShowInstruction(string instruction, string progress)
    {
        if (instructionText != null)
            instructionText.text = instruction;

        if (progressText != null)
            progressText.text = progress;
    }

    // ========================================
    // 자동 패턴 생성 (메인 PatternDirector와 독립)
    // ========================================

    IEnumerator AutoSpawnNormalPattern()
    {
        while (true)
        {
            currentPatternType = PatternType_st2.Normal;

            // 랜덤 몰드 선택
            int moldIndex = Random.Range(0, 3);
            yield return StartCoroutine(SpawnSingleFish(moldIndex));

            // 다음 패턴까지 대기
            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    IEnumerator AutoSpawnSync2Pattern()
    {
        while (true)
        {
            currentPatternType = PatternType_st2.Sync2;

            // 2개 몰드 선택
            int[][] pairs = { new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 0, 2 } };
            int[] selectedPair = pairs[Random.Range(0, 3)];

            // 큐 사운드
            if (cueSource != null && sync2Cue != null)
                cueSource.PlayOneShot(sync2Cue);

            yield return new WaitForSeconds(0.35f);

            // 동시 스폰
            foreach (int moldIndex in selectedPair)
            {
                StartCoroutine(SpawnSingleFish(moldIndex));
            }

            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    IEnumerator AutoSpawnRun3Pattern()
    {
        while (true)
        {
            currentPatternType = PatternType_st2.Run3;

            // 큐 사운드
            if (cueSource != null && run3Cue != null)
                cueSource.PlayOneShot(run3Cue);

            yield return new WaitForSeconds(0.35f);

            // 순차 스폰
            for (int i = 0; i < 3; i++)
            {
                StartCoroutine(SpawnSingleFish(i));
                yield return new WaitForSeconds(runStepInterval);
            }

            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    IEnumerator SpawnSingleFish(int moldIndex)
    {
        if (tutorialMolds[moldIndex] == null) yield break;

        // Telegraph 사운드
        if (telegraphSource != null && telegraphClip != null)
        {
            telegraphSource.PlayOneShot(telegraphClip);
            OnTelegraphPlayed();
        }

        // Telegraph → Pop 대기
        yield return new WaitForSeconds(telegraphToPopDelay);

        // Pop 사운드 & 물고기 스폰
        if (popSource != null && popClip != null)
        {
            popSource.PlayOneShot(popClip);
        }

        double popTime = AudioSettings.dspTime;
        tutorialMolds[moldIndex].SpawnFish(popTime);
        OnPopPlayed();
    }

    // ========================================
    // 손 입력 처리 (메인 CatchInput과 독립)
    // ========================================

    void ProcessHandInput(TutorialHandSensor_st2 sensor, OVRInput.Controller controller)
    {
        if (sensor == null) return;
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller)) return;

        var target = sensor.GetCurrentTarget();
        if (target == null) return;
        if (target.isResolved) return;

        // 판정 수행
        double catchTime = AudioSettings.dspTime;
        float delta = Mathf.Abs((float)(catchTime - target.popTime));

        JudgeResult_st2 result;
        if (delta <= perfectWindow)
            result = JudgeResult_st2.Perfect;
        else if (delta <= goodWindow)
            result = JudgeResult_st2.Good;
        else
            result = JudgeResult_st2.Miss;

        // ✅ 먼저 resolved 플래그 설정 (중복 처리 방지)
        target.isResolved = true;

        // 성공 시 처리
        if (result != JudgeResult_st2.Miss)
        {
            target.OnCaught();

            // 손으로 스냅
            target.transform.position = sensor.transform.position;
            target.transform.SetParent(sensor.transform, true);

            // ✅ 1프레임 후 반환 → ✅ 0.2초 홀드 후 반환
            StartCoroutine(SnapHoldThenRelease(target));

            OnCatchSuccess(result);
        }
        else
        {
            OnCatchMiss();
        }
    }

    IEnumerator SnapHoldThenRelease(TutorialFishToken_st2 fish)
    {
        yield return new WaitForSeconds(snapHoldSeconds);

        // 이미 정리됐으면 스킵
        if (fish == null || fish.gameObject == null || !fish.gameObject.activeInHierarchy)
            yield break;

        // 손에 부모로 남는 거 방지용: 떼고 반환
        fish.transform.SetParent(null, true);

        if (fish.ownerMold != null)
            fish.ownerMold.ReleaseFish(fish);
    }

    // ========================================
    // 진행 이벤트 처리
    // ========================================

    void OnTelegraphPlayed()
    {
        if (currentStage == TutorialStage.T0_Telegraph)
        {
            telegraphCount++;
            UpdateProgress($"Progress: {telegraphCount}/2");

            if (telegraphCount >= 2)
            {
                // ✅ 스테이지 완료 메시지 표시 후 전환
                ShowStageComplete("Telegraph Stage Complete!");
                StartCoroutine(AdvanceStageDelayed());
            }
        }
    }

    void OnPopPlayed()
    {
        if (currentStage == TutorialStage.T1_Pop)
        {
            popCount++;
            UpdateProgress($"Progress: {popCount}/2");

            if (popCount >= 2)
            {
                ShowStageComplete("Pop Stage Complete!");
                StartCoroutine(AdvanceStageDelayed());
            }
        }
    }

    void OnCatchSuccess(JudgeResult_st2 result)
    {
        if (result == JudgeResult_st2.Perfect || result == JudgeResult_st2.Good)
        {
            if (currentStage == TutorialStage.T2_Catch)
            {
                goodOrBetterCount++;
                UpdateProgress($"Good+ Catches: {goodOrBetterCount}/2");

                if (goodOrBetterCount >= 2)
                    AdvanceStage();
            }
            else if (currentStage == TutorialStage.T4_Sync2 && currentPatternType == PatternType_st2.Sync2)
            {
                hasSuccessSync2 = true; // ✅ 플래그 설정
                sync2SuccessCount++;
                UpdateProgress($"Sync2 Success: {sync2SuccessCount}/1");

                if (sync2SuccessCount >= 1)
                    AdvanceStage();
            }
            else if (currentStage == TutorialStage.T5_Run3 && currentPatternType == PatternType_st2.Run3)
            {
                hasSuccessRun3 = true; // ✅ 플래그 설정
                run3SuccessCount++;
                UpdateProgress($"Run3 Success: {run3SuccessCount}/1");

                if (run3SuccessCount >= 1)
                    AdvanceStage();
            }
        }
    }

    void OnCatchMiss()
    {
        // Miss는 진행에 영향 없음
    }

    void UpdateProgress(string text)
    {
        if (progressText != null)
            progressText.text = text;
    }

    // ✅ 추가: 스테이지 완료 메시지 표시
    void ShowStageComplete(string message)
    {
        if (!showStageCompleteMessage || stageCompleteText == null)
            return;

        stageCompleteText.text = message;
        stageCompleteText.gameObject.SetActive(true);
    }

    // ✅ 추가: 완료 메시지 숨기기
    void HideStageComplete()
    {
        if (stageCompleteText != null)
            stageCompleteText.gameObject.SetActive(false);
    }

    // ✅ 추가: 지연 후 스테이지 전환
    IEnumerator AdvanceStageDelayed()
    {
        // 자동 패턴 일시 중지
        if (autoPatternRoutine != null)
        {
            StopCoroutine(autoPatternRoutine);
            autoPatternRoutine = null;
        }

        // 완료 메시지 표시 시간
        yield return new WaitForSeconds(stageTransitionDelay);

        // 완료 메시지 숨기기
        HideStageComplete();

        // 다음 스테이지로 전환
        AdvanceStage();
    }

    void AdvanceStage()
    {
        StartStage(currentStage + 1);
    }

    // ========================================
    // 디버그/HUD용 메서드
    // ========================================

    /// <summary>
    /// DebugHUD에서 현재 진행 상황을 표시하기 위한 메서드
    /// </summary>
    public string GetCurrentStageInfo()
    {
        switch (currentStage)
        {
            case TutorialStage.T0_Telegraph:
                return $"Telegraph {telegraphCount}/2";
            case TutorialStage.T1_Pop:
                return $"Pop {popCount}/2";
            case TutorialStage.T2_Catch:
                return $"Catch {goodOrBetterCount}/2";
            case TutorialStage.T4_Sync2:
                return hasSuccessSync2 ? "Sync2 Done" : "Sync2 Try";
            case TutorialStage.T5_Run3:
                return hasSuccessRun3 ? "Run3 Done" : "Run3 Try";
            case TutorialStage.Complete:
                return "Complete";
            case TutorialStage.WaitBeforeMain:
                return "Wait...";
            default:
                return "Unknown";
        }
    }

    // ========================================
    // 튜토리얼 종료 및 정리
    // ========================================

    IEnumerator WaitThenStartMain()
    {
        // 자동 패턴 중지
        if (autoPatternRoutine != null)
        {
            StopCoroutine(autoPatternRoutine);
            autoPatternRoutine = null;
        }

        // ✅ BGM 중지
        StopTutorialBGM();

        // ✅ 모든 튜토리얼 물고기 정리
        CleanupAllTutorialFish();

        // 3초 대기
        yield return new WaitForSeconds(waitBeforeMainDuration);

        // 튜토리얼 완료 플래그
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // 튜토리얼 UI 비활성화
        if (tutorialCanvas != null)
            tutorialCanvas.SetActive(false);

        // ✅ 튜토리얼 전체 비활성화 (GameFlowController가 처리)
        gameObject.SetActive(false);

        // 메인 게임 시작
        GameFlowController_st2.Instance?.TransitionToPlaying();
    }

    void SkipTutorial()
    {
        // 모든 코루틴 중지
        StopAllCoroutines();

        // ✅ BGM 중지
        StopTutorialBGM();

        // ✅ 모든 튜토리얼 물고기 정리
        CleanupAllTutorialFish();

        // 플래그 저장
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // UI 비활성화
        if (tutorialCanvas != null)
            tutorialCanvas.SetActive(false);

        // ✅ 튜토리얼 전체 비활성화
        gameObject.SetActive(false);

        // 메인 게임 시작
        GameFlowController_st2.Instance?.TransitionToPlaying();
    }

    // ✅ 추가: 모든 튜토리얼 물고기 정리
    void CleanupAllTutorialFish()
    {
        // 각 몰드의 활성 물고기 모두 제거
        foreach (var mold in tutorialMolds)
        {
            if (mold != null)
            {
                mold.CleanupAllFish();
            }
        }

        // 손에 붙은 물고기도 정리 (부모 체크)
        if (leftSensor != null)
        {
            CleanupHandAttachedFish(leftSensor.transform);
        }

        if (rightSensor != null)
        {
            CleanupHandAttachedFish(rightSensor.transform);
        }
    }

    // ✅ 추가: 손에 붙은 물고기 강제 제거
    void CleanupHandAttachedFish(Transform handTransform)
    {
        // 손의 자식으로 있는 모든 TutorialFishToken 찾아서 제거
        var attachedFish = handTransform.GetComponentsInChildren<TutorialFishToken_st2>();
        foreach (var fish in attachedFish)
        {
            // ✅ 안전 체크
            if (fish != null && fish.gameObject != null && fish.gameObject.activeInHierarchy)
            {
                if (fish.ownerMold != null)
                {
                    fish.ownerMold.ReleaseFish(fish);
                }
            }
        }
    }
}
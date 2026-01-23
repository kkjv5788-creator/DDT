using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameState_st2;

public class StandaloneTutorialController_st2 : MonoBehaviour
{
    public enum TutorialStage
    {
        T0_Telegraph,   // 텔레그래프 + 달그락만
        T1_Pop,         // 구경(잡기 불가)
        T2_Catch,       // 잡기 가능
        T4_Sync2,       // 2개 동시: 2개 모두 잡아야 성공
        T5_Run3,        // 3개 연속: 3개 모두 잡아야 성공
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
    public TextMeshProUGUI stageCompleteText;
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
    public AudioSource tutorialBGMSource;
    public AudioClip tutorialBGMClip;
    public float bgmVolume = 0.5f;

    [Header("타이밍 설정")]
    public float telegraphToPopDelay = 0.4f;
    public float manualPatternInterval = 2.0f;
    public float runStepInterval = 0.25f;
    public float waitBeforeMainDuration = 3.0f;

    [Header("스테이지 전환 설정")]
    public float stageTransitionDelay = 2.0f;
    public bool showStageCompleteMessage = true;

    [Header("판정 설정")]
    public float perfectWindow = 0.04f;
    public float goodWindow = 0.075f;

    [Header("스냅 연출(튜토리얼)")]
    public float snapHoldSeconds = 0.2f;

    [Header("Pop 애니 리드타임 (퐁 소리보다 먼저 열기)")]
    public float popAnimLeadSeconds = 0.25f;

    [Header("특수 패턴 재시도")]
    public float specialAttemptTimeout = 3.0f;   // 이 시간 안에 다 못 잡으면 리셋 후 재시도
    public float specialRetryDelay = 0.5f;

    // 진행 카운터
    private int telegraphCount = 0;
    private int popCount = 0;
    private int goodOrBetterCount = 0;

    // 특수 패턴 목표/진행
    private int specialTargetCount = 0;   // 2 또는 3
    private int specialCaughtCount = 0;   // 현재 시도에서 잡은 개수
    private bool specialStageCompleted = false;

    private PatternType_st2 currentPatternType = PatternType_st2.Normal;
    private bool hasSuccessSync2 = false;
    private bool hasSuccessRun3 = false;

    private Coroutine autoPatternRoutine;

    void Start()
    {
        InitializeUI();
        InitializeSensors();
        StartTutorialBGM();
        StartStage(currentStage);
    }

    void Update()
    {
        // A 버튼 스킵
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            SkipTutorial();

        // ✅ 스테이지별로 잡기 허용
        if (IsCatchingEnabled())
        {
            ProcessHandInput(leftSensor, OVRInput.Controller.LTouch);
            ProcessHandInput(rightSensor, OVRInput.Controller.RTouch);
        }
    }

    bool IsCatchingEnabled()
    {
        return currentStage == TutorialStage.T2_Catch
            || currentStage == TutorialStage.T4_Sync2
            || currentStage == TutorialStage.T5_Run3;
    }

    void InitializeUI()
    {
        if (skipText != null)
            skipText.text = "[Press A to Skip Tutorial]";

        if (tutorialCanvas != null)
            tutorialCanvas.SetActive(true);

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

    void StartTutorialBGM()
    {
        if (tutorialBGMSource != null && tutorialBGMClip != null)
        {
            tutorialBGMSource.clip = tutorialBGMClip;
            tutorialBGMSource.loop = true;
            tutorialBGMSource.volume = bgmVolume;
            tutorialBGMSource.Play();
        }
    }

    void StopTutorialBGM()
    {
        if (tutorialBGMSource != null)
            tutorialBGMSource.Stop();
    }

    void StartStage(TutorialStage stage)
    {
        currentStage = stage;

        // 이전 루틴 중지
        if (autoPatternRoutine != null)
        {
            StopCoroutine(autoPatternRoutine);
            autoPatternRoutine = null;
        }

        HideStageComplete();

        // 특수 패턴 상태 초기화
        specialStageCompleted = false;
        specialTargetCount = 0;
        specialCaughtCount = 0;

        switch (stage)
        {
            case TutorialStage.T0_Telegraph:
                currentPatternType = PatternType_st2.Normal;
                ShowInstruction("Listen to the 'Telegraph' sound!", $"Progress: {telegraphCount}/2");
                autoPatternRoutine = StartCoroutine(AutoTelegraphOnly());
                break;

            case TutorialStage.T1_Pop:
                currentPatternType = PatternType_st2.Normal;
                ShowInstruction("Watch the fish 'Pop' out! (Can't catch yet)", $"Progress: {popCount}/2");
                autoPatternRoutine = StartCoroutine(AutoPopWatchOnly());
                break;

            case TutorialStage.T2_Catch:
                currentPatternType = PatternType_st2.Normal;
                ShowInstruction("Now catch fish with Good or Perfect!", $"Good+ Catches: {goodOrBetterCount}/2");
                autoPatternRoutine = StartCoroutine(AutoCatchNormal());
                break;

            case TutorialStage.T4_Sync2:
                currentPatternType = PatternType_st2.Sync2;
                ShowInstruction("Sync2 Pattern: Catch BOTH fish!", "Caught: 0/2");
                autoPatternRoutine = StartCoroutine(RunSync2UntilSuccess());
                break;

            case TutorialStage.T5_Run3:
                currentPatternType = PatternType_st2.Run3;
                ShowInstruction("Run3 Pattern: Catch ALL THREE fish!", "Caught: 0/3");
                autoPatternRoutine = StartCoroutine(RunRun3UntilSuccess());
                break;

            case TutorialStage.Complete:
                ShowInstruction("Tutorial Complete!", "Starting main game...");
                StartCoroutine(WaitThenStartMain());
                break;
        }
    }

    void ShowInstruction(string instruction, string progress)
    {
        if (instructionText != null) instructionText.text = instruction;
        if (progressText != null) progressText.text = progress;
    }

    void UpdateProgress(string text)
    {
        if (progressText != null) progressText.text = text;
    }

    // =========================================================
    // Stage 0: Telegraph Only (Shake only, no pop, no fish)
    // =========================================================
    IEnumerator AutoTelegraphOnly()
    {
        while (true)
        {
            int moldIndex = Random.Range(0, 3);
            yield return StartCoroutine(PlayTelegraphOnly(moldIndex));
            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    IEnumerator PlayTelegraphOnly(int moldIndex)
    {
        if (tutorialMolds[moldIndex] == null) yield break;

        // 텔레그래프 사운드
        if (telegraphSource != null && telegraphClip != null)
        {
            telegraphSource.PlayOneShot(telegraphClip);
        }

        // 달그락(흔들림) 비주얼
        tutorialMolds[moldIndex].PlayTelegraphVisual();

        OnTelegraphPlayed();
        yield return null;
    }

    // =========================================================
    // Stage 1: Pop Watch Only (spawn but catching disabled by Update gating)
    // =========================================================
    IEnumerator AutoPopWatchOnly()
    {
        while (true)
        {
            int moldIndex = Random.Range(0, 3);
            yield return StartCoroutine(SpawnTelegraphToPopFish(moldIndex));
            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    // =========================================================
    // Stage 2: Normal Catch
    // =========================================================
    IEnumerator AutoCatchNormal()
    {
        while (true)
        {
            int moldIndex = Random.Range(0, 3);
            yield return StartCoroutine(SpawnTelegraphToPopFish(moldIndex));
            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    // =========================================================
    // Special: Sync2 (catch both)
    // =========================================================
    IEnumerator RunSync2UntilSuccess()
    {
        specialTargetCount = 2;

        while (!specialStageCompleted && currentStage == TutorialStage.T4_Sync2)
        {
            // 새 시도 시작
            specialCaughtCount = 0;
            UpdateProgress($"Caught: {specialCaughtCount}/{specialTargetCount}");

            CleanupAllTutorialFish(); // 이전 시도 잔여 제거

            // 큐
            if (cueSource != null && sync2Cue != null)
                cueSource.PlayOneShot(sync2Cue);

            yield return new WaitForSeconds(0.35f);

            // 2개 몰드 선택
            int[][] pairs = { new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 0, 2 } };
            int[] selectedPair = pairs[Random.Range(0, 3)];

            // 2개 동시 스폰
            foreach (int moldIndex in selectedPair)
                StartCoroutine(SpawnTelegraphToPopFish(moldIndex));

            // 제한 시간 안에 둘 다 잡으면 성공
            float t = 0f;
            while (t < specialAttemptTimeout && !specialStageCompleted && currentStage == TutorialStage.T4_Sync2)
            {
                if (specialCaughtCount >= specialTargetCount)
                    break;

                t += Time.deltaTime;
                yield return null;
            }

            if (specialCaughtCount >= specialTargetCount)
            {
                hasSuccessSync2 = true;
                CompleteStageWithMessage("Sync2 Complete!");
                yield break;
            }

            // 실패 → 재시도
            yield return new WaitForSeconds(specialRetryDelay);
        }
    }

    // =========================================================
    // Special: Run3 (catch all three)
    // =========================================================
    IEnumerator RunRun3UntilSuccess()
    {
        specialTargetCount = 3;

        while (!specialStageCompleted && currentStage == TutorialStage.T5_Run3)
        {
            specialCaughtCount = 0;
            UpdateProgress($"Caught: {specialCaughtCount}/{specialTargetCount}");

            CleanupAllTutorialFish();

            // 큐
            if (cueSource != null && run3Cue != null)
                cueSource.PlayOneShot(run3Cue);

            yield return new WaitForSeconds(0.35f);

            // 3개 순차 스폰
            for (int i = 0; i < 3; i++)
            {
                StartCoroutine(SpawnTelegraphToPopFish(i));
                yield return new WaitForSeconds(runStepInterval);
            }

            float t = 0f;
            while (t < specialAttemptTimeout && !specialStageCompleted && currentStage == TutorialStage.T5_Run3)
            {
                if (specialCaughtCount >= specialTargetCount)
                    break;

                t += Time.deltaTime;
                yield return null;
            }

            if (specialCaughtCount >= specialTargetCount)
            {
                hasSuccessRun3 = true;
                CompleteStageWithMessage("Run3 Complete!");
                yield break;
            }

            yield return new WaitForSeconds(specialRetryDelay);
        }
    }

    // =========================================================
    // Common Spawn: Telegraph -> (Pop anim lead) -> Pop sound -> Spawn fish
    // =========================================================
    IEnumerator SpawnTelegraphToPopFish(int moldIndex)
    {
        if (tutorialMolds[moldIndex] == null) yield break;

        // Telegraph 사운드 + 달그락
        if (telegraphSource != null && telegraphClip != null)
            telegraphSource.PlayOneShot(telegraphClip);

        tutorialMolds[moldIndex].PlayTelegraphVisual();

        // (T0에서만 카운트 올라가도록 OnTelegraphPlayed 내부에서 스테이지 체크)
        OnTelegraphPlayed();

        // Pop 애니가 퐁 소리보다 먼저 열리도록
        float lead = Mathf.Max(0f, popAnimLeadSeconds);
        float pre = Mathf.Max(0f, telegraphToPopDelay - lead);
        yield return new WaitForSeconds(pre);

        tutorialMolds[moldIndex].TriggerPopAnimation();

        if (lead > 0f) yield return new WaitForSeconds(lead);

        // Pop 사운드
        if (popSource != null && popClip != null)
            popSource.PlayOneShot(popClip);

        // 스폰
        double popTime = AudioSettings.dspTime;
        tutorialMolds[moldIndex].SpawnFish(popTime);

        OnPopPlayed();
    }

    // =========================================================
    // Hand Input (catching)
    // =========================================================
    void ProcessHandInput(TutorialHandSensor_st2 sensor, OVRInput.Controller controller)
    {
        if (sensor == null) return;
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller)) return;

        var target = sensor.GetCurrentTarget();
        if (target == null) return;

        // ✅ 이미 잡힌 애만 중복 방지
        if (target.isResolved) return;

        double catchTime = AudioSettings.dspTime;
        float delta = Mathf.Abs((float)(catchTime - target.popTime));

        JudgeResult_st2 result;
        if (delta <= perfectWindow) result = JudgeResult_st2.Perfect;
        else if (delta <= goodWindow) result = JudgeResult_st2.Good;
        else result = JudgeResult_st2.Miss;

        if (result != JudgeResult_st2.Miss)
        {
            // ✅ 성공일 때만 resolved 처리
            target.isResolved = true;

            target.OnCaught();

            // 손 스냅
            target.transform.position = sensor.transform.position;
            target.transform.SetParent(sensor.transform, true);

            StartCoroutine(SnapHoldThenRelease(target));

            OnCatchSuccess(result);
        }
        else
        {
            // Miss는 resolved 처리 안 함 (계속 떨어지거나 다시 시도 가능)
            OnCatchMiss();
        }
    }

    IEnumerator SnapHoldThenRelease(TutorialFishToken_st2 fish)
    {
        yield return new WaitForSeconds(snapHoldSeconds);

        if (fish == null || fish.gameObject == null || !fish.gameObject.activeInHierarchy)
            yield break;

        fish.transform.SetParent(null, true);

        if (fish.ownerMold != null)
            fish.ownerMold.ReleaseFish(fish);
    }

    // =========================================================
    // Progress Events
    // =========================================================
    void OnTelegraphPlayed()
    {
        if (currentStage == TutorialStage.T0_Telegraph)
        {
            telegraphCount++;
            UpdateProgress($"Progress: {telegraphCount}/2");

            if (telegraphCount >= 2)
            {
                CompleteStageWithMessage("Telegraph Stage Complete!");
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
                CompleteStageWithMessage("Pop Watch Stage Complete!");
            }
        }
    }

    void OnCatchSuccess(JudgeResult_st2 result)
    {
        if (result != JudgeResult_st2.Perfect && result != JudgeResult_st2.Good)
            return;

        if (currentStage == TutorialStage.T2_Catch)
        {
            goodOrBetterCount++;
            UpdateProgress($"Good+ Catches: {goodOrBetterCount}/2");

            if (goodOrBetterCount >= 2)
                CompleteStageWithMessage("Catch Stage Complete!");
        }
        else if (currentStage == TutorialStage.T4_Sync2 && currentPatternType == PatternType_st2.Sync2)
        {
            // ✅ 2개 모두 잡아야 성공
            specialCaughtCount = Mathf.Min(specialTargetCount, specialCaughtCount + 1);
            UpdateProgress($"Caught: {specialCaughtCount}/{specialTargetCount}");
        }
        else if (currentStage == TutorialStage.T5_Run3 && currentPatternType == PatternType_st2.Run3)
        {
            // ✅ 3개 모두 잡아야 성공
            specialCaughtCount = Mathf.Min(specialTargetCount, specialCaughtCount + 1);
            UpdateProgress($"Caught: {specialCaughtCount}/{specialTargetCount}");
        }
    }

    void OnCatchMiss()
    {
        // 필요하면 여기서 Miss 카운트/피드백 추가 가능
    }

    // =========================================================
    // Stage Completion Helper
    // =========================================================
    void CompleteStageWithMessage(string message)
    {
        if (specialStageCompleted) return; // 중복 방지
        specialStageCompleted = true;

        ShowStageComplete(message);

        // 자동 루틴 중지
        if (autoPatternRoutine != null)
        {
            StopCoroutine(autoPatternRoutine);
            autoPatternRoutine = null;
        }

        StartCoroutine(AdvanceStageDelayed());
    }

    void ShowStageComplete(string message)
    {
        if (!showStageCompleteMessage || stageCompleteText == null)
            return;

        stageCompleteText.text = message;
        stageCompleteText.gameObject.SetActive(true);
    }

    void HideStageComplete()
    {
        if (stageCompleteText != null)
            stageCompleteText.gameObject.SetActive(false);
    }

    IEnumerator AdvanceStageDelayed()
    {
        yield return new WaitForSeconds(stageTransitionDelay);
        HideStageComplete();
        AdvanceStage();
    }

    void AdvanceStage()
    {
        StartStage(currentStage + 1);
    }

    // =========================================================
    // Debug HUD
    // =========================================================
    public string GetCurrentStageInfo()
    {
        switch (currentStage)
        {
            case TutorialStage.T0_Telegraph: return $"Telegraph {telegraphCount}/2";
            case TutorialStage.T1_Pop: return $"Pop {popCount}/2";
            case TutorialStage.T2_Catch: return $"Catch {goodOrBetterCount}/2";
            case TutorialStage.T4_Sync2: return hasSuccessSync2 ? "Sync2 Done" : $"Sync2 {specialCaughtCount}/2";
            case TutorialStage.T5_Run3: return hasSuccessRun3 ? "Run3 Done" : $"Run3 {specialCaughtCount}/3";
            case TutorialStage.Complete: return "Complete";
            case TutorialStage.WaitBeforeMain: return "Wait...";
            default: return "Unknown";
        }
    }

    // =========================================================
    // Tutorial End / Cleanup
    // =========================================================
    IEnumerator WaitThenStartMain()
    {
        if (autoPatternRoutine != null)
        {
            StopCoroutine(autoPatternRoutine);
            autoPatternRoutine = null;
        }

        StopTutorialBGM();
        CleanupAllTutorialFish();

        yield return new WaitForSeconds(waitBeforeMainDuration);

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        if (tutorialCanvas != null)
            tutorialCanvas.SetActive(false);

        gameObject.SetActive(false);

        GameFlowController_st2.Instance?.TransitionToPlaying();
    }

    void SkipTutorial()
    {
        StopAllCoroutines();

        StopTutorialBGM();
        CleanupAllTutorialFish();

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        if (tutorialCanvas != null)
            tutorialCanvas.SetActive(false);

        gameObject.SetActive(false);

        GameFlowController_st2.Instance?.TransitionToPlaying();
    }

    void CleanupAllTutorialFish()
    {
        foreach (var mold in tutorialMolds)
        {
            if (mold != null)
                mold.CleanupAllFish();
        }

        if (leftSensor != null) CleanupHandAttachedFish(leftSensor.transform);
        if (rightSensor != null) CleanupHandAttachedFish(rightSensor.transform);
    }

    void CleanupHandAttachedFish(Transform handTransform)
    {
        var attachedFish = handTransform.GetComponentsInChildren<TutorialFishToken_st2>();
        foreach (var fish in attachedFish)
        {
            if (fish != null && fish.gameObject != null && fish.gameObject.activeInHierarchy)
            {
                if (fish.ownerMold != null)
                    fish.ownerMold.ReleaseFish(fish);
            }
        }
    }
}

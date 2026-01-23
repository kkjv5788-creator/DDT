// ===============================
// StandaloneTutorialController_st2.cs
// (Pop 애니가 퐁 소리보다 0.25초 먼저 열리도록 수정 + Telegraph 사운드는 여기서 유지)
// ===============================
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameState_st2;

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

    // ✅ Pop 애니가 퐁 소리보다 먼저 열리는 시간(기본 0.25)
    [Header("Pop 애니 리드타임")]
    public float popAnimLeadSeconds = 0.25f;

    private int telegraphCount = 0;
    private int popCount = 0;
    private int goodOrBetterCount = 0;
    private int sync2SuccessCount = 0;
    private int run3SuccessCount = 0;

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
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            SkipTutorial();
        }

        ProcessHandInput(leftSensor, OVRInput.Controller.LTouch);
        ProcessHandInput(rightSensor, OVRInput.Controller.RTouch);
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

        if (autoPatternRoutine != null)
        {
            StopCoroutine(autoPatternRoutine);
            autoPatternRoutine = null;
        }

        HideStageComplete();

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

    IEnumerator AutoSpawnNormalPattern()
    {
        while (true)
        {
            currentPatternType = PatternType_st2.Normal;

            int moldIndex = Random.Range(0, 3);
            yield return StartCoroutine(SpawnSingleFish(moldIndex));

            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    IEnumerator AutoSpawnSync2Pattern()
    {
        while (true)
        {
            currentPatternType = PatternType_st2.Sync2;

            int[][] pairs = { new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 0, 2 } };
            int[] selectedPair = pairs[Random.Range(0, 3)];

            if (cueSource != null && sync2Cue != null)
                cueSource.PlayOneShot(sync2Cue);

            yield return new WaitForSeconds(0.35f);

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

            if (cueSource != null && run3Cue != null)
                cueSource.PlayOneShot(run3Cue);

            yield return new WaitForSeconds(0.35f);

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

        // Telegraph 사운드 (컨트롤러가 담당)
        if (telegraphSource != null && telegraphClip != null)
        {
            telegraphSource.PlayOneShot(telegraphClip);

            // ✅ 달그락(흔들림) 비주얼은 몰드가 담당
            tutorialMolds[moldIndex].PlayTelegraphVisual();

            OnTelegraphPlayed();
        }

        // ✅ Pop 애니메이션이 퐁 소리보다 popAnimLeadSeconds 만큼 먼저 열리도록
        float lead = Mathf.Max(0f, popAnimLeadSeconds);

        // telegraphToPopDelay 안에서 lead만큼 앞당겨 애니 트리거
        float pre = Mathf.Max(0f, telegraphToPopDelay - lead);
        yield return new WaitForSeconds(pre);

        // 애니 먼저 열기
        tutorialMolds[moldIndex].TriggerPopAnimation();

        // 남은 lead 시간 대기 후 소리 + 스폰
        if (lead > 0f)
            yield return new WaitForSeconds(lead);

        if (popSource != null && popClip != null)
            popSource.PlayOneShot(popClip);

        double popTime = AudioSettings.dspTime;
        tutorialMolds[moldIndex].SpawnFish(popTime);
        OnPopPlayed();
    }

    void ProcessHandInput(TutorialHandSensor_st2 sensor, OVRInput.Controller controller)
    {
        if (sensor == null) return;
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller)) return;

        var target = sensor.GetCurrentTarget();
        if (target == null) return;
        if (target.isResolved) return;

        double catchTime = AudioSettings.dspTime;
        float delta = Mathf.Abs((float)(catchTime - target.popTime));

        JudgeResult_st2 result;
        if (delta <= perfectWindow) result = JudgeResult_st2.Perfect;
        else if (delta <= goodWindow) result = JudgeResult_st2.Good;
        else result = JudgeResult_st2.Miss;

        target.isResolved = true;

        if (result != JudgeResult_st2.Miss)
        {
            target.OnCaught();

            target.transform.position = sensor.transform.position;
            target.transform.SetParent(sensor.transform, true);

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

        if (fish == null || fish.gameObject == null || !fish.gameObject.activeInHierarchy)
            yield break;

        fish.transform.SetParent(null, true);

        if (fish.ownerMold != null)
            fish.ownerMold.ReleaseFish(fish);
    }

    void OnTelegraphPlayed()
    {
        if (currentStage == TutorialStage.T0_Telegraph)
        {
            telegraphCount++;
            UpdateProgress($"Progress: {telegraphCount}/2");

            if (telegraphCount >= 2)
            {
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
                hasSuccessSync2 = true;
                sync2SuccessCount++;
                UpdateProgress($"Sync2 Success: {sync2SuccessCount}/1");

                if (sync2SuccessCount >= 1)
                    AdvanceStage();
            }
            else if (currentStage == TutorialStage.T5_Run3 && currentPatternType == PatternType_st2.Run3)
            {
                hasSuccessRun3 = true;
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
        if (autoPatternRoutine != null)
        {
            StopCoroutine(autoPatternRoutine);
            autoPatternRoutine = null;
        }

        yield return new WaitForSeconds(stageTransitionDelay);

        HideStageComplete();
        AdvanceStage();
    }

    void AdvanceStage()
    {
        StartStage(currentStage + 1);
    }

    public string GetCurrentStageInfo()
    {
        switch (currentStage)
        {
            case TutorialStage.T0_Telegraph: return $"Telegraph {telegraphCount}/2";
            case TutorialStage.T1_Pop: return $"Pop {popCount}/2";
            case TutorialStage.T2_Catch: return $"Catch {goodOrBetterCount}/2";
            case TutorialStage.T4_Sync2: return hasSuccessSync2 ? "Sync2 Done" : "Sync2 Try";
            case TutorialStage.T5_Run3: return hasSuccessRun3 ? "Run3 Done" : "Run3 Try";
            case TutorialStage.Complete: return "Complete";
            case TutorialStage.WaitBeforeMain: return "Wait...";
            default: return "Unknown";
        }
    }

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

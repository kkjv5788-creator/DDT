using System;
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

    [Header("Haptics (Tutorial)")]
    [SerializeField] private TutorialCatchHaptics_st2 tutorialHaptics;
    [Serializable]
    public class StageDialogueBlock
    {
        public TutorialStage stage;

        [Header("Before (스테이지 시작 전 대사)")]
        [TextArea(3, 12)]
        public string beforeDialogue; // 줄바꿈=한 줄씩

        [Header("During (상호작용 진행 중 화면)")]
        [Tooltip("대사 끝난 뒤~성공 처리 전까지 상호작용 진행 중에 dialogueText에 띄울 문구.\n" +
                 "플레이스홀더: {stage} {telegraphCount} {popCount} {goodCount} {specialCaught} {specialTarget}")]
        [TextArea(2, 8)]
        public string duringText;

        [Tooltip("상호작용 진행 중에 nextHintText에 띄울 힌트(예: '잡아보세요', '오른손 트리거로 잡기')")]
        public string duringHintText;

        [Header("After (스테이지 성공 후 대사)")]
        [TextArea(3, 12)]
        public string afterDialogue;  // 줄바꿈=한 줄씩
    }

    public enum DialoguePhase
    {
        None,
        BeforeStage,
        AfterStage
    }

    // =========================================================
    // ✅ 네가 실제로 쓰는 UI는 딱 2개만
    // =========================================================
    [Header("Dialogue Blocks")]
    public List<StageDialogueBlock> dialogueBlocks = new List<StageDialogueBlock>();

    [Header("대사 표시 텍스트")]
    [Tooltip("모니터 패널에 표시될 대사(TextMeshProUGUI)")]
    public TextMeshProUGUI dialogueText;

    [Header("다음 안내 텍스트")]
    [Tooltip("예: '오른손 트리거로 다음' 을 표시할 TextMeshProUGUI")]
    public TextMeshProUGUI nextHintText;

    [Header("대사 진행 입력")]
    public OVRInput.Controller dialogueAdvanceController = OVRInput.Controller.RTouch;
    public OVRInput.Button dialogueAdvanceButton = OVRInput.Button.PrimaryIndexTrigger;

    // =========================================================
    // ✅ 기존 UIController/DebugHUD 호환용(인스펙터 숨김)
    // =========================================================
    [HideInInspector] public TextMeshProUGUI instructionText;
    [HideInInspector] public TextMeshProUGUI progressText;
    [HideInInspector] public TextMeshProUGUI skipText;
    [HideInInspector] public TextMeshProUGUI stageCompleteText;
    [HideInInspector] public GameObject tutorialCanvas;

    // =========================================================
    [Header("현재 스테이지")]
    public TutorialStage currentStage = TutorialStage.T0_Telegraph;

    [Header("튜토리얼 전용 몰드 (메인과 별도)")]
    public TutorialMoldController_st2[] tutorialMolds = new TutorialMoldController_st2[3];

    [Header("튜토리얼 전용 손 센서")]
    public TutorialHandSensor_st2 leftSensor;
    public TutorialHandSensor_st2 rightSensor;

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

    [Header("판정 설정")]
    public float perfectWindow = 0.04f;
    public float goodWindow = 0.075f;

    [Header("스냅 연출(튜토리얼)")]
    public float snapHoldSeconds = 0.2f;

    [Header("Pop 애니 리드타임 (퐁 소리보다 먼저 열기)")]
    public float popAnimLeadSeconds = 0.25f;

    [Header("특수 패턴 재시도")]
    public float specialAttemptTimeout = 3.0f;
    public float specialRetryDelay = 0.5f;

    // =========================================================
    // ✅ 스테이지별 전환 딜레이 (성공 직후 잠깐 기다렸다가 After 대사/다음으로)
    // =========================================================
    [Serializable]
    public class StageClearDelay
    {
        public TutorialStage stage;
        [Min(0f)] public float delay;
    }

    [Header("Stage Transition Delay (Per Stage)")]
    [Tooltip("여기에 스테이지별 딜레이를 넣으면, 성공 직후 그 시간만큼 기다린 뒤 After 대사로 넘어감")]
    public List<StageClearDelay> perStageClearDelays = new List<StageClearDelay>()
    {
        new StageClearDelay{ stage = TutorialStage.T0_Telegraph, delay = 0.20f },
        new StageClearDelay{ stage = TutorialStage.T1_Pop,      delay = 0.35f },
        new StageClearDelay{ stage = TutorialStage.T2_Catch,    delay = 0.25f },
        new StageClearDelay{ stage = TutorialStage.T4_Sync2,    delay = 0.35f },
        new StageClearDelay{ stage = TutorialStage.T5_Run3,     delay = 0.35f },
    };

    [Tooltip("리스트에 해당 스테이지가 없을 때 사용할 기본 딜레이")]
    [Min(0f)] public float defaultStageClearDelay = 0.30f;

    [Tooltip("Time.timeScale 영향 없이 기다릴지(추천: true)")]
    public bool stageClearDelayUseRealtime = true;

    private Coroutine _stageClearRoutine;
    private bool _interactionLocked = false;

    // 진행 카운터
    private int telegraphCount = 0;
    private int popCount = 0;
    private int goodOrBetterCount = 0;

    // 특수 패턴 목표/진행
    private int specialTargetCount = 0;   // 2 또는 3
    private int specialCaughtCount = 0;   // 현재 시도에서 잡은 개수

    private PatternType_st2 currentPatternType = PatternType_st2.Normal;
    private bool hasSuccessSync2 = false;
    private bool hasSuccessRun3 = false;

    private Coroutine autoPatternRoutine;

    // ===== Dialogue Runtime =====
    private readonly Queue<string> _dialogueQueue = new Queue<string>();
    private bool _dialogueWaiting = false;
    private DialoguePhase _dialoguePhase = DialoguePhase.None;
    private Action _onDialogueDone = null;

    // ===== During UI cache (불필요한 SetText 최소화) =====
    private string _lastDuringDialogue = null;
    private string _lastDuringHint = null;

    void Start()
    {
        AutoBindLegacyUIForCompatibility();
    }

    void Update()
    {
        if (GameFlowController_st2.Instance != null &&
            GameFlowController_st2.Instance.CurrentState == GameStatest2.Paused)
            return;

        // ✅ 대사 진행 중이면: 오른손 트리거로만 Next, 나머지(잡기 포함) 차단
        if (_dialogueWaiting)
        {
            if (nextHintText != null) nextHintText.text = "오른손 트리거로 다음";

            if (OVRInput.GetDown(dialogueAdvanceButton, dialogueAdvanceController))
                AdvanceDialogue();

            return;
        }

        // ✅ 성공 직후 딜레이(전환 대기) 동안 입력/상호작용 차단
        if (_interactionLocked)
        {
            return;
        }

        // ✅ 상호작용 진행 중 UI 반영(네가 정한 duringText/duringHintText)
        ApplyDuringUI(currentStage);

        // ✅ 스테이지별로 잡기 허용
        if (IsCatchingEnabled())
        {
            ProcessHandInput(leftSensor, OVRInput.Controller.LTouch);
            ProcessHandInput(rightSensor, OVRInput.Controller.RTouch);
        }
    }

    private Coroutine _bgmRoutine;

    void OnEnable()
    {
        AutoBindLegacyUIForCompatibility();

        InitializeSensors();
        HardResetTutorial();

        // ✅ BGM은 1프레임 늦게 시작(초기화 충돌 방지)
        if (_bgmRoutine != null) StopCoroutine(_bgmRoutine);
        _bgmRoutine = StartCoroutine(StartTutorialBGM_NextFrame());

        StartStage(TutorialStage.T0_Telegraph);
    }

    IEnumerator StartTutorialBGM_NextFrame()
    {
        yield return null;
        StartTutorialBGM();
    }

    void OnDisable()
    {
        if (_bgmRoutine != null)
        {
            StopCoroutine(_bgmRoutine);
            _bgmRoutine = null;
        }

        if (_stageClearRoutine != null)
        {
            StopCoroutine(_stageClearRoutine);
            _stageClearRoutine = null;
        }

        StopAllCoroutines();
        StopTutorialBGM();
        CleanupAllTutorialFish();
    }

    public void HardResetTutorial()
    {
        currentStage = TutorialStage.T0_Telegraph;

        telegraphCount = 0;
        popCount = 0;
        goodOrBetterCount = 0;

        specialTargetCount = 0;
        specialCaughtCount = 0;

        hasSuccessSync2 = false;
        hasSuccessRun3 = false;

        _dialogueQueue.Clear();
        _dialogueWaiting = false;
        _dialoguePhase = DialoguePhase.None;
        _onDialogueDone = null;

        _lastDuringDialogue = null;
        _lastDuringHint = null;

        _interactionLocked = false;
        if (_stageClearRoutine != null)
        {
            StopCoroutine(_stageClearRoutine);
            _stageClearRoutine = null;
        }

        if (stageCompleteText != null) stageCompleteText.gameObject.SetActive(false);
        if (progressText != null) progressText.text = "";
        if (dialogueText != null) dialogueText.text = "";
        if (nextHintText != null) nextHintText.text = "";
    }

    // =========================================================
    // ✅ 외부(UIController/DebugHUD) 호환
    // =========================================================
    void AutoBindLegacyUIForCompatibility()
    {
        if (instructionText == null) instructionText = dialogueText;
        if (progressText == null) progressText = nextHintText;
    }

    bool IsCatchingEnabled()
    {
        if (_dialogueWaiting) return false;
        if (_interactionLocked) return false;

        return currentStage == TutorialStage.T2_Catch
            || currentStage == TutorialStage.T4_Sync2
            || currentStage == TutorialStage.T5_Run3;
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

    // =========================================================
    // Stage Start (Before 대사 → 액션 시작)
    // =========================================================
    void StartStage(TutorialStage stage)
    {
        currentStage = stage;

        StopAutoRoutine();
        CleanupAllTutorialFish();

        specialTargetCount = 0;
        specialCaughtCount = 0;

        // during UI 캐시 초기화 (스테이지 바뀌면 즉시 갱신되게)
        _lastDuringDialogue = null;
        _lastDuringHint = null;

        _interactionLocked = false;
        if (_stageClearRoutine != null)
        {
            StopCoroutine(_stageClearRoutine);
            _stageClearRoutine = null;
        }

        BeginStageDialogue(stage, DialoguePhase.BeforeStage, () =>
        {
            StartStageAction(stage);
        });
    }

    void StartStageAction(TutorialStage stage)
    {
        // ✅ 상호작용 시작 시점에 네가 지정한 during UI를 즉시 반영
        ApplyDuringUI(stage, force: true);

        switch (stage)
        {
            case TutorialStage.T0_Telegraph:
                currentPatternType = PatternType_st2.Normal;
                autoPatternRoutine = StartCoroutine(AutoTelegraphOnly());
                break;

            case TutorialStage.T1_Pop:
                currentPatternType = PatternType_st2.Normal;
                autoPatternRoutine = StartCoroutine(AutoPopWatchOnly());
                break;

            case TutorialStage.T2_Catch:
                currentPatternType = PatternType_st2.Normal;
                autoPatternRoutine = StartCoroutine(AutoCatchNormal());
                break;

            case TutorialStage.T4_Sync2:
                currentPatternType = PatternType_st2.Sync2;
                autoPatternRoutine = StartCoroutine(RunSync2UntilSuccess());
                break;

            case TutorialStage.T5_Run3:
                currentPatternType = PatternType_st2.Run3;
                autoPatternRoutine = StartCoroutine(RunRun3UntilSuccess());
                break;

            case TutorialStage.Complete:
                StartStage(TutorialStage.WaitBeforeMain);
                break;

            case TutorialStage.WaitBeforeMain:
                StartCoroutine(WaitThenStartMain());
                break;
        }
    }

    void StopAutoRoutine()
    {
        if (autoPatternRoutine != null)
        {
            StopCoroutine(autoPatternRoutine);
            autoPatternRoutine = null;
        }
    }

    float GetStageClearDelay(TutorialStage stage)
    {
        for (int i = 0; i < perStageClearDelays.Count; i++)
        {
            if (perStageClearDelays[i] != null && perStageClearDelays[i].stage == stage)
                return Mathf.Max(0f, perStageClearDelays[i].delay);
        }
        return Mathf.Max(0f, defaultStageClearDelay);
    }

    // =========================================================
    // ✅ During UI (상호작용 진행중 화면을 네가 정하도록)
    // =========================================================
    void ApplyDuringUI(TutorialStage stage, bool force = false)
    {
        if (_dialogueWaiting) return;

        var block = FindBlock(stage);

        string rawDuring = (block != null) ? block.duringText : "";
        string rawHint = (block != null) ? block.duringHintText : "";

        string during = FormatPlaceholders(rawDuring);
        string hint = FormatPlaceholders(rawHint);

        if (force || _lastDuringDialogue != during)
        {
            if (dialogueText != null) dialogueText.text = string.IsNullOrEmpty(during) ? "" : during;
            _lastDuringDialogue = during;
        }

        if (force || _lastDuringHint != hint)
        {
            if (nextHintText != null) nextHintText.text = string.IsNullOrEmpty(hint) ? "" : hint;
            _lastDuringHint = hint;
        }
    }

    string FormatPlaceholders(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        return s
            .Replace("{stage}", currentStage.ToString())
            .Replace("{telegraphCount}", telegraphCount.ToString())
            .Replace("{popCount}", popCount.ToString())
            .Replace("{goodCount}", goodOrBetterCount.ToString())
            .Replace("{specialCaught}", specialCaughtCount.ToString())
            .Replace("{specialTarget}", specialTargetCount.ToString());
    }

    // =========================================================
    // Dialogue System
    // =========================================================
    void BeginStageDialogue(TutorialStage stage, DialoguePhase phase, Action onDone)
    {
        _dialogueQueue.Clear();
        _dialoguePhase = phase;
        _onDialogueDone = onDone;

        var block = FindBlock(stage);
        string multi = "";
        if (block != null)
            multi = (phase == DialoguePhase.BeforeStage) ? block.beforeDialogue : block.afterDialogue;

        var lines = SplitLines(multi);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
                _dialogueQueue.Enqueue(lines[i].Trim());
        }

        if (_dialogueQueue.Count == 0)
        {
            _dialogueWaiting = false;
            _dialoguePhase = DialoguePhase.None;

            onDone?.Invoke();
            return;
        }

        _dialogueWaiting = true;
        ShowNextDialogueLine();
    }

    void AdvanceDialogue()
    {
        if (!_dialogueWaiting) return;

        if (_dialogueQueue.Count > 0)
        {
            ShowNextDialogueLine();
            return;
        }

        _dialogueWaiting = false;
        _dialoguePhase = DialoguePhase.None;

        var cb = _onDialogueDone;
        _onDialogueDone = null;
        cb?.Invoke();
    }

    void ShowNextDialogueLine()
    {
        if (dialogueText == null) return;

        if (_dialogueQueue.Count == 0)
            return;

        dialogueText.text = _dialogueQueue.Dequeue();
    }

    string[] SplitLines(string multiLine)
    {
        if (string.IsNullOrWhiteSpace(multiLine)) return Array.Empty<string>();

        var raw = multiLine.Replace("\r\n", "\n").Split('\n');
        var list = new List<string>();
        for (int i = 0; i < raw.Length; i++)
        {
            var t = raw[i].Trim();
            if (!string.IsNullOrEmpty(t)) list.Add(t);
        }
        return list.ToArray();
    }

    StageDialogueBlock FindBlock(TutorialStage stage)
    {
        for (int i = 0; i < dialogueBlocks.Count; i++)
        {
            if (dialogueBlocks[i] != null && dialogueBlocks[i].stage == stage)
                return dialogueBlocks[i];
        }
        return null;
    }

    // =========================================================
    // Stage 0: Telegraph Only
    // =========================================================
    IEnumerator AutoTelegraphOnly()
    {
        while (currentStage == TutorialStage.T0_Telegraph)
        {
            int moldIndex = UnityEngine.Random.Range(0, 3);
            yield return StartCoroutine(PlayTelegraphOnly(moldIndex));
            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    IEnumerator PlayTelegraphOnly(int moldIndex)
    {
        if (tutorialMolds[moldIndex] == null) yield break;

        if (telegraphSource != null && telegraphClip != null)
            telegraphSource.PlayOneShot(telegraphClip);

        tutorialMolds[moldIndex].PlayTelegraphVisual();

        OnTelegraphPlayed();
        yield return null;
    }

    // =========================================================
    // Stage 1: Pop Watch Only
    // =========================================================
    IEnumerator AutoPopWatchOnly()
    {
        while (currentStage == TutorialStage.T1_Pop)
        {
            int moldIndex = UnityEngine.Random.Range(0, 3);
            yield return StartCoroutine(SpawnTelegraphToPopFish(moldIndex));
            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    // =========================================================
    // Stage 2: Normal Catch
    // =========================================================
    IEnumerator AutoCatchNormal()
    {
        while (currentStage == TutorialStage.T2_Catch)
        {
            int moldIndex = UnityEngine.Random.Range(0, 3);
            yield return StartCoroutine(SpawnTelegraphToPopFish(moldIndex));
            yield return new WaitForSeconds(manualPatternInterval);
        }
    }

    // =========================================================
    // Special: Sync2
    // =========================================================
    IEnumerator RunSync2UntilSuccess()
    {
        specialTargetCount = 2;

        while (currentStage == TutorialStage.T4_Sync2)
        {
            specialCaughtCount = 0;
            ApplyDuringUI(currentStage, force: true);

            CleanupAllTutorialFish();

            if (cueSource != null && sync2Cue != null)
                cueSource.PlayOneShot(sync2Cue);

            yield return new WaitForSeconds(0.35f);

            int[][] pairs = { new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 0, 2 } };
            int[] selectedPair = pairs[UnityEngine.Random.Range(0, 3)];

            foreach (int moldIndex in selectedPair)
                StartCoroutine(SpawnTelegraphToPopFish(moldIndex));

            float t = 0f;
            while (t < specialAttemptTimeout && currentStage == TutorialStage.T4_Sync2)
            {
                if (specialCaughtCount >= specialTargetCount)
                    break;

                t += Time.deltaTime;
                yield return null;
            }

            if (specialCaughtCount >= specialTargetCount)
            {
                hasSuccessSync2 = true;
                CompleteStageAndWaitNextTrigger(TutorialStage.T4_Sync2);
                yield break;
            }

            yield return new WaitForSeconds(specialRetryDelay);
        }
    }

    // =========================================================
    // Special: Run3
    // =========================================================
    IEnumerator RunRun3UntilSuccess()
    {
        specialTargetCount = 3;

        while (currentStage == TutorialStage.T5_Run3)
        {
            specialCaughtCount = 0;
            ApplyDuringUI(currentStage, force: true);

            CleanupAllTutorialFish();

            if (cueSource != null && run3Cue != null)
                cueSource.PlayOneShot(run3Cue);

            yield return new WaitForSeconds(0.35f);

            for (int i = 0; i < 3; i++)
            {
                StartCoroutine(SpawnTelegraphToPopFish(i));
                yield return new WaitForSeconds(runStepInterval);
            }

            float t = 0f;
            while (t < specialAttemptTimeout && currentStage == TutorialStage.T5_Run3)
            {
                if (specialCaughtCount >= specialTargetCount)
                    break;

                t += Time.deltaTime;
                yield return null;
            }

            if (specialCaughtCount >= specialTargetCount)
            {
                hasSuccessRun3 = true;
                CompleteStageAndWaitNextTrigger(TutorialStage.T5_Run3);
                yield break;
            }

            yield return new WaitForSeconds(specialRetryDelay);
        }
    }

    // =========================================================
    // Common Spawn
    // =========================================================
    IEnumerator SpawnTelegraphToPopFish(int moldIndex)
    {
        if (tutorialMolds[moldIndex] == null) yield break;

        if (telegraphSource != null && telegraphClip != null)
            telegraphSource.PlayOneShot(telegraphClip);

        tutorialMolds[moldIndex].PlayTelegraphVisual();

        OnTelegraphPlayed();

        float lead = Mathf.Max(0f, popAnimLeadSeconds);
        float pre = Mathf.Max(0f, telegraphToPopDelay - lead);
        yield return new WaitForSeconds(pre);

        tutorialMolds[moldIndex].TriggerPopAnimation();

        if (lead > 0f) yield return new WaitForSeconds(lead);

        if (popSource != null && popClip != null)
            popSource.PlayOneShot(popClip);

        double popTime = AudioSettings.dspTime;
        tutorialMolds[moldIndex].SpawnFish(popTime);

        OnPopPlayed();
    }

    // =========================================================
    // Hand Input
    // =========================================================
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

        if (result != JudgeResult_st2.Miss)
        {
            // ✅ Good/Perfect일 때만, 손별로 햅틱
            if (tutorialHaptics != null)
                tutorialHaptics.TriggerForResult(result, controller);
            target.isResolved = true;

            target.OnCaught();

            target.transform.position = sensor.transform.position;
            target.transform.SetParent(sensor.transform, true);

            StartCoroutine(SnapHoldThenRelease(target));

            OnCatchSuccess(result);

            // ✅ 카운트 변화 즉시 반영
            ApplyDuringUI(currentStage, force: true);
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

    // =========================================================
    // Progress Events
    // =========================================================
    void OnTelegraphPlayed()
    {
        if (currentStage != TutorialStage.T0_Telegraph) return;

        telegraphCount++;
        ApplyDuringUI(currentStage, force: true);

        if (telegraphCount >= 2)
            CompleteStageAndWaitNextTrigger(TutorialStage.T0_Telegraph);
    }

    void OnPopPlayed()
    {
        if (currentStage != TutorialStage.T1_Pop) return;

        popCount++;
        ApplyDuringUI(currentStage, force: true);

        if (popCount >= 2)
            CompleteStageAndWaitNextTrigger(TutorialStage.T1_Pop);
    }

    void OnCatchSuccess(JudgeResult_st2 result)
    {
        if (result != JudgeResult_st2.Perfect && result != JudgeResult_st2.Good)
            return;

        if (currentStage == TutorialStage.T2_Catch)
        {
            goodOrBetterCount++;
            ApplyDuringUI(currentStage, force: true);

            if (goodOrBetterCount >= 2)
                CompleteStageAndWaitNextTrigger(TutorialStage.T2_Catch);
        }
        else if (currentStage == TutorialStage.T4_Sync2 && currentPatternType == PatternType_st2.Sync2)
        {
            specialCaughtCount = Mathf.Min(specialTargetCount, specialCaughtCount + 1);
            ApplyDuringUI(currentStage, force: true);
        }
        else if (currentStage == TutorialStage.T5_Run3 && currentPatternType == PatternType_st2.Run3)
        {
            specialCaughtCount = Mathf.Min(specialTargetCount, specialCaughtCount + 1);
            ApplyDuringUI(currentStage, force: true);
        }
    }

    void OnCatchMiss()
    {
        // 필요하면 Miss 피드백 추가 가능
    }

    // =========================================================
    // Stage Completion -> (딜레이) -> AFTER 대사 -> 다음 스테이지 진입
    // =========================================================
    void CompleteStageAndWaitNextTrigger(TutorialStage stage)
    {
        if (currentStage != stage) return;

        // 이미 전환 중이면 중복 방지
        if (_stageClearRoutine != null) return;

        _stageClearRoutine = StartCoroutine(CoCompleteStageWithDelay(stage));
    }

    IEnumerator CoCompleteStageWithDelay(TutorialStage stage)
    {
        _interactionLocked = true;

        // 추가 스폰/루프 중단(연출 마무리 시간 확보)
        StopAutoRoutine();

        float d = GetStageClearDelay(stage);
        if (d > 0f)
        {
            if (stageClearDelayUseRealtime) yield return new WaitForSecondsRealtime(d);
            else yield return new WaitForSeconds(d);
        }

        // 정리 후 After 대사
        CleanupAllTutorialFish();

        BeginStageDialogue(stage, DialoguePhase.AfterStage, () =>
        {
            _interactionLocked = false;

            if (stage == TutorialStage.T5_Run3)
                StartStage(TutorialStage.Complete);
            else
                StartStage(stage + 1);
        });

        _stageClearRoutine = null;
    }

    // =========================================================
    // ✅ DebugHUD 호환용(GetCurrentStageInfo)
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
        StopAutoRoutine();
        StopTutorialBGM();
        CleanupAllTutorialFish();

        yield return new WaitForSeconds(waitBeforeMainDuration);

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

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

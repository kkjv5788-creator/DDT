using Project.Data;
using Project.Gameplay.Kimbap;
using Project.Gameplay.Plate;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Core
{
    public class RhythmConductor : MonoBehaviour
    {
        [Header("Refs")]
        public KimbapSpawner kimbapSpawner;
        public PlateController plateController;

        [Header("Audio")]
        public AudioSource bgmSource;
        public AudioSource sfxSource; // guideSfx can use this

        [Header("Timings")]
        public float resultHoldSeconds = 1.2f;

        RhythmTriggerListSO list;
        int index;
        bool isTutorial;

        RhythmState state = RhythmState.Waiting;

        // runtime trigger params (assist overrides apply here)
        RhythmTriggerSO cur;
        int targetSlices;
        float judgeTimeLeft;
        float minKnifeSpeed;
        float sliceCooldownSeconds;
        bool useContactTime;
        int minContactMs;

        int successCount;

        // tutorial assist
        int failCountThisStep;
        bool assistAppliedThisStep;

        bool running;

        void OnEnable()
        {
            GameEvents.SliceSuccess += OnSliceSuccess;
            GameEvents.TutorialSkipped += OnTutorialSkipStop;
        }

        void OnDisable()
        {
            GameEvents.SliceSuccess -= OnSliceSuccess;
            GameEvents.TutorialSkipped -= OnTutorialSkipStop;
        }

        public void SetList(RhythmTriggerListSO triggerList, bool isTutorial)
        {
            list = triggerList;
            this.isTutorial = isTutorial;
            index = 0;
            failCountThisStep = 0;
            assistAppliedThisStep = false;
        }

        public void Begin()
        {
            if (list == null || list.triggers == null || list.triggers.Count == 0) return;
            running = true;
            StopAllCoroutines();
            StartCoroutine(MainLoop());
        }

        public void ForceIdleWaiting()
        {
            running = false;
            SetState(RhythmState.Waiting);
            // keep empty plate + fresh kimbap visible, but no judging
            if (plateController) plateController.PrepareEmptyPlate();
            if (kimbapSpawner) kimbapSpawner.SpawnFresh();
        }

        IEnumerator MainLoop()
        {
            // BGM
            SetupBgm();

            while (running)
            {
                if (index >= list.triggers.Count)
                {
                    if (isTutorial)
                    {
                        GameEvents.RaiseTutorialCompleted();
                        yield break;
                    }
                    else
                    {
                        // loop main list by default
                        index = 0;
                    }
                }

                cur = list.triggers[index];

                // Waiting
                SetState(RhythmState.Waiting);
                plateController?.PrepareEmptyPlate();
                kimbapSpawner?.SpawnFresh();
                yield return null; // one frame

                // Guiding
                SetState(RhythmState.Guiding);
                GameEvents.RaiseTriggerStarted(cur);
                PlayGuide(cur);
                yield return new WaitForSeconds(cur.guideLeadTime);

                // Judging
                ApplyTriggerParams(cur);
                successCount = 0;
                SetState(RhythmState.Judging);

                float t = judgeTimeLeft;
                while (t > 0f && successCount < targetSlices)
                {
                    t -= Time.deltaTime;
                    judgeTimeLeft = t;
                    yield return null;
                }

                bool success = (successCount >= targetSlices);

                // Result
                SetState(RhythmState.Result);
                GameEvents.RaiseRoundResult(success);

                if (success) plateController?.ShowResultPlateSuccess();
                else plateController?.ShowResultPlateFail();

                yield return new WaitForSeconds(resultHoldSeconds);

                // Cleanup
                SetState(RhythmState.Cleanup);
                kimbapSpawner?.Cleanup();
                plateController?.CleanupLoosePieces(); // optional
                yield return null;

                // Update tutorial assist state
                if (isTutorial)
                {
                    if (success)
                    {
                        // next step
                        failCountThisStep = 0;
                        assistAppliedThisStep = false;
                        index++;
                    }
                    else
                    {
                        // repeat same trigger, maybe apply assist
                        failCountThisStep++;
                        if (!assistAppliedThisStep && cur.allowAssistOverrides && failCountThisStep >= 2)
                        {
                            assistAppliedThisStep = true; // will apply in ApplyTriggerParams
                        }
                        // index unchanged
                    }
                }
                else
                {
                    index++;
                }
            }
        }

        void SetupBgm()
        {
            if (!bgmSource) return;
            bgmSource.Stop();
            bgmSource.clip = list ? list.bgm : null;
            if (bgmSource.clip)
            {
                bgmSource.loop = list.loopBgm;
                bgmSource.Play();
            }
        }

        void PlayGuide(RhythmTriggerSO t)
        {
            if (!sfxSource || !t || !t.guideSfx) return;
            sfxSource.PlayOneShot(t.guideSfx);
        }

        void ApplyTriggerParams(RhythmTriggerSO t)
        {
            // base
            targetSlices = Mathf.Max(1, t.targetSlices);
            judgeTimeLeft = Mathf.Max(0.1f, t.judgeTimeSeconds);
            minKnifeSpeed = Mathf.Max(0f, t.minKnifeSpeed);
            sliceCooldownSeconds = Mathf.Max(0.01f, t.sliceCooldownSeconds);
            useContactTime = t.useContactTime;
            minContactMs = Mathf.Max(0, t.minContactMs);

            // assist overrides (tutorial only)
            if (isTutorial && assistAppliedThisStep)
            {
                judgeTimeLeft *= 1.25f;
                minKnifeSpeed *= 0.85f;
                targetSlices = Mathf.Max(1, targetSlices - 1);
                if (useContactTime) minContactMs = Mathf.Max(0, Mathf.RoundToInt(minContactMs * 0.7f));
            }

            // push params to KnifeSlicer via static
            Gameplay.Knife.KnifeSlicerRuntime.SetRuntimeParams(
                isJudging: true,
                minKnifeSpeed: minKnifeSpeed,
                sliceCooldownSeconds: sliceCooldownSeconds,
                useContactTime: useContactTime,
                minContactMs: minContactMs
            );
        }

        void SetState(RhythmState s)
        {
            state = s;

            // Knife runtime judgable flag
            bool judging = (state == RhythmState.Judging);
            if (!judging)
            {
                Gameplay.Knife.KnifeSlicerRuntime.SetRuntimeParams(
                    isJudging: false,
                    minKnifeSpeed: minKnifeSpeed,
                    sliceCooldownSeconds: sliceCooldownSeconds,
                    useContactTime: useContactTime,
                    minContactMs: minContactMs
                );
            }

            GameEvents.RaiseStateChanged(state);
        }

        void OnSliceSuccess(Vector3 hitPos, Vector3 hitNormal, float speed)
        {
            if (state != RhythmState.Judging) return;
            successCount++;
        }

        void OnTutorialSkipStop()
        {
            // If tutorial skipped mid-loop, stop loop quickly.
            running = false;
            StopAllCoroutines();
        }

        // Optional: expose for DebugHUD
        public RhythmState State => state;
        public int SuccessCount => successCount;
        public int TargetSlices => targetSlices;
        public float JudgeTimeLeft => judgeTimeLeft;
    }
}

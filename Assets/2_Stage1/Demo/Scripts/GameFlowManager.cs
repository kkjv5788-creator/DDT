using UnityEngine;
using Project.Core;
using Project.Data;

namespace Project.Core
{
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Data")]
        public RhythmTriggerListSO tutorialList;
        public RhythmTriggerListSO mainList;

        [Header("Refs")]
        public RhythmConductor conductor;
        public TutorialController tutorialController;
        public RadioClickable radio;

        [Header("Start Mode")]
        public bool startWithTutorial = true;

        bool tutorialDoneOrSkipped;

        void Awake()
        {
            if (!conductor) conductor = GetComponentInChildren<RhythmConductor>(true);
        }

        void OnEnable()
        {
            GameEvents.TutorialSkipped += OnTutorialEnded;
            GameEvents.TutorialCompleted += OnTutorialEnded;
            GameEvents.MainStartRequested += OnMainStartRequested;
        }

        void OnDisable()
        {
            GameEvents.TutorialSkipped -= OnTutorialEnded;
            GameEvents.TutorialCompleted -= OnTutorialEnded;
            GameEvents.MainStartRequested -= OnMainStartRequested;
        }

        void Start()
        {
            if (startWithTutorial && tutorialList)
            {
                tutorialDoneOrSkipped = false;
                radio?.SetMainStartEnabled(false);

                conductor.SetList(tutorialList, isTutorial: true);
                GameEvents.RaiseFeedbackSetChanged(tutorialList.feedbackSet);
                GameEvents.RaiseModeChanged(true);
                conductor.Begin();
            }
            else
            {
                tutorialDoneOrSkipped = true;
                radio?.SetMainStartEnabled(true);

                conductor.SetList(mainList, isTutorial: false);
                GameEvents.RaiseFeedbackSetChanged(mainList ? mainList.feedbackSet : null);
                GameEvents.RaiseModeChanged(false);
                conductor.Begin();
            }
        }

        void OnTutorialEnded()
        {
            tutorialDoneOrSkipped = true;
            radio?.SetMainStartEnabled(true);

            // Stop conductor if running tutorial; keep scene idle until radio click starts main.
            conductor.StopAllCoroutines();
            conductor.ForceIdleWaiting();
            GameEvents.RaiseModeChanged(false); // now "ready for main"
        }

        void OnMainStartRequested()
        {
            if (!tutorialDoneOrSkipped) return;
            if (!mainList) return;

            radio?.SetMainStartEnabled(false);

            conductor.SetList(mainList, isTutorial: false);
            GameEvents.RaiseFeedbackSetChanged(mainList.feedbackSet);
            GameEvents.RaiseModeChanged(false);
            conductor.Begin();
        }
    }
}

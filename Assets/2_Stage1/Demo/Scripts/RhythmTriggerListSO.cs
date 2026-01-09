using System.Collections.Generic;
using UnityEngine;

namespace Project.Data
{
    [CreateAssetMenu(menuName = "Project/Rhythm/RhythmTriggerListSO")]
    public class RhythmTriggerListSO : ScriptableObject
    {
        public AudioClip bgm;
        public bool loopBgm = true;

        public FeedbackSetSO feedbackSet;

        public List<RhythmTriggerSO> triggers = new List<RhythmTriggerSO>();
    }
}

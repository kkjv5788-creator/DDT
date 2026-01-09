using UnityEngine;
using Project.Core;

namespace Project.Core
{
    public class RadioClickable : MonoBehaviour
    {
        [Tooltip("튜토리얼 종료/스킵 이후에만 메인 시작 클릭을 허용")]
        [SerializeField] bool mainStartEnabled;

        // Editor test
        void OnMouseDown()
        {
            TryClick();
        }

        // For VR pointer/interaction: hook this from UnityEvent (e.g., OVRInteractable)
        public void TryClick()
        {
            if (!mainStartEnabled) return;
            GameEvents.RaiseMainStartRequested();
        }

        public void SetMainStartEnabled(bool enabled)
        {
            mainStartEnabled = enabled;
        }
    }
}

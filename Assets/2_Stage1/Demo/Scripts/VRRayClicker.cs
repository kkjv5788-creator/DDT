using UnityEngine;
using Project.Core;

namespace Project.Input
{
    public class VRRayClicker : MonoBehaviour
    {
        [Header("Ray Origin")]
        [Tooltip("보통 RightHandAnchor 또는 RightControllerAnchor")]
        public Transform rayOrigin;

        [Header("Ray Settings")]
        public float maxDistance = 8f;
        public LayerMask interactMask = ~0; // 필요하면 UI/Interact 레이어만

        [Header("Input")]
        [Tooltip("라디오 클릭 버튼: 오른손 검지 트리거 추천")]
        public bool useRightIndexTrigger = true;

        void Update()
        {
            if (!rayOrigin) return;

            bool clickDown = false;

#if UNITY_ANDROID || UNITY_EDITOR
            if (useRightIndexTrigger)
                clickDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            else
                clickDown = OVRInput.GetDown(OVRInput.Button.One); // A 버튼
#endif

            if (!clickDown) return;

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactMask, QueryTriggerInteraction.Ignore))
            {
                // 라디오 클릭
                var radio = hit.collider.GetComponentInParent<RadioClickable>();
                if (radio != null)
                {
                    radio.TryClick();
                }
            }
        }

        // 에디터에서 Ray 확인용
        void OnDrawGizmosSelected()
        {
            if (!rayOrigin) return;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + rayOrigin.forward * maxDistance);
        }
    }
}

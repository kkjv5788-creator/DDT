using UnityEngine;
using Project.Core;

namespace Project.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class BlockHitDetector : MonoBehaviour
    {
        [Tooltip("Blocker layer mask")]
        public LayerMask blockerMask;

        void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.collider == null) return;

            if (((1 << collision.collider.gameObject.layer) & blockerMask) != 0)
            {
                Vector3 p = collision.GetContact(0).point;
                GameEvents.RaiseWrongCut(p);
            }
        }
    }
}

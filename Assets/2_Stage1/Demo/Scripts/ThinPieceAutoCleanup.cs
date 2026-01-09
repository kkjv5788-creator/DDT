using UnityEngine;

namespace Project.Gameplay.Kimbap
{
    public class ThinPieceAutoCleanup : MonoBehaviour
    {
        public float lifeSeconds = 2.5f;
        float born;

        void OnEnable()
        {
            born = Time.time;
        }

        void Update()
        {
            if (Time.time - born >= lifeSeconds)
            {
                Destroy(gameObject);
            }
        }
    }
}

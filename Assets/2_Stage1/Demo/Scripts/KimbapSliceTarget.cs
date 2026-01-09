using UnityEngine;

namespace Project.Gameplay.Kimbap
{
    public class KimbapSliceTarget : MonoBehaviour
    {
        public KimbapController Controller;

        void Awake()
        {
            if (!Controller) Controller = GetComponentInParent<KimbapController>();
        }
    }
}

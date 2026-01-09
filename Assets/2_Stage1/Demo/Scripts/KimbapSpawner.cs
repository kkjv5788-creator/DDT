using UnityEngine;

namespace Project.Gameplay.Kimbap
{
    public class KimbapSpawner : MonoBehaviour
    {
        public Transform anchor;
        public GameObject kimbapPrefab;

        GameObject current;

        public void SpawnFresh()
        {
            Cleanup();
            if (!kimbapPrefab || !anchor) return;

            current = Instantiate(kimbapPrefab, anchor.position, anchor.rotation);
        }

        public void Cleanup()
        {
            if (current) Destroy(current);
            current = null;
        }
    }
}

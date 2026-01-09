using UnityEngine;

namespace Project.Data
{
    [CreateAssetMenu(menuName = "Project/Feedback/FeedbackSetSO")]
    public class FeedbackSetSO : ScriptableObject
    {
        [Header("SFX")]
        public AudioClip sfxSliceSuccess;
        public AudioClip sfxSliceFail;
        public AudioClip sfxResultSuccess;
        public AudioClip sfxResultFail;
        public AudioClip sfxWrongCut;

        [Header("VFX Prefabs (optional)")]
        public GameObject vfxSliceSuccessPrefab;
        public GameObject vfxSliceFailPrefab;
        public GameObject vfxResultSuccessPrefab;
        public GameObject vfxResultFailPrefab;
        public GameObject vfxWrongCutPrefab;

        [Header("Plates")]
        public GameObject platePrefabEmptyStack;
        public GameObject platePrefabSuccessNeat;
        public GameObject platePrefabFailExplode;

        [Header("Plating Piece")]
        public GameObject platingPiecePrefab;
        public int maxPlatingPiecesPerRound = 12;

        [Header("Defaults")]
        public float failCooldown = 0.2f;
        public float skipInputCooldown = 0.5f;
    }
}

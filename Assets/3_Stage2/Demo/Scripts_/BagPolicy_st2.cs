using UnityEngine;

public class BagPolicy_st2 : MonoBehaviour
{
    [SerializeField] private Stage2EventHub_st2 hub;
    [SerializeField] private BagManager_st2 bag;

    [Header("Bag Visual Prefab")]
    public GameObject bagFishVisualPrefab;

    public void Construct(Stage2EventHub_st2 eventHub, BagManager_st2 bagManager)
    {
        hub = eventHub;
        bag = bagManager;
    }

    private void OnEnable()
    {
        if (hub != null) hub.FishConsumeRequested += OnConsumeRequested;
    }

    private void OnDisable()
    {
        if (hub != null) hub.FishConsumeRequested -= OnConsumeRequested;
    }

    private void OnConsumeRequested(FishCatchToken_st2 fish, FishConsumeReason_st2 reason)
    {
        if (bag == null) return;
        if (reason != FishConsumeReason_st2.Success) return;

        if (bagFishVisualPrefab != null)
            bag.AddItem(bagFishVisualPrefab);
    }
}

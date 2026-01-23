using UnityEngine;
using static GameState_st2;

/// <summary>
/// Consume 결과를 보고 "성공이면 봉투에 비주얼만 추가"만 담당.
/// </summary>
[DisallowMultipleComponent]
public class BagPolicy_st2 : MonoBehaviour
{
    [Header("Event Hub")]
    [SerializeField] private Stage2EventHub_st2 hub;

    [Header("Bag Manager")]
    [SerializeField] private BagManager_st2 bag;

    [Header("Fish Visual Prefab (required)")]
    public GameObject bagFishVisualPrefab;

    private bool _bound;

    public void Construct(Stage2EventHub_st2 eventHub, BagManager_st2 bagManager)
    {
        hub = eventHub;
        bag = bagManager;
        TryAutoWire();
        Bind();
    }

    private void Awake()
    {
        TryAutoWire();
    }

    private void OnEnable()
    {
        TryAutoWire();
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void TryAutoWire()
    {
        if (hub == null)
            hub = GetComponent<Stage2EventHub_st2>();

        if (bag == null)
            bag = FindObjectOfType<BagManager_st2>();
    }

    private void Bind()
    {
        if (_bound) return;
        if (hub == null) return;

        hub.FishConsumeRequested += OnFishConsumeRequested;
        _bound = true;
    }

    private void Unbind()
    {
        if (!_bound) return;

        if (hub != null)
            hub.FishConsumeRequested -= OnFishConsumeRequested;

        _bound = false;
    }

    private void OnFishConsumeRequested(FishCatchToken_st2 fish, FishConsumeReason_st2 reason)
    {
        if (reason != FishConsumeReason_st2.Success) return;
        if (bag == null) return;

        if (bagFishVisualPrefab == null)
        {
            Debug.LogWarning("[BagPolicy_st2] bagFishVisualPrefab이 비어있어서 봉투에 추가할 수 없습니다.");
            return;
        }

        bag.AddItem(bagFishVisualPrefab);
    }
}

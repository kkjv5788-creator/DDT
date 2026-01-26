using TMPro;
using UnityEngine;

public class MainBestWageView_st2 : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text stage1BestText;
    [SerializeField] private TMP_Text stage2BestText;

    private void Start()
    {
        int best1 = BestWageStore_st2.GetBest("stage1");
        int best2 = BestWageStore_st2.GetBest("stage2");

        if (stage1BestText) stage1BestText.text = $"최고기록 {best1:N0}원";
        if (stage2BestText) stage2BestText.text = $"최고기록 {best2:N0}원";
    }
}

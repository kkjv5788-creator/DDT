using UnityEngine;

public class RadioOutlineController : MonoBehaviour
{
    [Header("Refs")]
    public GameFlowManager gameFlowManager;
    public RadioClickable radioClickable;

    void Update()
    {
        if (!gameFlowManager || !radioClickable) return;

        // WaitForRadio 상태가 되면 클릭 가능하게
        if (gameFlowManager.CurrentState == GameState.WaitForRadio)
        {
            radioClickable.SetClickable(true);
        }
        else
        {
            radioClickable.SetClickable(false);
        }
    }
}
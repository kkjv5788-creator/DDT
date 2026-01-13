using UnityEngine;

public class RadioOutlineController : MonoBehaviour
{
    [Header("Refs")]
    public GameFlowManager gameFlowManager;
    public RadioClickable radioClickable;

    bool _lastClickableState = false;

    void Update()
    {
        if (!gameFlowManager || !radioClickable) return;

        // WaitForRadio 상태가 되면 클릭 가능하게
        bool shouldBeClickable = (gameFlowManager.CurrentState == GameState.WaitForRadio);

        if (shouldBeClickable != _lastClickableState)
        {
            _lastClickableState = shouldBeClickable;

            if (shouldBeClickable)
            {
                // 🔥 튜토리얼 완료 후 클릭 가능 상태로
                // 반드시 SetTutorialCompleted 먼저 호출!
                radioClickable.SetTutorialCompleted(true);
                radioClickable.SetClickable(true);
                Debug.Log("[RadioOutlineController] Radio enabled (Tutorial Completed + Clickable)");
            }
            else
            {
                // 클릭 불가 상태로
                radioClickable.SetClickable(false);

                // PlayingMain 상태로 넘어가면 튜토리얼 플래그도 리셋
                if (gameFlowManager.CurrentState == GameState.PlayingMain)
                {
                    radioClickable.SetTutorialCompleted(false);
                }

                Debug.Log("[RadioOutlineController] Radio disabled");
            }
        }
    }
}
using UnityEngine;

public class FinalResultInputHandler : MonoBehaviour
{
    [Header("Refs")]
    public GameFlowManager gameFlowManager;

    [Header("Settings")]
    public float inputCooldown = 0.5f;

    float _lastInputTime = -999f;

    void Update()
    {
        // FinalResult 상태가 아니면 무시
        if (!gameFlowManager || gameFlowManager.CurrentState != GameState.FinalResult)
            return;

        // A 버튼만 허용
        if (OVRInput.GetDown(OVRInput.Button.One)) // A 버튼
        {
            if (Time.time - _lastInputTime > inputCooldown)
            {
                _lastInputTime = Time.time;
                OnAButtonPressed();
            }
        }
    }

    void OnAButtonPressed()
    {
        Debug.Log("[FinalResultInput] A button pressed - Going to main menu");

        if (gameFlowManager)
        {
            gameFlowManager.GoToMainMenu();
        }
    }
}

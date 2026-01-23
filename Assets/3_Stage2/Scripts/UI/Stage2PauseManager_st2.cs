// Assets/3_Stage2/Scripts/System/Stage2PauseManager_st2.cs
using System;
using UnityEngine;

public class Stage2PauseManager_st2 : MonoBehaviour
{
    public static Stage2PauseManager_st2 Instance { get; private set; }

    [Header("UI")]
    public Stage2UIManager_st2 ui;
    public bool blockPauseWhenClosingResult = true;

    [Header("Disable Targets On Pause (상호작용 루트들)")]
    public GameObject[] objectsToDisableWhenPaused;

    [Header("Input")]
    public OVRInput.Button pauseButton = OVRInput.Button.One; // A 버튼(오른손)
    public OVRInput.Controller pauseController = OVRInput.Controller.RTouch;

    public bool IsPaused { get; private set; }

    public event Action<bool> PauseChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        // A 버튼 눌렀을 때 토글
        if (OVRInput.GetDown(pauseButton, pauseController))
        {
            // 결과창에서는 무시
            if (blockPauseWhenClosingResult && GameFlowController_st2.Instance != null)
            {
                if (GameFlowController_st2.Instance.CurrentState == GameState_st2.GameStatest2.Result)
                    return;
            }

            SetPaused(!IsPaused);
        }
    }

    public void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;

        // UI
        if (ui != null) ui.ShowPause(paused);

        // 상호작용 루트 OFF/ON
        if (objectsToDisableWhenPaused != null)
        {
            for (int i = 0; i < objectsToDisableWhenPaused.Length; i++)
            {
                var go = objectsToDisableWhenPaused[i];
                if (go == null) continue;
                go.SetActive(!paused);
            }
        }

        // 시간/오디오도 같이 멈추고 싶으면 (선택)
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;

        PauseChanged?.Invoke(paused);
    }
}

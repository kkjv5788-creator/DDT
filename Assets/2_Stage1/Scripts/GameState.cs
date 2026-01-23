using UnityEngine;

public enum GameState
{
    Intro,           // ★ [추가필수] 이거 없으면 GameFlowManager에서 에러남
    Tutorial,
    WaitForRadio,
    PlayingMain,
    Paused,
    EndingDelay,
    FinalResult
}
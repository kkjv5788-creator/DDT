using UnityEngine;

public enum GameState
{
    Tutorial,        // 튜토리얼 진행 중
    WaitForRadio,    // 튜토리얼 완료 후 라디오 대기
    PlayingMain,     // 메인 게임 진행 중
    Paused,          // 일시정지
    EndingDelay,     // 마지막 트리거 완료 후 대기
    FinalResult      // 최종 결과창
}
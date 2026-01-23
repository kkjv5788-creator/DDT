using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState_st2 : MonoBehaviour
{
    public enum GameStatest2
    {
        MainMenu,
        Tutorial,
        Playing,
        Paused,
        Result
    }

    public enum JudgeResult_st2
    {
        Perfect,
        Good,
        Miss
    }

    public enum PatternType_st2
    {
        Normal,
        Sync2,
        Run3
    }

}

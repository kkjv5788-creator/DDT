using UnityEngine;

public class DebugStatus : MonoBehaviour
{
    public RhythmConductor conductor;

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 35;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        // 배경 박스
        GUI.Box(new Rect(10, 10, 900, 400), "");

        if (!conductor)
        {
            GUI.Label(new Rect(20, 20, 800, 50), "🚨 Conductor 연결 안됨! 인스펙터 확인 필요", style);
            return;
        }

        float currentTime = 0f;
        bool isPlaying = false;
        if (conductor.bgmSource)
        {
            currentTime = conductor.bgmSource.time;
            isPlaying = conductor.bgmSource.isPlaying;
        }

        string musicStatus = isPlaying ? "<color=lime>재생 중</color>" : "<color=red>멈춤</color>";
        string timeScaleStatus = (Time.timeScale > 0) ? "정상(1.0)" : "<color=red>정지(0.0)</color>";
        
        // 🔥 여기가 핵심 확인 포인트
        string tutorialStatus = conductor.isTutorialMode ? "<color=red>켜짐 (문제!)</color>" : "<color=lime>꺼짐 (정상)</color>";

        GUI.Label(new Rect(20, 20, 880, 50), $"BGM: {musicStatus} / 시간: {timeScaleStatus}", style);
        GUI.Label(new Rect(20, 70, 880, 50), $"튜토리얼 모드: {tutorialStatus}", style);
        GUI.Label(new Rect(20, 120, 880, 50), $"현재 음악 시간: {currentTime:F2} 초", style);
        
        string nextTrigger = "없음";
        if (conductor.data && conductor.data.triggers != null)
        {
            int nextIdx = conductor.CurrentTriggerIndex + 1;
            if (nextIdx < conductor.data.triggers.Length)
            {
                float targetTime = conductor.data.triggers[nextIdx].triggerTime;
                nextTrigger = $"{targetTime:F2} 초 (Wait)";
                
                if (currentTime > targetTime) 
                    nextTrigger += " <color=red><- 지연 발생!</color>";
            }
            else
            {
                nextTrigger = "종료됨";
            }
        }

        GUI.Label(new Rect(20, 170, 880, 50), $"다음 노트: {nextTrigger}", style);
        GUI.Label(new Rect(20, 220, 880, 50), $"상태: {conductor.State}", style);
        
        if (GUI.Button(new Rect(20, 300, 300, 80), "13.5초로 점프"))
        {
            if(conductor.bgmSource) conductor.bgmSource.time = 13.5f;
        }
    }
}
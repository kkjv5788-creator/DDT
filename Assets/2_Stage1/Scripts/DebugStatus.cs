using UnityEngine;

public class DebugStatus : MonoBehaviour
{
    public RhythmConductor conductor;

    void OnGUI()
    {
        // 글자 스타일 설정
        GUIStyle style = new GUIStyle();
        style.fontSize = 40;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        // 배경 박스 (잘 보이게)
        GUI.Box(new Rect(10, 10, 800, 350), "");

        if (!conductor)
        {
            GUI.Label(new Rect(20, 20, 700, 50), "Conductor 연결 안됨!", style);
            return;
        }

        float currentTime = 0f;
        bool isPlaying = false;
        if (conductor.bgmSource)
        {
            currentTime = conductor.bgmSource.time;
            isPlaying = conductor.bgmSource.isPlaying;
        }

        // 상태 출력
        string musicStatus = isPlaying ? "<color=lime>재생 중</color>" : "<color=red>멈춤</color>";
        string timeScaleStatus = (Time.timeScale > 0) ? "정상(1.0)" : "<color=red>정지(0.0)</color>";
        
        GUI.Label(new Rect(20, 20, 780, 50), $"BGM 상태: {musicStatus} / 시간배율: {timeScaleStatus}", style);
        GUI.Label(new Rect(20, 70, 780, 50), $"현재 음악 시간: {currentTime:F2} 초", style);
        
        string nextTrigger = "없음";
        if (conductor.data && conductor.data.triggers != null)
        {
            int nextIdx = conductor.CurrentTriggerIndex + 1;
            if (nextIdx < conductor.data.triggers.Length)
            {
                float targetTime = conductor.data.triggers[nextIdx].triggerTime;
                nextTrigger = $"{targetTime:F2} 초 (대기중)";
                
                // 시간이 지났는데 안 나오면 빨간색 경고
                if (currentTime > targetTime) 
                    nextTrigger += " <color=red><- 왜 발동 안함?</color>";
            }
            else
            {
                nextTrigger = "모두 종료됨";
            }
        }

        GUI.Label(new Rect(20, 120, 780, 50), $"다음 노트 시간: {nextTrigger}", style);
        GUI.Label(new Rect(20, 170, 780, 50), $"현재 상태(State): {conductor.State}", style);
        
        // 강제 실행 버튼
        if (GUI.Button(new Rect(20, 240, 300, 80), "강제 노트 시작 (Skip Time)"))
        {
            conductor.bgmSource.time = 13.5f; // 13.5초로 점프
        }
    }
}
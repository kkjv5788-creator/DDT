using System.Diagnostics;
using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    RhythmConductor _conductor;
    string _logLine = "";
    float _logTimer;

    public KnifeVelocityEstimator knifeSpeedSource;

    public void Bind(RhythmConductor c) => _conductor = c;

    public void Log(string msg)
    {
        _logLine = msg;
        _logTimer = 2.5f;
        UnityEngine.Debug.Log("[HUD] " + msg);
    }

    void Update()
    {
        if (_logTimer > 0f) _logTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (_conductor == null) return;

        GUIStyle s = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16
        };

        float x = 12, y = 12, w = 640, h = 22;

        GUI.Label(new Rect(x, y, w, h), $"State: {_conductor.State}", s); y += h;
        GUI.Label(new Rect(x, y, w, h), $"BGM Time: {_conductor.BgmTime:F3}s", s); y += h;
        GUI.Label(new Rect(x, y, w, h), $"Trigger Index: {_conductor.CurrentTriggerIndex}", s); y += h;
        GUI.Label(new Rect(x, y, w, h), $"SliceCount: {_conductor.SliceCount} / {_conductor.RequiredSliceCount}", s); y += h;

        float spd = knifeSpeedSource ? knifeSpeedSource.speed : 0f;
        GUI.Label(new Rect(x, y, w, h), $"Knife Speed: {spd:F2}", s); y += h;

        if (_logTimer > 0f)
        {
            GUI.Label(new Rect(x, y + 8, w, h), $"LOG: {_logLine}", s);
        }
    }

    [SerializeField] RhythmConductor conductor;

    void Awake()
    {
        if (conductor) _conductor = conductor;
    }

}

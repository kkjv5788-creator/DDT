using UnityEngine;

[DisallowMultipleComponent]
public class RhythmConductor : MonoBehaviour
{
    public RhythmChartData data;
    public AudioSource bgmSource;
    public KimbapSpawner spawner;
    public PlateAssembler plate;
    public float judgeDurationMultiplier = 1f;

    // scene 직렬화 필드(유지)
    public int state = 0;        // 0 Idle, 1 Playing, 2 Finished
    public int triggerIndex = 0; // 현재 판정 트리거 인덱스
    public int sliceCount = 0;   // 누적 성공 슬라이스 수

    public float SongTime { get; private set; }

    float _songStartDsp;
    bool _playing;

    void Start()
    {
        if (!bgmSource) bgmSource = GetComponent<AudioSource>();
        // 자동 시작을 원하면 아래 호출
        // StartGame();
    }

    public void StartGame()
    {
        if (!data || !bgmSource)
        {
            Debug.LogWarning("[RhythmConductor] Missing data or bgmSource.");
            return;
        }

        triggerIndex = 0;
        sliceCount = 0;
        state = 1;

        if (spawner) spawner.SpawnOrPrepare();

        _songStartDsp = (float)AudioSettings.dspTime;
        bgmSource.time = 0f;
        bgmSource.Play();
        _playing = true;
    }

    public void StopGame()
    {
        _playing = false;
        state = 2;
        if (bgmSource && bgmSource.isPlaying) bgmSource.Stop();
    }

    void Update()
    {
        if (!_playing || !bgmSource) return;

        SongTime = Mathf.Max(0f, (float)AudioSettings.dspTime - _songStartDsp);

        // 곡 끝 처리(클립 길이 기반)
        if (bgmSource.clip && SongTime >= bgmSource.clip.length)
        {
            StopGame();
        }
    }

    public bool CanSliceNow(float knifeSpeed, out RhythmWindow window)
    {
        window = default;

        if (state != 1 || data == null || data.triggers == null || data.triggers.Count == 0)
            return false;

        // 다음 트리거 기준으로 판정
        int idx = Mathf.Clamp(triggerIndex, 0, data.triggers.Count - 1);
        var t = data.triggers[idx];

        float now = SongTime;
        float early = t.time - t.earlyWindow;
        float late = t.time + t.lateWindow;

        window = new RhythmWindow
        {
            targetTime = t.time,
            early = early,
            late = late,
            minSpeed = t.minKnifeSpeed
        };

        if (knifeSpeed < t.minKnifeSpeed) return false;
        if (now < early || now > late) return false;

        return true;
    }

    public void RegisterSlice(SliceResult result)
    {
        sliceCount++;

        // 판정 성공이면 다음 트리거로
        if (data != null && data.triggers != null && data.triggers.Count > 0)
        {
            triggerIndex = Mathf.Min(triggerIndex + 1, data.triggers.Count - 1);
        }

        if (plate && result.spawnedPiece)
        {
            plate.TryAddPiece(result.spawnedPiece);
        }
    }

    public void ReportBlockedHit()
    {
        // 막힘 이벤트를 난이도 보정 등에 활용 가능
        // Debug.Log("[RhythmConductor] Blocked hit.");
    }
}

public struct RhythmWindow
{
    public float targetTime;
    public float early;
    public float late;
    public float minSpeed;
}

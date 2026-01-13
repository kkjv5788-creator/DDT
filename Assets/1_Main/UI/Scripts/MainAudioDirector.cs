using System.Collections;
using UnityEngine;

public class MainAudioDirector : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip titleBgm;
    public AudioClip lobbyBgm;

    [Header("Audio Sources (2 for crossfade)")]
    public AudioSource a;
    public AudioSource b;

    [Header("Mix")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Min(0f)] public float fadeSeconds = 0.75f;

    private AudioSource _active;
    private AudioSource _inactive;
    private Coroutine _co;

    private void Awake()
    {
        if (a == null || b == null)
        {
            Debug.LogError("[MainAudioDirector] AudioSource a/b not assigned.");
            enabled = false;
            return;
        }

        Setup2D(a);
        Setup2D(b);

        _active = a;
        _inactive = b;
    }

    private void Start()
    {
        // 타이틀에서 시작
        if (titleBgm != null)
            PlayTitle();
    }

    public void PlayTitle()
    {
        CrossFadeTo(titleBgm);
    }

    public void PlayLobby()
    {
        CrossFadeTo(lobbyBgm);
    }

    public void StopAll(float fade = -1f)
    {
        float t = fade >= 0f ? fade : fadeSeconds;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Co_FadeOutStop(t));
    }

    private void CrossFadeTo(AudioClip clip)
    {
        if (clip == null) return;

        // 같은 클립이면 스킵
        if (_active.isPlaying && _active.clip == clip) return;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Co_CrossFade(clip, fadeSeconds));
    }

    private IEnumerator Co_CrossFade(AudioClip clip, float t)
    {
        _inactive.clip = clip;
        _inactive.volume = 0f;
        _inactive.loop = true;
        _inactive.Play();

        float startActiveVol = _active.isPlaying ? _active.volume : 0f;
        float target = masterVolume;

        if (t <= 0f)
        {
            _active.volume = 0f;
            _active.Stop();
            _inactive.volume = target;
            Swap();
            yield break;
        }

        float e = 0f;
        while (e < t)
        {
            e += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(e / t);
            _active.volume = Mathf.Lerp(startActiveVol, 0f, u);
            _inactive.volume = Mathf.Lerp(0f, target, u);
            yield return null;
        }

        _active.volume = 0f;
        _active.Stop();
        _inactive.volume = target;
        Swap();
    }

    private IEnumerator Co_FadeOutStop(float t)
    {
        if (!_active.isPlaying) yield break;

        float start = _active.volume;
        if (t <= 0f)
        {
            _active.Stop();
            _active.volume = 0f;
            yield break;
        }

        float e = 0f;
        while (e < t)
        {
            e += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(e / t);
            _active.volume = Mathf.Lerp(start, 0f, u);
            yield return null;
        }

        _active.Stop();
        _active.volume = 0f;
    }

    private void Swap()
    {
        var tmp = _active;
        _active = _inactive;
        _inactive = tmp;
    }

    private static void Setup2D(AudioSource s)
    {
        s.playOnAwake = false;
        s.loop = true;
        s.spatialBlend = 0f; // 2D
        s.spatialize = false;
    }
}

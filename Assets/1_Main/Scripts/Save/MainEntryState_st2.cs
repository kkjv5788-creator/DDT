using UnityEngine;

public class MainEntryState_st2 : MonoBehaviour
{
    public static MainEntryState_st2 Instance { get; private set; }

    public bool HasEnteredMainOnce { get; private set; }
    public bool SpawnLobbyNext { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>스테이지(게임)에서 메인으로 돌아가기 직전에 호출</summary>
    public void MarkReturnToMain()
    {
        SpawnLobbyNext = true;
    }

    /// <summary>Main에서 스폰 처리 후 플래그 소비</summary>
    public void ConsumeAfterSpawn()
    {
        HasEnteredMainOnce = true;
        SpawnLobbyNext = false;
    }

    public void ForceTitleNext()
    {
        SpawnLobbyNext = false;
        HasEnteredMainOnce = false;
    }
}

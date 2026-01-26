using UnityEngine;

public class MainSpawnRouter_st2 : MonoBehaviour
{
    [Header("Spawn Points (Main Scene)")]
    [SerializeField] private Transform pointTitle;   // Point_Title
    [SerializeField] private Transform pointLobby;   // Point_Lobby

    [Header("Optional")]
    [SerializeField] private bool handleCharacterController = true;

    private void Reset()
    {
        var t = GameObject.Find("Point_Title");
        if (t) pointTitle = t.transform;

        var l = GameObject.Find("Point_Lobby");
        if (l) pointLobby = l.transform;
    }

    private void Start()
    {
        var state = MainEntryState_st2.Instance;

        // 상태 매니저가 없으면 Title 우선
        if (state == null)
        {
            Teleport(pointTitle ? pointTitle : pointLobby);
            return;
        }

        bool firstTime = !state.HasEnteredMainOnce;
        bool returnFromStage = state.SpawnLobbyNext;

        Transform target = (firstTime && !returnFromStage) ? pointTitle : pointLobby;
        Teleport(target);

        state.ConsumeAfterSpawn();
    }

    private void Teleport(Transform target)
    {
        if (!target) return;

        CharacterController cc = null;
        if (handleCharacterController) cc = GetComponent<CharacterController>();

        if (cc) cc.enabled = false;
        transform.SetPositionAndRotation(target.position, target.rotation);
        if (cc) cc.enabled = true;
    }
}

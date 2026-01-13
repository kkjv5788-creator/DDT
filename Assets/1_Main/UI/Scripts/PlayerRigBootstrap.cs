using UnityEngine;

public class PlayerRigBootstrap : MonoBehaviour
{
    [Tooltip("XR_PlayerRig prefab")]
    public GameObject xrPlayerRigPrefab;

    private static bool _spawned;

    private void Awake()
    {
        if (_spawned)
        {
            Destroy(gameObject);
            return;
        }

        _spawned = true;
        DontDestroyOnLoad(gameObject);

        EnsurePlayerRig();
    }

    private void EnsurePlayerRig()
    {
        // 이미 존재하면 새로 만들지 않음
        if (FindObjectOfType<OVRCameraRig>() != null)
            return;

        if (xrPlayerRigPrefab == null)
        {
            Debug.LogError("[PlayerRigBootstrap] XR_PlayerRig prefab not assigned.");
            return;
        }

        var rig = Instantiate(xrPlayerRigPrefab);
        DontDestroyOnLoad(rig);
    }
}

using UnityEngine;

public class HandToolSwitcher : MonoBehaviour
{
    public GameObject controllerRoot; // OVRControllerPrefab (UI용)
    public GameObject knifeRoot;      // KnifePhysicalRoot (게임용)

    public void SwitchToKnife()
    {
        if (controllerRoot) controllerRoot.SetActive(false);
        if (knifeRoot) knifeRoot.SetActive(true);
    }

    public void SwitchToController()
    {
        if (controllerRoot) controllerRoot.SetActive(true);
        if (knifeRoot) knifeRoot.SetActive(false);
    }
}
using System.Diagnostics;
using UnityEngine;

public class TriggerProbe : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        UnityEngine.Debug.Log($"[TriggerProbe] ENTER by {other.name} layer={LayerMask.LayerToName(other.gameObject.layer)}");
    }
}

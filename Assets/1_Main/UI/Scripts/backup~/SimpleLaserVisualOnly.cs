using UnityEngine;

[DisallowMultipleComponent]
public class SimpleLaserVisualOnly : MonoBehaviour
{
    public Transform rayOrigin;          // left_laser_begin (또는 left_ray_origin)
    public LaserLineDriver laser;
    public float length = 20f;

    private void Awake()
    {
        if (!laser) laser = GetComponent<LaserLineDriver>();
    }

    private void Update()
    {
        if (!laser || !rayOrigin) return;
        laser.Apply(rayOrigin, length, LaserLineDriver.VisualState.Idle);
    }
}

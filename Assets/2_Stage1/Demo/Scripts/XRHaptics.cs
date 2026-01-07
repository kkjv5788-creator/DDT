using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public static class XRHaptics
{
    public static void SendHaptic(bool rightHand, float amplitude01, float durationSeconds)
    {
        amplitude01 = Mathf.Clamp01(amplitude01);
        durationSeconds = Mathf.Clamp(durationSeconds, 0.01f, 0.25f);

        var desired = rightHand ? InputDeviceCharacteristics.Right : InputDeviceCharacteristics.Left;
        desired |= InputDeviceCharacteristics.Controller;

        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(desired, devices);

        foreach (var d in devices)
        {
            if (!d.isValid) continue;
            if (d.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
            {
                // channel 0 is common
                d.SendHapticImpulse(0u, amplitude01, durationSeconds);
                return;
            }
        }
        // Simulator/Editor may not support haptics; that's okay.
    }
}

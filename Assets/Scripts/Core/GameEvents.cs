using System;
using UnityEngine;

namespace StealthHeist.Core
{
    /// <summary>
    /// A static class to manage global game events.
    /// This helps decouple systems, e.g., a camera doesn't need a direct reference to the UI manager.
    /// A UI script can subscribe to OnSecurityAlert to display a visual warning to the player.
    /// </summary>
    public static class GameEvents
    {
        // Event triggered when a security system (like a camera) raises an alarm.
        // The Vector3 is the location of the event (e.g., player's last seen position).
        public static event Action<Vector3> OnSecurityAlert;

        // Event triggered when the player is caught by a guard.
        public static event Action OnPlayerCaught;

        public static void TriggerSecurityAlert(Vector3 location)
        {
            OnSecurityAlert?.Invoke(location);
        }

        public static void TriggerPlayerCaught()
        {
            OnPlayerCaught?.Invoke();
        }

    }
}

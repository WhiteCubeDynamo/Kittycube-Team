using System;
using UnityEngine;
using StealthHeist.Inventory;

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

        // Event triggered when an item is added to the inventory.
        public static event Action<InventoryItem> OnItemAddedToInventory;

        // Event triggered when an item is removed from the inventory.
        public static event Action<string, int> OnItemRemovedFromInventory;

        public static void TriggerSecurityAlert(Vector3 location)
        {
            OnSecurityAlert?.Invoke(location);
        }

        public static void TriggerPlayerCaught()
        {
            OnPlayerCaught?.Invoke();
        }

        public static void TriggerItemAdded(InventoryItem item)
        {
            OnItemAddedToInventory?.Invoke(item);
        }

        public static void TriggerItemRemoved(string itemName, int quantity)
        {
            OnItemRemovedFromInventory?.Invoke(itemName, quantity);
        }
    }
}
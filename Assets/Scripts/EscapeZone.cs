using UnityEngine;
using StealthHeist.Core;
using StealthHeist.Player;

namespace StealthHeist.Environment
{
    /// <summary>
    /// A trigger volume that the player enters to attempt to win the game.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EscapeZone : MonoBehaviour
    {
        public GameLoopManager gameLoopManager;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PhysicalPlayerController player = other.GetComponent<PhysicalPlayerController>();
                
                // Check if player is carrying an item
                if (player.collectedItem == null)
                {
                    Debug.Log("Cannot escape: No item being carried");
                    return;
                }
                
                // Check stealth state before allowing escape
                PlayerStealth playerStealth = player.GetComponent<PlayerStealth>();
                if (playerStealth != null && playerStealth.VisibilityLevel > 1.1f)
                {
                    Debug.Log("Cannot escape: Player too visible");
                    return;
                }
                
                // Attempt escape with the carried item
                string carriedItemName = player.collectedItem.name;
                
                if (gameLoopManager.AttemptEscape(carriedItemName))
                {
                    player.DetachItem();
                    Debug.Log($"Successfully escaped with {carriedItemName}!");
                }
                else
                {
                    Debug.Log($"Escape attempt failed with {carriedItemName}");
                }
            }
        }
    }
}

using UnityEngine;
using StealthHeist.Core;

namespace StealthHeist.Environment
{
    /// <summary>
    /// A trigger volume that the player enters to attempt to win the game.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EscapeZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                GameLoopManager.Instance?.AttemptEscape();
            }
        }
    }
}
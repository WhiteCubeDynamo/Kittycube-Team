using UnityEngine;
using StealthHeist.Core.Interfaces;
using System.Collections;

namespace StealthHeist.Environment
{
    /// <summary>
    /// An interactable panel that can disable one or more security cameras
    /// for a set duration or for the remainder of the current game loop.
    /// </summary>
    public class SecurityPanel : MonoBehaviour, IInteractable
    {
        [Header("Panel Settings")]
        [Tooltip("The cameras that this panel will disable.")]
        [SerializeField] private SecurityCamera[] _camerasToControl;

        [Tooltip("If true, disables cameras for the rest of the loop. If false, uses the duration below.")]
        [SerializeField] private bool _isPermanentForLoop = false;

        [Tooltip("Duration in seconds to disable cameras if not permanent for the loop.")]
        [SerializeField] private float _disableDuration = 30f;

        [Header("Interaction Text")]
        [SerializeField] private string _readyText = "Use Security Panel";
        [SerializeField] private string _cooldownText = "Panel on Cooldown";

        private bool _isOnCooldown = false;

        #region IInteractable Implementation

        public string InteractionText => _isOnCooldown ? _cooldownText : _readyText;
        public bool CanInteract => !_isOnCooldown;

        public void Interact()
        {
            if (!CanInteract) return;

            Debug.Log($"Security Panel used. Disabling {_camerasToControl.Length} cameras.");

            foreach (var camera in _camerasToControl)
            {
                if (camera == null) continue;

                if (_isPermanentForLoop)
                {
                    camera.DisableForLoop();
                }
                else
                {
                    camera.DisableForDuration(_disableDuration);
                }
            }

            // The panel becomes unusable after one use.
            // If temporary, it will reset after the cooldown.
            _isOnCooldown = true;

            if (!_isPermanentForLoop)
            {
                StartCoroutine(CooldownRoutine());
            }
        }

        public void OnHighlight() { /* Optional: Show an outline or UI element */ }
        public void OnUnhighlight() { /* Optional: Hide outline or UI element */ }

        #endregion

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(_disableDuration);
            _isOnCooldown = false;
        }
    }
}
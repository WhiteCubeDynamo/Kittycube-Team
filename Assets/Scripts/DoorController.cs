using UnityEngine;
using System.Collections;
using StealthHeist.Core.Interfaces;
using StealthHeist.Inventory;

namespace StealthHeist.Environment
{
    /// <summary>
    /// Controls the behavior of an interactable door, including opening, closing, and locking.
    /// </summary>
    public class DoorController : MonoBehaviour, IInteractable, IPersistent
    {
        private enum DoorState { Closed, Opening, Open, Closing }

        [Header("Persistence")]
        [SerializeField] private string _persistenceID;

        [Header("Door Components")]
        [Tooltip("The part of the door that will rotate on its pivot.")]
        [SerializeField] private Transform _doorPivot;

        [Header("Door Animation")]
        [Tooltip("The angle the door will open to.")]
        [SerializeField] private float _openAngle = 90f;
        [Tooltip("How fast the door opens and closes.")]
        [SerializeField] private float _animationSpeed = 2f;

        [Header("Locking")]
        [Tooltip("Is the door locked at the start?")]
        [SerializeField] private bool _isLocked = false;
        [Tooltip("The name of the item in the inventory required to unlock this door. Leave empty for no key.")]
        [SerializeField] private string _requiredKeyName = "";

        [Header("Interaction Text")]
        [SerializeField] private string _openText = "Open";
        [SerializeField] private string _closeText = "Close";
        [SerializeField] private string _lockedText = "Locked";
        [SerializeField] private string _unlockText = "Unlock";

        private DoorState _currentState = DoorState.Closed;
        private Quaternion _closedRotation;
        private Quaternion _openRotation;

        #region IInteractable Implementation

        public string InteractionText
        {
            get
            {
                if (_isLocked)
                {
                    return HasKey() ? _unlockText : _lockedText;
                }

                switch (_currentState)
                {
                    case DoorState.Closed:
                        return _openText;
                    case DoorState.Open:
                        return _closeText;
                    default:
                        return ""; // Not interactable while moving
                }
            }
        }

        public bool CanInteract => _currentState == DoorState.Open || _currentState == DoorState.Closed || _isLocked;

        public void Interact()
        {
            if (!CanInteract) return;

            if (_isLocked)
            {
                if (HasKey())
                {
                    _isLocked = false;
                    Debug.Log("Door unlocked!");
                    // Optional: Consume the key
                    InventoryManager.Instance?.RemoveItem(_requiredKeyName);
                }
                else
                {
                    Debug.Log("Door is locked. You need the right key.");
                    // You could play a "locked" sound effect here.
                }
                return;
            }

            if (_currentState == DoorState.Closed)
            {
                StartCoroutine(AnimateDoor(true)); // Open the door
            }
            else if (_currentState == DoorState.Open)
            {
                StartCoroutine(AnimateDoor(false)); // Close the door
            }
        }

        public void OnHighlight() { /* Optional: Show an outline on the door */ }
        public void OnUnhighlight() { /* Optional: Hide the outline */ }

        #endregion

        private void Awake()
        {
            if (_doorPivot == null)
                _doorPivot = transform;

            _closedRotation = _doorPivot.rotation;
            _openRotation = _closedRotation * Quaternion.Euler(0, _openAngle, 0);
        }

        private bool HasKey()
        {
            if (string.IsNullOrEmpty(_requiredKeyName))
                return true; // No key required

            return InventoryManager.Instance != null && InventoryManager.Instance.HasItem(_requiredKeyName);
        }

        private IEnumerator AnimateDoor(bool open)
        {
            _currentState = open ? DoorState.Opening : DoorState.Closing;

            Quaternion startRotation = _doorPivot.rotation;
            Quaternion endRotation = open ? _openRotation : _closedRotation;

            float time = 0f;
            while (time < 1f)
            {
                _doorPivot.rotation = Quaternion.Slerp(startRotation, endRotation, time);
                time += Time.deltaTime * _animationSpeed;
                yield return null;
            }

            _doorPivot.rotation = endRotation; // Ensure it ends at the exact rotation
            _currentState = open ? DoorState.Open : DoorState.Closed;
        }

        #region IPersistent Implementation

        public string PersistenceID => _persistenceID;

        public object CaptureState()
        {
            return new DoorPersistentState
            {
                isLocked = _isLocked,
                doorState = _currentState
            };
        }

        public void RestoreState(object state)
        {
            if (state is DoorPersistentState doorState)
            {
                _isLocked = doorState.isLocked;
                _currentState = doorState.doorState;

                // Update the door's visual position based on its state
                if (_currentState == DoorState.Open)
                {
                    _doorPivot.rotation = _openRotation;
                }
                else if (_currentState == DoorState.Closed)
                {
                    _doorPivot.rotation = _closedRotation;
                }
            }
        }

        [System.Serializable]
        private struct DoorPersistentState
        {
            public bool isLocked;
            public DoorState doorState;
        }

        #endregion
    }
}

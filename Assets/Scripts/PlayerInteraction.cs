using UnityEngine;
using UnityEngine.InputSystem;
using StealthHeist.Core.Interfaces;

namespace StealthHeist.Player
{
    /// <summary>
    /// Handles player interaction with IInteractable objects in the world.
    /// Detects interactable objects via raycasting and displays a UI prompt.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float _interactionDistance = 3f;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private LayerMask _interactionLayer;
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private string _interactKeyDisplay = "E";

        private PlayerInput _playerInput;
        private InputAction _interactAction;

        private IInteractable _currentInteractable;
        private IInteractable _lastInteractable;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _interactAction = _playerInput.actions[_interactActionName];

            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main.transform;
                if (_cameraTransform == null)
                {
                    Debug.LogError("PlayerInteraction: No camera found. Please assign _cameraTransform.", this);
                }
            }
        }

        private void OnEnable()
        {
            if (_interactAction != null)
            {
                _interactAction.performed += OnInteractPerformed;
            }
        }

        private void OnDisable()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
            }
            // Ensure prompt is hidden when this component is disabled
            if (_lastInteractable != null)
            {
                _lastInteractable.OnUnhighlight();
                DialogueUIController.Instance?.HideInteractionPrompt();
            }
        }

        private void Update()
        {
            HandleInteractionDetection();
        }

        private void HandleInteractionDetection()
        {
            if (_cameraTransform == null) return;

            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
            _currentInteractable = null;
            Vector3 hitPoint = Vector3.zero;

            if (Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _interactionLayer))
            {
                if (hit.collider.TryGetComponent(out IInteractable interactable))
                {
                    _currentInteractable = interactable;
                    hitPoint = hit.point;
                }
            }

            if (_currentInteractable != _lastInteractable)
            {
                // Unhighlight the previous interactable
                _lastInteractable?.OnUnhighlight();

                // Highlight the new one if it's valid
                _currentInteractable?.OnHighlight();
                
                _lastInteractable = _currentInteractable;
            }

            // Update the UI prompt every frame to reflect state changes (e.g., cooldowns)
            if (_currentInteractable != null && _currentInteractable.CanInteract)
            {
                string promptText = $"[{_interactKeyDisplay}] {_currentInteractable.InteractionText}";
                DialogueUIController.Instance?.ShowInteractionPrompt(hitPoint, promptText);
            }
            else
            {
                // Hide the prompt if we are looking away or at something not currently interactable
                DialogueUIController.Instance?.HideInteractionPrompt();
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (_currentInteractable != null && _currentInteractable.CanInteract)
            {
                _currentInteractable.Interact();
            }
        }
    }
}
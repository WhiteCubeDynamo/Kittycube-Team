using UnityEngine;
using UnityEngine.UI;
using StealthHeist.Core.Interfaces;
using TMPro;

/// <summary>
/// Enhanced player interaction system that handles interaction detection,
/// highlighting, and UI prompts.
/// </summary>
public class PlayerInteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float _interactionRange = 2f;
    [SerializeField] private float _interactionCheckInterval = 0.1f;
    [SerializeField] private LayerMask _interactableLayer = -1;
    
    [Header("UI References")]
    [SerializeField] private GameObject _interactionPrompt;
    [SerializeField] private TextMeshProUGUI _interactionText;
    [SerializeField] private Image _interactionIcon;
    
    [Header("Detection")]
    [SerializeField] private Transform _interactionOrigin;
    [SerializeField] private bool _useRaycast = true;
    [SerializeField] private bool _useSphereCheck = false;
    [SerializeField] private float _sphereRadius = 0.5f;
    
    private IInteractable _currentInteractable;
    private GameObject _currentInteractableObject;
    private float _nextCheckTime;
    private Camera _playerCamera;
    
    private void Start()
    {
        if (_interactionOrigin == null)
        {
            _interactionOrigin = transform;
        }
        
        _playerCamera = Camera.main;
        
        if (_interactionPrompt != null)
        {
            _interactionPrompt.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (Time.time >= _nextCheckTime)
        {
            CheckForInteractable();
            _nextCheckTime = Time.time + _interactionCheckInterval;
        }
        
        UpdateInteractionUI();
    }
    
    private void CheckForInteractable()
    {
        IInteractable newInteractable = null;
        GameObject newInteractableObject = null;
        
        if (_useRaycast)
        {
            // Raycast from camera center
            Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _interactionRange, _interactableLayer))
            {
                newInteractable = hit.collider.GetComponent<IInteractable>();
                if (newInteractable != null)
                {
                    newInteractableObject = hit.collider.gameObject;
                }
            }
        }
        
        if (_useSphereCheck && newInteractable == null)
        {
            // Sphere check as fallback or alternative
            Collider[] colliders = Physics.OverlapSphere(_interactionOrigin.position, _sphereRadius, _interactableLayer);
            
            float closestDistance = float.MaxValue;
            foreach (Collider col in colliders)
            {
                float distance = Vector3.Distance(_interactionOrigin.position, col.transform.position);
                if (distance < closestDistance)
                {
                    IInteractable interactable = col.GetComponent<IInteractable>();
                    if (interactable != null && interactable.CanInteract)
                    {
                        closestDistance = distance;
                        newInteractable = interactable;
                        newInteractableObject = col.gameObject;
                    }
                }
            }
        }
        
        // Handle interactable changes
        if (newInteractable != _currentInteractable)
        {
            // Unhighlight previous
            if (_currentInteractable != null)
            {
                _currentInteractable.OnUnhighlight();
            }
            
            // Highlight new
            if (newInteractable != null && newInteractable.CanInteract)
            {
                newInteractable.OnHighlight();
                _currentInteractable = newInteractable;
                _currentInteractableObject = newInteractableObject;
            }
            else
            {
                _currentInteractable = null;
                _currentInteractableObject = null;
            }
        }
    }
    
    private void UpdateInteractionUI()
    {
        if (_interactionPrompt == null) return;
        
        if (_currentInteractable != null && _currentInteractable.CanInteract)
        {
            // Show prompt
            if (!_interactionPrompt.activeSelf)
            {
                _interactionPrompt.SetActive(true);
            }
            
            // Update text
            if (_interactionText != null)
            {
                _interactionText.text = _currentInteractable.InteractionText;
            }
            
            // Position prompt above object (optional)
            if (_currentInteractableObject != null)
            {
                Vector3 screenPos = _playerCamera.WorldToScreenPoint(_currentInteractableObject.transform.position + Vector3.up);
                if (screenPos.z > 0) // Object is in front of camera
                {
                    // You can position the UI element here if needed
                    // _interactionPrompt.transform.position = screenPos;
                }
            }
        }
        else
        {
            // Hide prompt
            if (_interactionPrompt.activeSelf)
            {
                _interactionPrompt.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Call this method when the interact button is pressed (from PhysicalPlayerController)
    /// </summary>
    public void TryInteract()
    {
        if (_currentInteractable != null && _currentInteractable.CanInteract)
        {
            _currentInteractable.Interact();
        }
    }
    
    /// <summary>
    /// Get the current interactable object
    /// </summary>
    public IInteractable GetCurrentInteractable()
    {
        return _currentInteractable;
    }
    
    /// <summary>
    /// Check if there's an interactable object in range
    /// </summary>
    public bool HasInteractable()
    {
        return _currentInteractable != null && _currentInteractable.CanInteract;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (_interactionOrigin == null) return;
        
        // Draw interaction range
        Gizmos.color = Color.yellow;
        if (_useRaycast && _playerCamera != null)
        {
            Gizmos.DrawRay(_playerCamera.transform.position, _playerCamera.transform.forward * _interactionRange);
        }
        
        if (_useSphereCheck)
        {
            Gizmos.DrawWireSphere(_interactionOrigin.position, _sphereRadius);
        }
    }
}

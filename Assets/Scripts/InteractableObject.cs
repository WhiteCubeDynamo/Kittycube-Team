using UnityEngine;
using StealthHeist.Core.Interfaces;

/// <summary>
/// Basic implementation of an interactable object.
/// Attach this to any GameObject you want to make interactable.
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] public string _interactionText = "Press E to interact";
    [SerializeField] public bool _canInteract = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private Material _highlightMaterial;
    [SerializeField] private Color _highlightColor = Color.yellow;
    
    private Renderer _renderer;
    private Material _originalMaterial;
    private Color _originalColor;
    
    // IInteractable implementation
    public string InteractionText => _interactionText;
    public bool CanInteract => _canInteract;
    


    protected virtual void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material;
            _originalColor = _originalMaterial.color;
        }
    }
    
    public virtual void Interact()
    {
        if (!_canInteract) return;
        
        Debug.Log($"Interacted with {gameObject.name}");
        OnInteract();
    }
    
    public virtual void OnHighlight()
    {
        if (_renderer != null)
        {
            if (_highlightMaterial != null)
            {
                _renderer.material = _highlightMaterial;
            }
            else
            {
                _renderer.material.color = _highlightColor;
            }
        }
        
        OnHighlightStart();
    }
    
    public virtual void OnUnhighlight()
    {
        if (_renderer != null)
        {
            if (_highlightMaterial != null)
            {
                _renderer.material = _originalMaterial;
            }
            else
            {
                _renderer.material.color = _originalColor;
            }
        }
        
        OnHighlightEnd();
    }
    
    // Virtual methods for derived classes to override
    protected virtual void OnInteract()
    {
        // Override this in derived classes for specific interaction behavior
    }
    
    protected virtual void OnHighlightStart()
    {
        // Override this for custom highlight behavior
    }
    
    protected virtual void OnHighlightEnd()
    {
        // Override this for custom unhighlight behavior
    }
    
    // Helper method to enable/disable interaction
    public void SetInteractable(bool interactable)
    {
        _canInteract = interactable;
    }
}

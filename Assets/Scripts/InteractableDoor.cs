using UnityEngine;
using System.Collections;

/// <summary>
/// Example of an interactable door that can be opened and closed.
/// </summary>
public class InteractableDoor : InteractableObject
{
    [Header("Door Settings")]
    [SerializeField] private bool _isOpen = false;
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _openSpeed = 2f;
    [SerializeField] private bool _autoClose = false;
    [SerializeField] private float _autoCloseDelay = 3f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip _openSound;
    [SerializeField] private AudioClip _closeSound;
    
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private AudioSource _audioSource;
    private Coroutine _doorCoroutine;
    
    protected override void Awake()
    {
        base.Awake();
        
        _closedRotation = transform.rotation;
        _openRotation = _closedRotation * Quaternion.Euler(0, _openAngle, 0);
        _audioSource = GetComponent<AudioSource>();
        
        // Update interaction text based on door state
        UpdateInteractionText();
    }
    
    protected override void OnInteract()
    {
        ToggleDoor();
    }
    
    private void ToggleDoor()
    {
        _isOpen = !_isOpen;
        
        if (_doorCoroutine != null)
        {
            StopCoroutine(_doorCoroutine);
        }
        
        _doorCoroutine = StartCoroutine(AnimateDoor(_isOpen));
        
        // Play sound
        if (_audioSource != null)
        {
            AudioClip soundToPlay = _isOpen ? _openSound : _closeSound;
            if (soundToPlay != null)
            {
                _audioSource.PlayOneShot(soundToPlay);
            }
        }
        
        UpdateInteractionText();
        
        // Auto close if enabled
        if (_isOpen && _autoClose)
        {
            StartCoroutine(AutoCloseDoor());
        }
    }
    
    private IEnumerator AnimateDoor(bool open)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = open ? _openRotation : _closedRotation;
        
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * _openSpeed;
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsed);
            yield return null;
        }
        
        transform.rotation = targetRotation;
    }
    
    private IEnumerator AutoCloseDoor()
    {
        yield return new WaitForSeconds(_autoCloseDelay);
        
        if (_isOpen)
        {
            ToggleDoor();
        }
    }
    
    private void UpdateInteractionText()
    {
        string action = _isOpen ? "Close" : "Open";
        _interactionText = $"Press E to {action} door";
    }
    
    // Public method to open/close door programmatically
    public void SetDoorState(bool open)
    {
        if (_isOpen != open)
        {
            ToggleDoor();
        }
    }
}

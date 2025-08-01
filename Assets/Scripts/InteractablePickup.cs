using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Example of an interactable pickup item.
/// </summary>
public class InteractablePickup : InteractableObject
{
    [Header("Pickup Settings")]
    [SerializeField] private string _itemName = "Item";
    [SerializeField] private int _quantity = 1;
    [SerializeField] private bool _destroyOnPickup = true;
    
    [Header("Animation")]
    [SerializeField] private bool _rotateItem = true;
    [SerializeField] private float _rotationSpeed = 30f;
    [SerializeField] private bool _bobItem = true;
    [SerializeField] private float _bobHeight = 0.2f;
    [SerializeField] private float _bobSpeed = 2f;
    
    [Header("Effects")]
    [SerializeField] private GameObject _pickupEffect;
    [SerializeField] private AudioClip _pickupSound;
    
    [Header("Events")]
    public UnityEvent OnPickup;
    public UnityEvent<string, int> OnPickupWithInfo;
    
    private Vector3 _startPosition;
    private AudioSource _audioSource;
    
    protected override void Awake()
    {
        base.Awake();
        _startPosition = transform.position;
        _audioSource = GetComponent<AudioSource>();
        
        // Set interaction text
        _interactionText = $"Press E to pick up {_itemName}";
    }
    
    private void Update()
    {
        // Rotate item
        if (_rotateItem)
        {
            transform.Rotate(Vector3.up * _rotationSpeed * Time.deltaTime);
        }
        
        // Bob item up and down
        if (_bobItem)
        {
            float newY = _startPosition.y + Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    protected override void OnInteract()
    {
        PickupItem();
    }
    
    private void PickupItem()
    {
        Debug.Log($"Picked up {_quantity} {_itemName}(s)");
        
        // Play pickup sound
        if (_pickupSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_pickupSound);
        }
        
        // Spawn pickup effect
        if (_pickupEffect != null)
        {
            GameObject effect = Instantiate(_pickupEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Destroy effect after 2 seconds
        }
        
        // Fire events
        OnPickup?.Invoke();
        OnPickupWithInfo?.Invoke(_itemName, _quantity);
        
        // Add to inventory (you would implement this based on your inventory system)
        AddToInventory();
        
        // Destroy or disable the pickup
        if (_destroyOnPickup)
        {
            // If we have audio, wait for it to finish
            if (_pickupSound != null && _audioSource != null)
            {
                Destroy(gameObject, _pickupSound.length);
                // Hide the object immediately
                GetComponent<Renderer>().enabled = false;
                GetComponent<Collider>().enabled = false;
                _rotateItem = false;
                _bobItem = false;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            SetInteractable(false);
            gameObject.SetActive(false);
        }
    }
    
    private void AddToInventory()
    {
        // Example: Find inventory manager and add item
        // InventoryManager inventory = FindObjectOfType<InventoryManager>();
        // if (inventory != null)
        // {
        //     inventory.AddItem(_itemName, _quantity);
        // }
        
        // For now, just log it
        Debug.Log($"TODO: Add {_quantity} {_itemName}(s) to inventory");
    }
    
    // Public method to set item details
    public void SetItemDetails(string itemName, int quantity)
    {
        _itemName = itemName;
        _quantity = quantity;
        _interactionText = $"Press E to pick up {_itemName}";
    }
}

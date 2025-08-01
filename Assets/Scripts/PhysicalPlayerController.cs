using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using StealthHeist.Player;

[RequireComponent(typeof(Rigidbody), typeof(PlayerStealth))]
public class PhysicalPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float crouchSpeed = 1.5f;
    public float rotationSpeed = 720f;
    public float jumpForce = 25f;
    public float maxSpeed = 10f;
    public float friction = 10f;
    public AnimationCurve accelerationCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundDistance = 1f;
    public LayerMask groundMask;
    
    [Header("Throwing")]
    [SerializeField] private GameObject _throwablePrefab;
    [SerializeField] private float _throwForce = 10f;
    [SerializeField] private float _throwCooldown = 0.5f;
    
    [Header("Interaction")]
    public Transform collectedItem;
    public Transform hand;
    private int itemsStolen;
    public float interactRange = 2f;
    [SerializeField] private LayerMask itemMask;
    
    [Header("Physics")]
    public float gravityScale = 2f;

    private Rigidbody rb;
    private Transform cameraTransform;
    private InputSystem_Actions inputActions;
    private PlayerStealth _playerStealth;
    private PlayerAnimationController _animationController;
    private bool _isCrouching = false;
    private bool _isRunning = false;
    private bool isGrounded;
    private float _lastThrowTime = -999f;
    
    // Public properties for animation controller
    public bool IsCrouching => _isCrouching;
    public bool IsRunning => _isRunning;

    [SerializeField] private GameLoopManager gameLoopManager;

    void Awake()
    {
        cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        _playerStealth = GetComponent<PlayerStealth>();
        _animationController = GetComponent<PlayerAnimationController>();

        // Configure rigidbody to prevent flying and unwanted rotation
        rb.freezeRotation = true;
        rb.linearDamping = 5f;
        rb.angularDamping = 10f;

        inputActions = new InputSystem_Actions();
        inputActions.Player.Move.Enable();
        inputActions.Player.Run.Enable();
        inputActions.Player.Crouch.Enable();
        inputActions.Player.Throw.Enable();
        inputActions.Player.Interact.Enable();

        inputActions.Player.Crouch.performed += _ => ToggleCrouch();
        inputActions.Player.Throw.performed += _ => TryThrowObject();
        inputActions.Player.Interact.performed += OnInteractPerformed;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        inputActions.Player.Crouch.performed -= _ => ToggleCrouch();
        inputActions.Player.Throw.performed -= _ => TryThrowObject();
        inputActions.Player.Interact.performed -= OnInteractPerformed;
    }
    
    void Update()
    {
        ProcessJump();
    }
    
    private void ProcessJump()
    {
        // Check if player is grounded
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        bool runningPressed = inputActions.Player.Run.IsPressed();
        _isRunning = runningPressed;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        forward.Normalize();
        Vector3 right = cameraTransform.right;
        right.y = 0;
        right.Normalize();

        Vector3 moveDir = forward * input.y + right * input.x;

        float speed = _isCrouching ? crouchSpeed : (runningPressed ? moveSpeed * 1.5f : moveSpeed);

        Vector3 horizontalVel = moveDir * speed;
        Vector3 newVel = horizontalVel;
        newVel.y = rb.linearVelocity.y;
        rb.linearVelocity = newVel;

        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

        // Update animation parameters with 2D coordinates
        UpdateAnimationParameters(input, runningPressed);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            Quaternion smoothRot = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothRot);
        }
    }   //
    
    private void UpdateAnimationParameters(Vector2 input, bool isRunning)
    {
        // if (_animationController == null) return;
        
        // Calculate movement coordinates for 2D blend tree
        // X = horizontal movement (strafe left/right)
        // Y = vertical movement (forward/backward)
        float moveX = input.x;
        float moveZ = input.y;
        
        // Apply speed multiplier based on state
        float speedMultiplier = 1f;
        if (_isCrouching)
        {
            speedMultiplier = 0.3f; // Slower animation for crouching
        }
        else if (isRunning)
        {
            speedMultiplier = 1.5f; // Faster animation for running
        }
        
        moveX *= speedMultiplier;
        moveZ *= speedMultiplier;
        
        // Set animation parameters
        _animationController.SetFloatParameter("MoveX", moveX);
        _animationController.SetFloatParameter("MoveZ", moveZ);
        
        // Optional: Set overall speed for blend trees that might still use it
        float overallSpeed = new Vector2(moveX, moveZ).magnitude;
        _animationController.SetFloatParameter("Speed", overallSpeed);
        
        // Set boolean states
        bool isMoving = overallSpeed > 0.1f;
        _animationController.SetBoolParameter("IsMoving", isMoving);
        _animationController.SetBoolParameter("IsCrouching", _isCrouching);
        _animationController.SetBoolParameter("IsRunning", _isRunning);
    }

    private void ToggleCrouch()
    {
        _isCrouching = !_isCrouching;
        _playerStealth.SetCrouching(_isCrouching);
    }

    private void TryThrowObject()
    {
        if (_throwablePrefab == null || Time.time < _lastThrowTime + _throwCooldown)
            return;

        _lastThrowTime = Time.time;

        if (_animationController != null)
            _animationController.TriggerThrowAnimation();

        Vector3 spawnPos = transform.position + transform.forward + Vector3.up * 0.5f;
        GameObject thrownObject = Instantiate(_throwablePrefab, spawnPos, transform.rotation);
        thrownObject.GetComponent<Rigidbody>()?.AddForce(transform.forward * _throwForce, ForceMode.VelocityChange);
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Interact performed");
        if (_animationController != null)
            _animationController.TriggerInteractAnimation();

        TryInteract();
    }

    private void TryInteract()
    {
        // If already carrying an item, don't try to pick up another
        if (collectedItem != null)
        {
            Debug.Log("Already carrying an item");
            return;
        }

        Vector3 origin = transform.position + Vector3.up * 0.5f; // Start from chest height
        Vector3 direction = transform.forward; // Use player's forward direction
        
        Debug.Log($"Trying spherecast from {origin} in direction {direction} with range {interactRange}");
        
        // Try pickup item using spherecast
        if (Physics.SphereCast(origin, 0.3f, direction, out RaycastHit hit, interactRange, itemMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"SphereCast hit: {hitObject.name} at distance {hit.distance}");
            
            // Check if this object matches any of our collectible names
            foreach (string collectibleName in gameLoopManager.StealableItems)
            {
                Debug.Log($"Checking {hitObject.name} against {collectibleName}");
                if (hitObject.name.Contains(collectibleName))
                {
                    // Add to collected items list
                    AttachItem(hitObject);
                    Debug.Log($"Collected: {hitObject.name}. Total items: {itemsStolen}");
                    return;
                }
            }
        }
        else
        {
            Debug.Log("SphereCast hit nothing");
        }
        
        // Alternative: try simple overlap sphere as backup
        Collider[] colliders = Physics.OverlapSphere(origin, interactRange, itemMask);
        if (colliders.Length > 0)
        {
            Debug.Log($"OverlapSphere found {colliders.Length} colliders");
            foreach (Collider col in colliders)
            {
                foreach (string collectibleName in gameLoopManager.StealableItems)
                {
                    if (col.gameObject.name.Contains(collectibleName))
                    {
                        AttachItem(col.gameObject);
                        Debug.Log($"Collected via OverlapSphere: {col.gameObject.name}");
                        return;
                    }
                }
            }
        }
        
        // Now interactables process input on their own
    }
    // When player picks up item
    public void AttachItem(GameObject item)
    {
        if(collectedItem == null) {
            collectedItem = item.transform;
            collectedItem.parent = hand;
            collectedItem.localPosition = Vector3.zero;
            collectedItem.localRotation = Quaternion.identity;
            
            // Disable the item's physics while carried
            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.isKinematic = true;
            }
            
            Debug.Log($"Successfully attached {item.name} to hand");
        } else {
            Debug.Log("Already carrying an item");
        }
    }

    public void DetachItem()
    {
        if(collectedItem != null) {
            // Re-enable physics
            Rigidbody itemRb = collectedItem.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.isKinematic = false;
            }
            
            collectedItem.parent = null;
            itemsStolen++;
            collectedItem = null;
            Debug.Log($"Detached item. Total stolen: {itemsStolen}");
        } else {
            Debug.Log("No item to drop");
        }
    }
    
    // Public method to check if player has a specific item
    public bool HasItem(string itemName)
    {
        return collectedItem != null && collectedItem.name.Contains(itemName);
    }
    
    // Public method to get all collected items
    public List<string> GetCollectedItems()
    {
        return new List<string>(gameLoopManager.CollectedItems);
    }
}

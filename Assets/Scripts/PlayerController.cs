using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthHeist.Player
{
    /// <summary>
    /// Handles player movement, camera look, and actions like running and crouching.
    /// Communicates with PlayerStealth to update stealth-related states.
    /// Requires the new Input System package and a PlayerInput component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerStealth), typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3.0f;
        [SerializeField] private float _runSpeed = 6.0f;
        [SerializeField] private float _crouchSpeed = 1.5f;
        [SerializeField] private float _jumpHeight = 1.0f;
        [SerializeField] private float _gravity = -9.81f;

        [Header("Look Settings")]
        [SerializeField] private Transform _playerCamera;
        [SerializeField] private float _lookSpeed = 2.0f;
        [SerializeField] private float _lookXLimit = 80.0f;

        [Header("Crouch Settings")]
        [SerializeField] private float _standingHeight = 2.0f;
        [SerializeField] private float _crouchingHeight = 1.0f;

        [Header("Throwing")]
        [SerializeField] private GameObject _throwablePrefab;
        [SerializeField] private float _throwForce = 15f;
        [SerializeField] private float _throwCooldown = 1.0f;

        // --- Component References ---
        private CharacterController _characterController;
        private PlayerStealth _playerStealth;

        // --- Input & State ---
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private InputAction _crouchAction;
        private InputAction _throwAction;

        private Vector3 _playerVelocity;
        private bool _isGrounded;
        private float _rotationX = 0;
        private bool _isRunning = false;
        private bool _isCrouching = false;
        private float _lastThrowTime = -999f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerStealth = GetComponent<PlayerStealth>();
            _playerInput = GetComponent<PlayerInput>();

            // Find actions from the PlayerInput component
            _moveAction = _playerInput.actions["Move"];
            _lookAction = _playerInput.actions["Look"];
            _jumpAction = _playerInput.actions["Jump"];
            _runAction = _playerInput.actions["Run"];
            _crouchAction = _playerInput.actions["Crouch"];
            _throwAction = _playerInput.actions["Throw"];

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            _crouchAction.performed += _ => ToggleCrouch();
            _throwAction.performed += _ => TryThrowObject();
        }

        private void OnDisable()
        {
            _crouchAction.performed -= _ => ToggleCrouch();
            _throwAction.performed -= _ => TryThrowObject();
        }

        void Update()
        {
            HandleMovement();
            HandleLook();
        }

        private void HandleMovement()
        {
            _isGrounded = _characterController.isGrounded;
            if (_isGrounded && _playerVelocity.y < 0)
            {
                _playerVelocity.y = -2f; // A small downward force to keep the controller grounded
            }

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            _isRunning = _runAction.IsPressed();

            float currentSpeed = _isCrouching ? _crouchSpeed : (_isRunning ? _runSpeed : _walkSpeed);

            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            _characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

            if (_jumpAction.triggered && _isGrounded && !_isCrouching)
            {
                _playerVelocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }

            _playerVelocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_playerVelocity * Time.deltaTime);
        }

        private void HandleLook()
        {
            if (_playerCamera == null) return;

            Vector2 lookInput = _lookAction.ReadValue<Vector2>();
            float lookX = lookInput.x * _lookSpeed * Time.deltaTime;
            float lookY = lookInput.y * _lookSpeed * Time.deltaTime;

            _rotationX -= lookY;
            _rotationX = Mathf.Clamp(_rotationX, -_lookXLimit, _lookXLimit);

            _playerCamera.localRotation = Quaternion.Euler(_rotationX, 0, 0);
            transform.Rotate(Vector3.up * lookX);
        }

        private void ToggleCrouch()
        {
            _isCrouching = !_isCrouching;
            _playerStealth.SetCrouching(_isCrouching);
            _characterController.height = _isCrouching ? _crouchingHeight : _standingHeight;
        }

        private void TryThrowObject()
        {
            if (_throwablePrefab == null)
            {
                Debug.LogWarning("Throwable Prefab not assigned in PlayerController.");
                return;
            }

            if (Time.time < _lastThrowTime + _throwCooldown) return;

            _lastThrowTime = Time.time;

            // Instantiate at a point in front of the camera to avoid self-collision
            Vector3 spawnPosition = _playerCamera.position + _playerCamera.forward;
            GameObject thrownObject = Instantiate(_throwablePrefab, spawnPosition, _playerCamera.rotation);

            thrownObject.GetComponent<Rigidbody>()?.AddForce(_playerCamera.forward * _throwForce, ForceMode.VelocityChange);
        }
    }
}
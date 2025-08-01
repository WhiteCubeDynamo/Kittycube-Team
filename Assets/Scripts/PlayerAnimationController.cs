using UnityEngine;

namespace StealthHeist.Player
{
    /// <summary>
    /// Handles player character animations based on movement states and actions.
    /// This script communicates with the PlayerController to determine animation states.
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string _speedParameterName = "Speed";
        [SerializeField] private string _isMovingParameterName = "IsMoving";
        [SerializeField] private string _isCrouchingParameterName = "IsCrouching";
        [SerializeField] private string _isRunningParameterName = "IsRunning";
        [SerializeField] private string _throwTriggerName = "Throw";
        [SerializeField] private string _interactTriggerName = "Interact";

        [Header("Settings")]
        [SerializeField] private float _smoothTime = 0.1f;
        [SerializeField] private float _movementThreshold = 0.1f;

        // Component References
        [SerializeField] private Animator _animator;
        private Rigidbody _rigidbody;

        // Animation state tracking
        private float _currentSpeed;
        private float _speedVelocity;
        private bool _wasMoving;
        private bool _wasCrouching;
        private bool _wasRunning;

        // Hash IDs for performance
        private int _speedHash;
        private int _isMovingHash;
        private int _isCrouchingHash;
        private int _isRunningHash;
        private int _throwTriggerHash;
        private int _interactTriggerHash;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _rigidbody = GetComponent<Rigidbody>();

            // Cache animator parameter hashes for better performance
            _speedHash = Animator.StringToHash(_speedParameterName);
            _isMovingHash = Animator.StringToHash(_isMovingParameterName);
            _isCrouchingHash = Animator.StringToHash(_isCrouchingParameterName);
            _isRunningHash = Animator.StringToHash(_isRunningParameterName);
            _throwTriggerHash = Animator.StringToHash(_throwTriggerName);
            _interactTriggerHash = Animator.StringToHash(_interactTriggerName);
        }

        private void Update()
        {
            UpdateMovementAnimations();
            UpdateStateAnimations();
        }

        private void UpdateMovementAnimations()
        {
            // Calculate horizontal movement speed
            Vector3 horizontalVelocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            float targetSpeed = horizontalVelocity.magnitude;

            // Smooth the speed value for animation
            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, _smoothTime);

            // Set speed parameter
            _animator.SetFloat(_speedHash, _currentSpeed);

            // Determine if player is moving
            bool isMoving = _currentSpeed > _movementThreshold;
            if (isMoving != _wasMoving)
            {
                _animator.SetBool(_isMovingHash, isMoving);
                _wasMoving = isMoving;
            }
        }

        private void UpdateStateAnimations()
        {
            // if (_playerController == null) return;
            //
            // // Update crouching state
            // bool isCrouching = _playerController._isCrouching;
            // if (isCrouching != _wasCrouching)
            // {
            //     _animator.SetBool(_isCrouchingHash, isCrouching);
            //     _wasCrouching = isCrouching;
            // }
            //
            // // Update running state
            // bool isRunning = _playerController._isRunning;
            // if (isRunning != _wasRunning)
            // {
            //     _animator.SetBool(_isRunningHash, isRunning);
            //     _wasRunning = isRunning;
            // }
        }

        /// <summary>
        /// Triggers the throw animation
        /// </summary>
        public void TriggerThrowAnimation()
        {
            _animator.SetTrigger(_throwTriggerHash);
        }

        /// <summary>
        /// Triggers the interact animation
        /// </summary>
        public void TriggerInteractAnimation()
        {
            _animator.SetTrigger(_interactTriggerHash);
        }

        /// <summary>
        /// Sets a custom animation trigger by name
        /// </summary>
        /// <param name="triggerName">Name of the trigger parameter</param>
        public void TriggerAnimation(string triggerName)
        {
            _animator.SetTrigger(triggerName);
        }

        /// <summary>
        /// Sets a custom boolean parameter
        /// </summary>
        /// <param name="parameterName">Name of the boolean parameter</param>
        /// <param name="value">Value to set</param>
        public void SetBoolParameter(string parameterName, bool value)
        {
            _animator.SetBool(parameterName, value);
        }

        /// <summary>
        /// Sets a custom float parameter
        /// </summary>
        /// <param name="parameterName">Name of the float parameter</param>
        /// <param name="value">Value to set</param>
        public void SetFloatParameter(string parameterName, float value)
        {
            _animator.SetFloat(parameterName, value);
        }
    }
}

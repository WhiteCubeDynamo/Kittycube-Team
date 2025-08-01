using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace StealthHeist.Environment
{
    /// <summary>
    /// A pressure plate that can be triggered by objects with specific tags.
    /// Can be used to open doors or trigger other mechanisms.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [Tooltip("Tags that can trigger this pressure plate")]
        [SerializeField] private string[] triggerTags = { "Player", "ThrowableObject" };
        
        [Tooltip("Minimum weight needed to keep the plate pressed (0 = any weight)")]
        [SerializeField] private float minimumWeight = 0f;
        
        [Tooltip("How long the plate stays active after being triggered (0 = instant deactivation)")]
        [SerializeField] private float holdTime = 0f;
        
        [Header("Visual Feedback")]
        [Tooltip("How far the plate moves down when pressed")]
        [SerializeField] private float pressDepth = 0.1f;
        
        [Tooltip("Speed of the plate animation")]
        [SerializeField] private float animationSpeed = 5f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pressSound;
        [SerializeField] private AudioClip releaseSound;
        
        [Header("Events")]
        [Tooltip("Called when the pressure plate is activated")]
        public UnityEvent OnPlatePressed;
        
        [Tooltip("Called when the pressure plate is deactivated")]
        public UnityEvent OnPlateReleased;

        // Internal state
        private bool _isPressed = false;
        private Vector3 _originalPosition;
        private Vector3 _pressedPosition;
        private List<GameObject> _objectsOnPlate = new List<GameObject>();
        private float _currentWeight = 0f;
        private float _releaseTimer = 0f;
        
        private void Start()
        {
            // Store original position and calculate pressed position
            _originalPosition = transform.position;
            _pressedPosition = _originalPosition - Vector3.up * pressDepth;
            
            // Ensure the collider is set as a trigger
            GetComponent<Collider>().isTrigger = true;
            
            // Setup audio source if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }
        
        private void Update()
        {
            // Handle release timer
            if (_releaseTimer > 0f)
            {
                _releaseTimer -= Time.deltaTime;
                if (_releaseTimer <= 0f && _objectsOnPlate.Count == 0)
                {
                    ReleasePlate();
                }
            }
            
            // Animate the plate position
            Vector3 targetPosition = _isPressed ? _pressedPosition : _originalPosition;
            transform.position = Vector3.Lerp(transform.position, targetPosition, animationSpeed * Time.deltaTime);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (ShouldTrigger(other))
            {
                _objectsOnPlate.Add(other.gameObject);
                UpdateWeight();
                CheckActivation();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (_objectsOnPlate.Contains(other.gameObject))
            {
                _objectsOnPlate.Remove(other.gameObject);
                UpdateWeight();
                CheckDeactivation();
            }
        }
        
        private bool ShouldTrigger(Collider other)
        {
            foreach (string tag in triggerTags)
            {
                if (other.CompareTag(tag))
                    return true;
            }
            return false;
        }
        
        private void UpdateWeight()
        {
            _currentWeight = 0f;
            
            foreach (GameObject obj in _objectsOnPlate)
            {
                if (obj != null)
                {
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        _currentWeight += rb.mass;
                    }
                    else
                    {
                        // If no rigidbody, assume default weight of 1
                        _currentWeight += 1f;
                    }
                }
            }
        }
        
        private void CheckActivation()
        {
            if (!_isPressed && _currentWeight >= minimumWeight && _objectsOnPlate.Count > 0)
            {
                ActivatePlate();
            }
        }
        
        private void CheckDeactivation()
        {
            if (_isPressed && (_currentWeight < minimumWeight || _objectsOnPlate.Count == 0))
            {
                if (holdTime > 0f)
                {
                    _releaseTimer = holdTime;
                }
                else
                {
                    ReleasePlate();
                }
            }
        }
        
        private void ActivatePlate()
        {
            if (_isPressed) return;
            
            _isPressed = true;
            _releaseTimer = 0f;
            
            // Play sound effect
            if (audioSource != null && pressSound != null)
                audioSource.PlayOneShot(pressSound);
            
            // Trigger events
            OnPlatePressed?.Invoke();
            
            Debug.Log($"Pressure plate activated! Weight: {_currentWeight}, Objects: {_objectsOnPlate.Count}");
        }
        
        private void ReleasePlate()
        {
            if (!_isPressed) return;
            
            _isPressed = false;
            
            // Play sound effect
            if (audioSource != null && releaseSound != null)
                audioSource.PlayOneShot(releaseSound);
            
            // Trigger events
            OnPlateReleased?.Invoke();
            
            Debug.Log("Pressure plate released!");
        }
        
        // Public methods for external access
        public bool IsPressed => _isPressed;
        public float CurrentWeight => _currentWeight;
        public int ObjectCount => _objectsOnPlate.Count;
        
        // Force activation/deactivation (useful for scripted events)
        public void ForceActivate()
        {
            ActivatePlate();
        }
        
        public void ForceRelease()
        {
            ReleasePlate();
        }
        
        private void OnDrawGizmos()
        {
            // Draw the trigger area
            Gizmos.color = _isPressed ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(Vector3.zero, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(Vector3.zero, sphere.radius);
            }
            
            // Draw press depth indicator
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.zero, Vector3.down * pressDepth);
        }
    }
}

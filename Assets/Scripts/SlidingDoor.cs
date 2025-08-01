using UnityEngine;
using System.Collections;

namespace StealthHeist.Environment
{
    /// <summary>
    /// A sliding door that can be opened and closed by pressure plates or other triggers.
    /// Supports different slide directions and smooth animations.
    /// </summary>
    public class SlidingDoor : MonoBehaviour
    {
        [Header("Door Settings")]
        [Tooltip("Direction the door slides when opening")]
        [SerializeField] private Vector3 slideDirection = Vector3.up;
        
        [Tooltip("Distance the door slides")]
        [SerializeField] private float slideDistance = 3f;
        
        [Tooltip("Speed of the door animation")]
        [SerializeField] private float slideSpeed = 2f;
        
        [Tooltip("Curve for door animation (optional)")]
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Auto-Close Settings")]
        [Tooltip("Whether the door should automatically close")]
        [SerializeField] private bool autoClose = false;
        
        [Tooltip("Time before auto-closing (if enabled)")]
        [SerializeField] private float autoCloseDelay = 5f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip movingSound; // Looping sound while moving
        
        [Header("Visual Effects")]
        [Tooltip("Particle effect to play when opening")]
        [SerializeField] private ParticleSystem openEffect;
        
        [Tooltip("Particle effect to play when closing")]
        [SerializeField] private ParticleSystem closeEffect;
        
        [Header("Collision")]
        [Tooltip("Should the door block movement when closed?")]
        [SerializeField] private bool blockMovement = true;
        
        [Tooltip("Collider to enable/disable for blocking")]
        [SerializeField] private Collider doorCollider;

        // Internal state
        private bool _isOpen = false;
        private bool _isMoving = false;
        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private Coroutine _slideCoroutine;
        private Coroutine _autoCloseCoroutine;
        
        public bool IsOpen => _isOpen;
        public bool IsMoving => _isMoving;
        
        private void Start()
        {
            // Store the initial position as closed position
            _closedPosition = transform.position;
            _openPosition = _closedPosition + slideDirection.normalized * slideDistance;
            
            // Setup audio source if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            // Setup door collider if not assigned
            if (doorCollider == null)
                doorCollider = GetComponent<Collider>();
            
            // Ensure door starts in closed state
            SetColliderState(!_isOpen);
        }
        
        /// <summary>
        /// Opens the door
        /// </summary>
        public void OpenDoor()
        {
            if (_isOpen || _isMoving) return;
            
            Debug.Log("Opening sliding door");
            
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);
            
            _slideCoroutine = StartCoroutine(SlideDoor(true));
        }
        
        /// <summary>
        /// Closes the door
        /// </summary>
        public void CloseDoor()
        {
            if (!_isOpen || _isMoving) return;
            
            Debug.Log("Closing sliding door");
            
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);
            
            _slideCoroutine = StartCoroutine(SlideDoor(false));
        }
        
        /// <summary>
        /// Toggles the door state
        /// </summary>
        public void ToggleDoor()
        {
            if (_isOpen)
                CloseDoor();
            else
                OpenDoor();
        }
        
        private IEnumerator SlideDoor(bool opening)
        {
            _isMoving = true;
            
            Vector3 startPos = transform.position;
            Vector3 targetPos = opening ? _openPosition : _closedPosition;
            
            // Play sound effects
            AudioClip soundToPlay = opening ? openSound : closeSound;
            if (audioSource != null && soundToPlay != null)
                audioSource.PlayOneShot(soundToPlay);
            
            // Start looping movement sound
            if (audioSource != null && movingSound != null)
            {
                audioSource.clip = movingSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            // Play particle effects
            ParticleSystem effectToPlay = opening ? openEffect : closeEffect;
            if (effectToPlay != null)
                effectToPlay.Play();
            
            // Animate the door
            float elapsedTime = 0f;
            float duration = Vector3.Distance(startPos, targetPos) / slideSpeed;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                
                // Apply animation curve if available
                float curveValue = slideCurve.Evaluate(progress);
                
                transform.position = Vector3.Lerp(startPos, targetPos, curveValue);
                yield return null;
            }
            
            // Ensure final position is exact
            transform.position = targetPos;
            
            // Stop movement sound
            if (audioSource != null && movingSound != null)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
            
            // Update state
            _isOpen = opening;
            _isMoving = false;
            
            // Update collider
            SetColliderState(!_isOpen);
            
            // Handle auto-close
            if (opening && autoClose)
            {
                if (_autoCloseCoroutine != null)
                    StopCoroutine(_autoCloseCoroutine);
                
                _autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
            }
            
            Debug.Log($"Door {(opening ? "opened" : "closed")}");
        }
        
        private IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            CloseDoor();
        }
        
        private void SetColliderState(bool enabled)
        {
            if (doorCollider != null && blockMovement)
            {
                doorCollider.enabled = enabled;
            }
        }
        
        /// <summary>
        /// Method to be called by pressure plates or other triggers
        /// </summary>
        public void OnTriggerActivated()
        {
            OpenDoor();
        }
        
        /// <summary>
        /// Method to be called when trigger is deactivated
        /// </summary>
        public void OnTriggerDeactivated()
        {
            if (!autoClose) // Only close if not auto-closing
                CloseDoor();
        }
        
        private void OnDrawGizmos()
        {
            // Draw the slide path
            Vector3 startPos = Application.isPlaying ? _closedPosition : transform.position;
            Vector3 endPos = startPos + slideDirection.normalized * slideDistance;
            
            // Draw door in closed position
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(startPos, Vector3.one * 0.5f);
            
            // Draw door in open position
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(endPos, Vector3.one * 0.5f);
            
            // Draw slide direction arrow
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startPos, endPos);
            
            // Draw arrowhead
            Vector3 arrowHead = endPos - slideDirection.normalized * 0.3f;
            Vector3 perpendicular = Vector3.Cross(slideDirection.normalized, Vector3.up).normalized * 0.1f;
            if (perpendicular.magnitude < 0.1f) // Handle case where slide direction is up/down
                perpendicular = Vector3.Cross(slideDirection.normalized, Vector3.right).normalized * 0.1f;
            
            Gizmos.DrawLine(endPos, arrowHead + perpendicular);
            Gizmos.DrawLine(endPos, arrowHead - perpendicular);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw detailed information when selected
            Gizmos.color = Color.yellow;
            
            // Draw slide distance measurement
            Vector3 startPos = Application.isPlaying ? _closedPosition : transform.position;
            Vector3 endPos = startPos + slideDirection.normalized * slideDistance;
            
            // Draw measurement lines
            Vector3 offset = Vector3.Cross(slideDirection.normalized, Vector3.up).normalized * 0.2f;
            if (offset.magnitude < 0.1f)
                offset = Vector3.Cross(slideDirection.normalized, Vector3.right).normalized * 0.2f;
            
            Gizmos.DrawLine(startPos + offset, endPos + offset);
            Gizmos.DrawLine(startPos + offset * 0.5f, startPos + offset * 1.5f);
            Gizmos.DrawLine(endPos + offset * 0.5f, endPos + offset * 1.5f);
        }
    }
}

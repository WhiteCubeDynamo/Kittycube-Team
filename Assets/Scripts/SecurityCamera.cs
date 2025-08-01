using UnityEngine;
using StealthHeist.Core.Interfaces;
using StealthHeist.Enemies;
using StealthHeist.Core;
using System.Collections;

namespace StealthHeist.Environment
{
    public class SecurityCamera : MonoBehaviour, IInteractable, IPersistent
    {
        private enum CameraState { Scanning, Detecting, Alerted, Disabled }

        [Header("Camera Settings")]
        [SerializeField] private float _viewRadius = 15f;
        [SerializeField, Range(0, 360)] private float _viewAngle = 90f;
        [SerializeField] private Transform _cameraPivot; // The part of the camera that visually rotates.

        [Header("Panning")]
        [SerializeField] private float _panSpeed = 30f;
        [SerializeField] private float _panAngle = 45f; // Will pan this many degrees to each side from its forward direction.
        [SerializeField] private float _panPauseDuration = 2f;

        [Header("Detection")]
        [SerializeField] private LayerMask _playerLayer;
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private float _timeToDetect = 2f;
        [SerializeField] private float _alertRange = 50f; // How far the alert reaches guards.

        [Header("State")]
        // This flag should be saved by a game state manager to persist across loops.
        [SerializeField] private string _persistenceID;
        [SerializeField] private bool _isPermanentlyDisabled = false;
        [SerializeField] private float _alertCooldown = 10f; // How long camera is disabled after an alert.

        private CameraState _currentState = CameraState.Scanning;
        private IDetectable _player;
        private float _detectionProgress = 0f;
        private float _panTimer = 0f;
        private Quaternion _initialRotation;
        private Coroutine _disableCoroutine;

        #region IPersistent Implementation

        public string PersistenceID => _persistenceID;

        public object CaptureState()
        {
            return _isPermanentlyDisabled;
        }

        public void RestoreState(object state)
        {
            _isPermanentlyDisabled = (bool)state;
        }
        #endregion

        #region IInteractable Implementation

        public string InteractionText => _isPermanentlyDisabled ? "Camera Disabled" : "Disable Camera";
        public bool CanInteract => !_isPermanentlyDisabled;

        public void Interact()
        {
            if (CanInteract)
            {
                Debug.Log("Player has permanently disabled the camera.");
                _isPermanentlyDisabled = true;
                _currentState = CameraState.Disabled;
                _detectionProgress = 0;
                // Here you would also change a light on the camera model from green/yellow to red/off.
            }
        }

        public void OnHighlight() { /* Optional: Show outline or UI prompt */ }
        public void OnUnhighlight() { /* Optional: Hide outline or UI prompt */ }

        #endregion

        private void Awake()
        {
            // In a full game, the player might be registered with a manager.
            // For now, we find the object with the IDetectable interface.
            _player = (IDetectable)FindFirstObjectByType(typeof(IDetectable), FindObjectsInactive.Include);

            if (_cameraPivot == null)
                _cameraPivot = transform;

            _initialRotation = _cameraPivot.rotation;
        }

        private void Update()
        {
            if (_isPermanentlyDisabled)
            {
                _currentState = CameraState.Disabled;
            }

            switch (_currentState)
            {
                case CameraState.Scanning:
                    HandleScanning();
                    LookForPlayer();
                    break;
                case CameraState.Detecting:
                    LookForPlayer();
                    break;
                case CameraState.Alerted:
                    // The camera is on cooldown after an alert.
                    break;
                case CameraState.Disabled:
                    // Do nothing.
                    break;
            }
        }

        private void HandleScanning()
        {
            if (_panAngle <= 0) return;

            _panTimer += Time.deltaTime;
            float pauseTime = _panPauseDuration * 2;
            float moveTime = (360 / _panSpeed) * (_panAngle * 2 / 360f);

            if (_panTimer < moveTime)
            {
                // PingPongs between -_panAngle and +_panAngle over moveTime
                float angle = Mathf.Lerp(-_panAngle, _panAngle, Mathf.PingPong(_panTimer * 2 / moveTime, 1));
                _cameraPivot.rotation = _initialRotation * Quaternion.Euler(0, angle, 0);
            }
            else if (_panTimer > moveTime + pauseTime)
            {
                _panTimer = 0; // Reset cycle
            }
        }

        private void LookForPlayer()
        {
            if (_player == null) return;

            Vector3 playerPosition = _player.Position;
            Vector3 directionToPlayer = (playerPosition - _cameraPivot.position).normalized;
            float distanceToPlayer = Vector3.Distance(_cameraPivot.position, playerPosition);

            bool playerInSight = false;
            if (distanceToPlayer < _viewRadius)
            {
                if (Vector3.Angle(_cameraPivot.forward, directionToPlayer) < _viewAngle / 2)
                {
                    if (!Physics.Raycast(_cameraPivot.position, directionToPlayer, distanceToPlayer, _obstacleLayer))
                    {
                        playerInSight = true;
                    }
                }
            }

            if (playerInSight)
            {
                _detectionProgress += Time.deltaTime;
                _currentState = CameraState.Detecting;

                if (_detectionProgress >= _timeToDetect)
                {
                    TriggerAlert(playerPosition);
                }
            }
            else
            {
                if (_detectionProgress > 0)
                {
                    _detectionProgress -= Time.deltaTime * 2; // Lose sight faster than you gain it.
                    _detectionProgress = Mathf.Max(0, _detectionProgress);
                }
                else
                {
                    _currentState = CameraState.Scanning;
                }
            }
        }

        private void TriggerAlert(Vector3 playerLastPosition)
        {
            if (_currentState == CameraState.Alerted) return;

            Debug.LogWarning($"CAMERA ALERT! Player detected at {playerLastPosition}");
            _currentState = CameraState.Alerted;
            _detectionProgress = 0;

            // Trigger the global event for UI and other systems.
            GameEvents.TriggerSecurityAlert(playerLastPosition);

            // Find nearby guards and send them to investigate.
            Collider[] guardsInArea = Physics.OverlapSphere(transform.position, _alertRange);
            foreach (var col in guardsInArea)
            {
                if (col.TryGetComponent<MuseumGuard>(out var guard))
                {
                    guard.RespondToAlarm(playerLastPosition);
                }
            }

            // Go on cooldown.
            StartCoroutine(AlertCooldownRoutine());
        }

        private IEnumerator AlertCooldownRoutine()
        {
            yield return new WaitForSeconds(_alertCooldown);
            _currentState = CameraState.Scanning;
        }

        /// <summary>
        /// Disables the camera for a specific duration.
        /// Called by external systems like a security panel.
        /// </summary>
        /// <param name="duration">How long to disable the camera for, in seconds.</param>
        public void DisableForDuration(float duration)
        {
            if (_isPermanentlyDisabled) return;

            if (_disableCoroutine != null)
            {
                StopCoroutine(_disableCoroutine);
            }
            _currentState = CameraState.Disabled;
            _disableCoroutine = StartCoroutine(DisableTimerRoutine(duration));
        }

        /// <summary>
        /// Disables the camera for the remainder of the current game loop.
        /// It will be re-enabled when the scene is reloaded.
        /// </summary>
        public void DisableForLoop()
        {
            if (_isPermanentlyDisabled) return;

            if (_disableCoroutine != null)
            {
                StopCoroutine(_disableCoroutine);
            }
            _currentState = CameraState.Disabled;
        }

        private IEnumerator DisableTimerRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (!_isPermanentlyDisabled) _currentState = CameraState.Scanning;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, _viewRadius);

            Vector3 viewAngleA = DirectionFromAngle(-_viewAngle / 2, false);
            Vector3 viewAngleB = DirectionFromAngle(_viewAngle / 2, false);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_cameraPivot.position, _cameraPivot.position + viewAngleA * _viewRadius);
            Gizmos.DrawLine(_cameraPivot.position, _cameraPivot.position + viewAngleB * _viewRadius);

            if (_player != null && _detectionProgress > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(_cameraPivot.position, _player.Position);
            }
        }

        private Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += _cameraPivot.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
    }
}
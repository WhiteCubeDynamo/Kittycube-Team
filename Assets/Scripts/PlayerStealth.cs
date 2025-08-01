using UnityEngine;
using StealthHeist.Core.Enums;

namespace StealthHeist.Player
{
    /// <summary>
    /// Implements the IDetectable interface for the player character.
    /// This script is responsible for reporting the player's position,
    /// and calculating their current noise and visibility levels based on actions
    /// like moving, running, and crouching.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerStealth : MonoBehaviour
    {
        [Header("Stealth Attributes")]
        [SerializeField] private float _walkingNoiseLevel = 0.4f;
        [SerializeField] private float _runningNoiseLevel = 0.8f;
        [SerializeField] private float _crouchingNoiseLevel = 0.1f;
        [Space]
        [SerializeField] private float _standingVisibility = 0.8f;
        [SerializeField] private float _crouchingVisibility = 0.4f;
        [SerializeField] private float _runningSpeedThreshold = 3.5f;

        // --- Private Fields ---
        private Rigidbody _rigidbody;
        private bool _isCrouching = false; // This should be controlled by a player input script
/*
        #region IDetectable Implementation

        public Vector3 Position => transform.position;
        public StealthState CurrentStealthState { get; private set; } = StealthState.Hidden;
        public float NoiseLevel { get; private set; }
        public float VisibilityLevel { get; private set; }

        public void ChangeStealthState(StealthState newState)
        {
            if (CurrentStealthState == newState) return;

            CurrentStealthState = newState;
            Debug.Log($"Player stealth state changed to: {newState}");
            // Here you could trigger UI changes or other feedback.
        }

        public void MakeNoise(float level)
        {
            // This method is for discrete noise events, like knocking something over.
            // A more complex implementation might use a temporary noise boost that decays over time.
            Debug.Log($"Player made a loud noise of level: {level}");
            // You could alert nearby guards directly from here if needed.
        }

        #endregion*/
        public float NoiseLevel=0f;
        public float VisibilityLevel=0f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            CalculateStealthLevels();
        }

        private void CalculateStealthLevels()
        {
            // Calculate Noise Level based on movement
            float horizontalSpeed = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z).magnitude;

            if (horizontalSpeed > _runningSpeedThreshold) // Running
            {
                NoiseLevel = _runningNoiseLevel;
            }
            else if (horizontalSpeed > 0.1f) // Walking or Crouching
            {
                NoiseLevel = _isCrouching ? _crouchingNoiseLevel : _walkingNoiseLevel;
            }
            else // Standing still
            {
                NoiseLevel = 0f;
            }

            // Calculate Visibility Level
            VisibilityLevel = _isCrouching ? _crouchingVisibility : _standingVisibility;
            // This could be further modified by lighting conditions in a more advanced system.
        }

        /// <summary>
        /// Allows external scripts (like a player controller) to update the crouching state.
        /// </summary>
        /// <param name="isCrouching">The new crouching state.</param>
        public void SetCrouching(bool isCrouching)
        {
            _isCrouching = isCrouching;
        }
    }
}

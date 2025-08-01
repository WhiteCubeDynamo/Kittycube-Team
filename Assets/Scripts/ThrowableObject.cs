using UnityEngine;
using StealthHeist.Enemies;

namespace StealthHeist.Environment
{
    /// <summary>
    /// A script for objects that can be thrown to create a noise distraction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class ThrowableObject : MonoBehaviour
    {
        [Header("Distraction Settings")]
        [Tooltip("The radius within which guards will hear this object land.")]
        [SerializeField] private float _noiseRadius = 15f;
        [Tooltip("How long the object remains in the world after landing.")]
        [SerializeField] private float _lifeTimeAfterLanding = 5f;

        private bool _hasLanded = false;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Only trigger the noise on the first impact.
            if (_hasLanded) return;

            // A small velocity check to prevent triggering noise when just being placed.
            if (_rb.linearVelocity.magnitude < 1f) return;

            _hasLanded = true;
            MakeNoise();

            // Destroy the object after a short delay to clean up the scene.
            Destroy(gameObject, _lifeTimeAfterLanding);
        }

        private void MakeNoise()
        {
            Debug.Log($"Throwable landed at {transform.position}, creating a distraction.");

            // Find all colliders within the noise radius.
            Collider[] collidersInArea = Physics.OverlapSphere(transform.position, _noiseRadius);

            foreach (var col in collidersInArea)
            {
                // If a collider belongs to an enemy, tell them to investigate.
                if (col.TryGetComponent<BaseEnemy>(out var enemy))
                {
                    enemy.HearNoise(transform.position);
                }
            }
        }
    }
}
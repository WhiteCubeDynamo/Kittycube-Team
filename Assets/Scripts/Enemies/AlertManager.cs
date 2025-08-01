using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StealthHeist.Core.Enums;

namespace StealthHeist.Enemies
{
    /// <summary>
    /// Global alert manager that coordinates guard responses to player detection
    /// </summary>
    public class AlertManager : MonoBehaviour
    {
        [Header("Alert Settings")]
        public float alertRadius = 20f;
        public float alertDuration = 30f;
        public float searchDuration = 15f;
        public LayerMask guardLayer = -1;
        
        [Header("Debug")]
        public bool showAlertRadius = true;
        public Color alertColor = Color.red;
        
        // Singleton instance
        public static AlertManager Instance { get; private set; }
        
        // Alert state
        private bool isAlerted = false;
        private Vector3 lastKnownPlayerPosition;
        private float alertTimer = 0f;
        private List<BaseGuard> allGuards = new List<BaseGuard>();
        
        // Events
        public System.Action<Vector3> OnPlayerDetected;
        public System.Action OnAlertEnded;
        
        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        void Start()
        {
            // Find all guards in the scene
            RefreshGuardList();
        }
        
        void Update()
        {
            if (isAlerted)
            {
                alertTimer -= Time.deltaTime;
                
                if (alertTimer <= 0)
                {
                    EndAlert();
                }
            }
        }
        
        public void RefreshGuardList()
        {
            allGuards.Clear();
            BaseGuard[] guards = FindObjectsByType<BaseGuard>(FindObjectsSortMode.None);
            allGuards.AddRange(guards);
            Debug.Log($"Alert Manager found {allGuards.Count} guards");
        }
        
        public void TriggerAlert(Vector3 playerPosition, BaseGuard reportingGuard = null)
        {
            lastKnownPlayerPosition = playerPosition;
            alertTimer = alertDuration;
            
            if (!isAlerted)
            {
                isAlerted = true;
                Debug.Log($"Alert triggered at position: {playerPosition}");
                OnPlayerDetected?.Invoke(playerPosition);
            }
            
            // Notify nearby guards
            NotifyNearbyGuards(playerPosition, reportingGuard);
        }
        
        private void NotifyNearbyGuards(Vector3 alertPosition, BaseGuard reportingGuard)
        {
            foreach (BaseGuard guard in allGuards)
            {
                if (guard == null || guard == reportingGuard) continue;
                
                float distance = Vector3.Distance(guard.transform.position, alertPosition);
                
                if (distance <= alertRadius)
                {
                    guard.OnAlertReceived(alertPosition);
                }
            }
        }
        
        public void UpdatePlayerPosition(Vector3 newPosition)
        {
            if (isAlerted)
            {
                lastKnownPlayerPosition = newPosition;
                alertTimer = alertDuration; // Reset timer when player is spotted again
                
                // Update all guards with new position
                foreach (BaseGuard guard in allGuards)
                {
                    if (guard != null && guard.CurrentState == EnemyState.Chasing)
                    {
                        guard.UpdatePlayerPosition(newPosition);
                    }
                }
            }
        }
        
        private void EndAlert()
        {
            isAlerted = false;
            alertTimer = 0f;
            Debug.Log("Alert ended - guards returning to patrol");
            OnAlertEnded?.Invoke();
            
            // Tell all guards to stop chasing
            foreach (BaseGuard guard in allGuards)
            {
                if (guard != null)
                {
                    guard.OnAlertEnded();
                }
            }
        }
        
        public void RegisterGuard(BaseGuard guard)
        {
            if (!allGuards.Contains(guard))
            {
                allGuards.Add(guard);
            }
        }
        
        public void UnregisterGuard(BaseGuard guard)
        {
            allGuards.Remove(guard);
        }
        
        // Public getters
        public bool IsAlerted => isAlerted;
        public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
        public float AlertTimeRemaining => alertTimer;
        
        void OnDrawGizmosSelected()
        {
            if (!showAlertRadius) return;
            
            Gizmos.color = alertColor;
            Gizmos.DrawWireSphere(transform.position, alertRadius);
            
            if (isAlerted)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 2f);
            }
        }
    }
}

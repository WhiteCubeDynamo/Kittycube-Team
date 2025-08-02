using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using StealthHeist.Core.Enums;
using StealthHeist.Player;

namespace StealthHeist.Enemies
{
    /// <summary>
    /// Base class for guards with integrated waypoint system for patrol, detection, and chase
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class BaseGuard : MonoBehaviour
    {
        [Header("Guard Settings")]
        public float patrolSpeed = 3.5f;
        public float chaseSpeed = 6f;
        public float detectionRadius = 5f;
        public float fieldOfViewAngle = 60f;
        public float stopChaseTime = 5f; // time until chase ends if player not visible
        public float catchDistance = 1.5f; // distance to catch player
        public LayerMask obstacleLayer = -1;
        
        [Header("Patrol Settings")]
        public List<Transform> waypoints = new List<Transform>();
        public bool isLooping = true;
        public bool reverseOnEnd = false;
        public float waitTimeAtWaypoint = 2f;
        
        [Header("Detection Settings")]
        public bool useLineOfSight = true;
        public float maxDetectionDistance = 8f;
        
        [Header("Debug")]
        public bool showDetectionRadius = true;
        public bool showFieldOfView = true;
        public bool showWaypoints = true;
        public Color detectionColor = Color.red;
        public Color waypointColor = Color.yellow;
        public Color pathColor = Color.green;
        
        [Header("Vision Cone Settings")]
        public Material visionConeMaterial;
        public Color normalVisionColor = new Color(1f, 1f, 0f, 0.2f);
        public Color detectedVisionColor = new Color(1f, 0f, 0f, 0.4f);

        // Private fields
        private int currentWaypointIndex = 0;
        private bool isReversing = false;
        private float waypointWaitTimer = 0f;
        private Vector3 originalPosition;
        private int originalWaypointIndex;

        // Internal state
        private NavMeshAgent navAgent;
        private EnemyState currentState;
        private float chaseTimer;
        private Vector3 lastKnownPlayerPosition;
        private Transform playerTransform;
        private GameLoopManager gameLoopManager;
        
        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            originalPosition = transform.position;

            // Add and configure the FieldOfView component
            FieldOfView fov = gameObject.AddComponent<FieldOfView>();
            fov.viewRadius = detectionRadius;
            fov.viewAngle = fieldOfViewAngle;
            fov.targetMask = LayerMask.GetMask("Player"); // Assuming player is on "Player" layer
            fov.obstacleMask = obstacleLayer;
            fov.viewMaterial = visionConeMaterial;
            fov.normalColor = normalVisionColor;
            fov.detectedColor = detectedVisionColor;
            
            // Find closest waypoint as starting point if waypoints exist
            if (waypoints.Count > 0)
            {
                currentWaypointIndex = GetClosestWaypointIndex(transform.position);
                originalWaypointIndex = currentWaypointIndex;
            }
            
            ChangeState(EnemyState.Patrolling);
        }
        
        private void Start()
        {
            // Register with alert manager
            if (AlertManager.Instance != null)
            {
                AlertManager.Instance.RegisterGuard(this);
            }
            
            // Find game loop manager
            gameLoopManager = FindFirstObjectByType<GameLoopManager>();
            if (gameLoopManager == null)
            {
                Debug.LogWarning($"Guard {gameObject.name} could not find GameLoopManager!");
            }
            
            // Find player - try multiple methods
            FindPlayer();
        }
        
        private void FindPlayer()
        {
            // Method 1: Try to find by tag
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                Debug.Log($"Guard {gameObject.name} found player by tag: {playerObj.name}");
                return;
            }
            
            // Method 2: Try to find PhysicalPlayerController component
            PhysicalPlayerController playerController = FindFirstObjectByType<PhysicalPlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
                Debug.Log($"Guard {gameObject.name} found player by component: {playerController.name}");
                return;
            }
            
            // Method 3: Try to find by name
            playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                Debug.Log($"Guard {gameObject.name} found player by name: {playerObj.name}");
                return;
            }
            
            Debug.LogWarning($"Guard {gameObject.name} could not find player! Make sure player GameObject has 'Player' tag or is named 'Player'");
        }

        private void OnDestroy()
        {
            if (AlertManager.Instance != null)
            {
                AlertManager.Instance.UnregisterGuard(this);
            }
        }

        private void Update()
        {
            switch (currentState)
            {
                case EnemyState.Patrolling:
                    Patrol();
                    break;
                case EnemyState.Chasing:
                    Chase();
                    break;
                case EnemyState.Returning:
                    ReturnToPatrol();
                    break;
            }
        }

        private void Patrol()
        {
            navAgent.speed = patrolSpeed;
            
            if (waypoints.Count == 0)
            {
                // No waypoints, just detect player
                DetectPlayer();
                return;
            }
            
            // Handle waiting at waypoint
            if (waypointWaitTimer > 0)
            {
                waypointWaitTimer -= Time.deltaTime;
                DetectPlayer();
                return;
            }
            
            Transform waypoint = waypoints[currentWaypointIndex];
            if (waypoint != null)
            {
                navAgent.SetDestination(waypoint.position);

                if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
                {
                    // Arrived at waypoint
                    waypointWaitTimer = waitTimeAtWaypoint;
                    currentWaypointIndex = GetNextWaypointIndex(currentWaypointIndex, isReversing);
                }
            }

            DetectPlayer();
        }

        private void DetectPlayer()
        {
            // Try to find player if we don't have reference
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // Debug info
            if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
            {
                Debug.Log($"Guard {gameObject.name}: Player distance = {distanceToPlayer:F2}, Detection radius = {detectionRadius}");
            }
            
            // Check if player is within detection radius
            if (distanceToPlayer <= detectionRadius)
            {
                // Check if player is within field of view
                Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToPlayer);
                
                if (angle <= fieldOfViewAngle / 2)
                {
                    // Check line of sight if enabled
                    if (!useLineOfSight || HasLineOfSight(playerTransform.position))
                    {
                        // Check player stealth state
                        PlayerStealth playerStealth = playerTransform.GetComponent<PlayerStealth>();
                        if (playerStealth != null)
                        {
                            // More likely to detect if player is visible or making noise
                            float detectionChance = playerStealth.VisibilityLevel + playerStealth.NoiseLevel;
                            
                            if (detectionChance > 0.5f) // Adjust threshold as needed
                            {
                                Debug.Log($"Guard {gameObject.name} detected player! Visibility: {playerStealth.VisibilityLevel}, Noise: {playerStealth.NoiseLevel}");
                                TriggerChase(playerTransform.position);
                            }
                        }
                        else
                        {
                            // Fallback if no stealth component - detect immediately
                            Debug.Log($"Guard {gameObject.name} detected player (no stealth component)!");
                            TriggerChase(playerTransform.position);
                        }
                    }
                    else
                    {
                        if (Time.frameCount % 60 == 0)
                        {
                            Debug.Log($"Guard {gameObject.name}: Player in FOV but no line of sight");
                        }
                    }
                }
                else
                {
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"Guard {gameObject.name}: Player in range but outside FOV (angle: {angle:F1}°, FOV: {fieldOfViewAngle}°)");
                    }
                }
            }
        }
        
        private bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position + Vector3.up * 1.5f; // Eye level
            Vector3 directionToTarget = (targetPosition - startPosition).normalized;
            float distanceToTarget = Vector3.Distance(startPosition, targetPosition);
            
            Ray ray = new Ray(startPosition, directionToTarget);
            
            // Debug ray
            Debug.DrawRay(startPosition, directionToTarget * distanceToTarget, Color.red, 0.1f);
            
            if (Physics.Raycast(ray, out RaycastHit hit, distanceToTarget, obstacleLayer))
            {
                // Check if we hit the player (not an obstacle)
                if (hit.collider.CompareTag("Player"))
                {
                    return true; // We can see the player
                }
                
                Debug.Log($"Guard {gameObject.name}: Line of sight blocked by {hit.collider.name}");
                return false; // Obstacle blocking view
            }
            
            return true; // Clear line of sight
        }

        private void TriggerChase(Vector3 playerPosition)
        {
            if (AlertManager.Instance != null)
            {
                AlertManager.Instance.TriggerAlert(playerPosition, this);
            }
            ChangeState(EnemyState.Chasing);
            chaseTimer = stopChaseTime;
            Debug.Log($"Guard {gameObject.name} detected player at {playerPosition}");
        }
        
        private void CatchPlayer()
        {
            Debug.Log($"Guard {gameObject.name} caught the player! Resetting loop...");
            
            // Stop the chase
            ChangeState(EnemyState.Patrolling);
            
            // End the alert
            if (AlertManager.Instance != null)
            {
                AlertManager.Instance.OnAlertEnded?.Invoke();
            }
            
            // Trigger loop reset via GameLoopManager
            if (gameLoopManager != null)
            {
                gameLoopManager.OnPlayerCaught();
            }
            else
            {
                Debug.LogError("Cannot reset loop - GameLoopManager not found!");
            }
        }

        private void Chase()
        {
            navAgent.speed = chaseSpeed;
            
            // Check if we caught the player
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                
                if (distanceToPlayer <= catchDistance)
                {
                    CatchPlayer();
                    return;
                }
            }
            
            // Continue detecting player while chasing
            DetectPlayer();
            
            if (AlertManager.Instance != null && AlertManager.Instance.IsAlerted)
            {
                lastKnownPlayerPosition = AlertManager.Instance.LastKnownPlayerPosition;
                navAgent.SetDestination(lastKnownPlayerPosition);
                
                // Reset timer if we can still see the player
                if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= detectionRadius)
                {
                    chaseTimer = stopChaseTime;
                }
            }
            else
            {
                // No global alert, chase last known position
                if (lastKnownPlayerPosition != Vector3.zero)
                {
                    navAgent.SetDestination(lastKnownPlayerPosition);
                }
            }

            // Countdown chase timer
            chaseTimer -= Time.deltaTime;
            
            // Check if reached last known position or timer expired
            if ((!navAgent.pathPending && navAgent.remainingDistance < 1f) || chaseTimer <= 0)
            {
                ChangeState(EnemyState.Returning);
            }
        }

        private void ReturnToPatrol()
        {
            navAgent.speed = patrolSpeed;
            
            if (waypoints.Count > 0)
            {
                // Find closest waypoint to return to
                int closestWaypointIndex = GetClosestWaypointIndex(transform.position);
                Transform closestWaypoint = waypoints[closestWaypointIndex];
                
                if (closestWaypoint != null)
                {
                    navAgent.SetDestination(closestWaypoint.position);
                    
                    // Check if reached waypoint
                    if (!navAgent.pathPending && navAgent.remainingDistance < 1f)
                    {
                        currentWaypointIndex = closestWaypointIndex;
                        ChangeState(EnemyState.Patrolling);
                        Debug.Log($"Guard {gameObject.name} returned to patrol");
                    }
                }
            }
            else
            {
                // No waypoints, return to original position
                navAgent.SetDestination(originalPosition);
                
                if (!navAgent.pathPending && navAgent.remainingDistance < 1f)
                {
                    ChangeState(EnemyState.Patrolling);
                }
            }
        }

        public void OnAlertReceived(Vector3 playerPosition)
        {
            if (currentState != EnemyState.Chasing)
            {
                ChangeState(EnemyState.Chasing);
                chaseTimer = stopChaseTime;
            }
            lastKnownPlayerPosition = playerPosition;
        }

        public void UpdatePlayerPosition(Vector3 playerPosition)
        {
            lastKnownPlayerPosition = playerPosition;
            if (currentState == EnemyState.Chasing)
            {
                chaseTimer = stopChaseTime; // Reset chase timer
            }
        }

        public void OnAlertEnded()
        {
            if (currentState == EnemyState.Chasing)
            {
                ChangeState(EnemyState.Returning);
            }
        }

        private void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;

            EnemyState previousState = currentState;
            currentState = newState;
            
            Debug.Log($"Guard {gameObject.name} changed state from {previousState} to {newState}");
            
            // Reset timers and state when changing states
            switch (newState)
            {
                case EnemyState.Patrolling:
                    waypointWaitTimer = 0f;
                    break;
                case EnemyState.Chasing:
                    chaseTimer = stopChaseTime;
                    break;
                case EnemyState.Returning:
                    break;
            }
        }
        
        // Waypoint helper methods
        private int GetNextWaypointIndex(int currentIndex, bool reversing)
        {
            if (waypoints.Count == 0) return 0;
            
            if (reversing)
            {
                int nextIndex = currentIndex - 1;
                if (nextIndex < 0)
                {
                    if (reverseOnEnd)
                    {
                        isReversing = false;
                        return 1;
                    }
                    else
                    {
                        return isLooping ? waypoints.Count - 1 : 0;
                    }
                }
                return nextIndex;
            }
            else
            {
                int nextIndex = currentIndex + 1;
                if (nextIndex >= waypoints.Count)
                {
                    if (reverseOnEnd)
                    {
                        isReversing = true;
                        return waypoints.Count - 2;
                    }
                    else
                    {
                        return isLooping ? 0 : waypoints.Count - 1;
                    }
                }
                return nextIndex;
            }
        }
        
        private int GetClosestWaypointIndex(Vector3 position)
        {
            if (waypoints.Count == 0) return 0;
            
            int closestIndex = 0;
            float closestDistance = Vector3.Distance(position, waypoints[0].position);
            
            for (int i = 1; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                
                float distance = Vector3.Distance(position, waypoints[i].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }
            
            return closestIndex;
        }
        
        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            if (showDetectionRadius)
            {
                Gizmos.color = detectionColor;
                Gizmos.DrawWireSphere(transform.position, detectionRadius);
            }
            
            // Draw catch radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, catchDistance);
            
            // Draw field of view
            if (showFieldOfView)
            {
                Vector3 forward = transform.forward;
                Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * forward;
                Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * forward;
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, leftBoundary * detectionRadius);
                Gizmos.DrawRay(transform.position, rightBoundary * detectionRadius);
            }
            
            // Draw waypoints and path
            if (showWaypoints && waypoints.Count > 0)
            {
                // Draw waypoints
                Gizmos.color = waypointColor;
                for (int i = 0; i < waypoints.Count; i++)
                {
                    if (waypoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
                        
                        // Highlight current waypoint
                        if (i == currentWaypointIndex)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireSphere(waypoints[i].position, 0.7f);
                            Gizmos.color = waypointColor;
                        }
                    }
                }
                
                // Draw path
                Gizmos.color = pathColor;
                for (int i = 0; i < waypoints.Count - 1; i++)
                {
                    if (waypoints[i] != null && waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }
                }
                
                // Draw loop connection if looping
                if (isLooping && waypoints.Count > 2 && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
                {
                    Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
                }
            }
        }

        public EnemyState CurrentState => currentState;
    }
}

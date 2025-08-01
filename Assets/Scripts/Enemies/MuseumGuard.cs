using UnityEngine;
using StealthHeist.Core.Enums;

namespace StealthHeist.Enemies
{
    public class MuseumGuard : BaseEnemy
    {
        [Header("Guard Specific")]
        [SerializeField] private float searchDuration = 10f;
        [SerializeField] private float chaseSpeed = 6f;
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float losePlayerTime = 3f; // Time before losing player after breaking line of sight
        
        [Header("Audio")]
        [SerializeField] private AudioClip chaseStartClip;
        [SerializeField] private AudioClip searchingClip;
        [SerializeField] private AudioClip lostPlayerClip;
        
        private AudioSource audioSource;
        private float losePlayerTimer = 0f;
        
        private float searchTimer = 0f;
        private Vector3 investigationPoint;
        
        /// <summary>
        /// Safely tries to set a destination, validating the path first
        /// </summary>
        private bool TrySetDestination(Vector3 destination)
        {
            UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
            if (agent.CalculatePath(destination, path))
            {
                if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(destination);
                    return true;
                }
            }
            return false;
        }
        
        protected override void Start()
        {
            base.Start();
            audioSource = GetComponent<AudioSource>();
            if (!audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            agent.speed = patrolSpeed;

        }
        
        protected override void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            
            agent.speed = patrolSpeed;
            
            // Check if agent is stuck or can't reach destination
            if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial || 
                agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                // Skip to next patrol point if current one is unreachable
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                TrySetDestination(patrolPoints[currentPatrolIndex].position);
                return;
            }
            
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                waitTimer += Time.deltaTime;
                
                if (waitTimer >= waitTime)
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    TrySetDestination(patrolPoints[currentPatrolIndex].position);
                    waitTimer = 0f;
                }
            }
            else if (agent.remainingDistance > 0.5f)
            {
                waitTimer = 0f;
            }
        }
        
        protected override void Investigate()
        {
            agent.speed = patrolSpeed * 1.5f;
            
            if (Vector3.Distance(transform.position, lastKnownPlayerPosition) > 1f)
            {
                if (!TrySetDestination(lastKnownPlayerPosition))
                {
                    // Can't reach investigation point, start searching instead
                    ChangeState(EnemyState.Searching);
                    return;
                }
            }
            else
            {
                // Look around the area
                waitTimer += Time.deltaTime;
                
                if (waitTimer >= waitTime * 2f)
                {
                    if (detectionLevel > 0.3f)
                    {
                        ChangeState(EnemyState.Searching);
                    }
                    else
                    {
                        ChangeState(EnemyState.Returning);
                    }
                }
            }
        }
        
private void PlayAudioClip(AudioClip clip)
{
    if (audioSource && clip && !audioSource.isPlaying)
    {
        audioSource.PlayOneShot(clip);
    }
}

protected override void Chase()
        {
            agent.speed = chaseSpeed;
            
            if (playerTarget != null)
            {
                Vector3 directionToPlayer = (playerTarget.Position - transform.position).normalized;
                float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.Position);
                
                // Check if we can still see the player
                bool canSeePlayer = false;
                if (distanceToPlayer <= viewRadius)
                {
                    float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
                    if (angleToPlayer <= viewAngle * 0.5f)
                    {
                        if (!Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distanceToPlayer, obstacleLayer))
                        {
                            canSeePlayer = true;
                            losePlayerTimer = 0f;
                        }
                    }
                }
                
                if (canSeePlayer)
                {
                    if (!TrySetDestination(playerTarget.Position))
                    {
                        // Can't reach player directly, start searching at last known position
                        ChangeState(EnemyState.Searching);
                        return;
                    }
                    lastKnownPlayerPosition = playerTarget.Position;
                }
                else
                {
                    // Lost sight of player, count down before searching
                    losePlayerTimer += Time.deltaTime;
                    if (losePlayerTimer >= losePlayerTime)
                    {
                        ChangeState(EnemyState.Searching);
                        losePlayerTimer = 0f;
                    }
                }
            }
            else
            {
                // No player target, go to searching
                ChangeState(EnemyState.Searching);
            }
        }
        
        protected override void Search()
        {
            agent.speed = patrolSpeed * 1.2f;
            searchTimer += Time.deltaTime;
            
            // Search around the last known position
            if (!agent.pathPending && agent.remainingDistance < 1f)
            {
                // Try to find a reachable random point around the last known position
                int attempts = 0;
                while (attempts < 5) // Limit attempts to avoid infinite loop
                {
                    Vector3 randomDirection = Random.insideUnitSphere * 5f;
                    randomDirection += lastKnownPlayerPosition;
                    randomDirection.y = transform.position.y;
                    
                    if (Physics.Raycast(randomDirection + Vector3.up * 2f, Vector3.down, 3f))
                    {
                        if (TrySetDestination(randomDirection))
                        {
                            break; // Successfully set a reachable destination
                        }
                    }
                    attempts++;
                }
            }
            
            if (searchTimer >= searchDuration)
            {
                ChangeState(EnemyState.Returning);
                searchTimer = 0f;
            }
        }
        
        protected override void ChangeState(EnemyState newState)
        {
            // Play audio based on state transitions
            if (newState != currentState)
            {
                switch (newState)
                {
                    case EnemyState.Chasing:
                        PlayAudioClip(chaseStartClip);
                        Debug.Log("Guard: Stop right there!");
                        break;
                    case EnemyState.Searching:
                        if (currentState == EnemyState.Chasing)
                        {
                            PlayAudioClip(searchingClip);
                            Debug.Log("Guard: Where did they go?");
                        }
                        break;
                    case EnemyState.Returning:
                        if (currentState == EnemyState.Searching)
                        {
                            PlayAudioClip(lostPlayerClip);
                            Debug.Log("Guard: Must have been nothing...");
                        }
                        break;
                }
            }
            
            base.ChangeState(newState);
        }
        
        protected override void ReturnToPatrol()
        {
            agent.speed = patrolSpeed;
            
            Vector3 nearestPatrolPoint = patrolPoints[0].position;
            float nearestDistance = Vector3.Distance(transform.position, nearestPatrolPoint);
            int nearestIndex = 0;
            
            for (int i = 1; i < patrolPoints.Length; i++)
            {
                float distance = Vector3.Distance(transform.position, patrolPoints[i].position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPatrolPoint = patrolPoints[i].position;
                    nearestIndex = i;
                }
            }
            
            // Check if we can reach the nearest patrol point
            if (!TrySetDestination(nearestPatrolPoint))
            {
                // If we can't reach any patrol point, just start patrolling from current position
                currentPatrolIndex = nearestIndex;
                ChangeState(EnemyState.Patrolling);
                return;
            }
            
            // Check if agent is stuck
            if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial || 
                agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                currentPatrolIndex = nearestIndex;
                ChangeState(EnemyState.Patrolling);
                return;
            }
            
            if (!agent.pathPending && agent.remainingDistance < 1f)
            {
                currentPatrolIndex = nearestIndex;
                ChangeState(EnemyState.Patrolling);
            }
        }

        /// <summary>
        /// Called by external systems (like a SecurityCamera) to alert the guard.
        /// </summary>
        /// <param name="alarmPosition">The position the guard should investigate.</param>
        public void RespondToAlarm(Vector3 alarmPosition)
        {
            // A guard will only respond if they are in a non-alerted state.
            if (currentState == EnemyState.Patrolling || currentState == EnemyState.Returning)
            {
                lastKnownPlayerPosition = alarmPosition;
                ChangeState(EnemyState.Investigating);
            }
        }
        
        // Override to add additional debugging
        protected override void DetectPlayer()
        {
            base.DetectPlayer();
            
            // Debug visualization in Scene view
            if (playerTarget != null && detectionLevel > 0)
            {
                Debug.DrawLine(transform.position + Vector3.up, playerTarget.Position, Color.Lerp(Color.yellow, Color.red, detectionLevel));
            }
        }
        
        // Enhanced gizmo drawing for better debugging
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // Draw current state
            Vector3 textPos = transform.position + Vector3.up * 3f;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(textPos, $"State: {currentState}\nDetection: {detectionLevel:F2}");
            #endif
            
            // Draw last known player position
            if (lastKnownPlayerPosition != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
                Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
            }
        }
    }
}

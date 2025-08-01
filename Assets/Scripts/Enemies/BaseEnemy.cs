using System;
using UnityEngine;
using UnityEngine.AI;
using StealthHeist.Core;
using StealthHeist.Core.Enums;
using StealthHeist.Core.Interfaces;

namespace StealthHeist.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class BaseEnemy : MonoBehaviour
    {
        [Header("Enemy Settings")]
        [SerializeField] protected float viewRadius = 10f;
        [SerializeField] protected float viewAngle = 90f;
        [SerializeField] protected float hearingRange = 8f;
        [SerializeField] protected float detectionRate = 0.5f;
        [SerializeField] protected float alertTime = 5f;
        [SerializeField] protected LayerMask obstacleLayer = -1;
        [SerializeField] protected LayerMask playerLayer = -1;
        
        [Header("Patrol Points")]
        [SerializeField] protected Transform[] patrolPoints;
        [SerializeField] protected float waitTime = 2f;
        
        public event Action<EnemyState> OnStateChanged;
        public event Action<float> OnDetectionChanged;
        public event Action OnPlayerDetected;
        
        protected NavMeshAgent agent;
        protected EnemyState currentState;
        protected Vector3 lastKnownPlayerPosition;
        protected float detectionLevel = 0f;
        protected int currentPatrolIndex = 0;
        protected float waitTimer = 0f;
        protected IDetectable playerTarget;
        
        public EnemyState CurrentState => currentState;
        public float DetectionLevel => detectionLevel;
        
        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }
        
        protected virtual void Start()
        {
            ChangeState(EnemyState.Patrolling);
        }
        
        protected virtual void Update()
        {
            switch (currentState)
            {
                case EnemyState.Patrolling:
                    Patrol();
                    break;
                case EnemyState.Investigating:
                    Investigate();
                    break;
                case EnemyState.Chasing:
                    Chase();
                    break;
                case EnemyState.Searching:
                    Search();
                    break;
                case EnemyState.Returning:
                    ReturnToPatrol();
                    break;
            }
            
            DetectPlayer();
            UpdateDetectionLevel();
        }
        
        protected virtual void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            
            currentState = newState;
            OnStateChanged?.Invoke(currentState);
            
            // Reset timers when changing states
            waitTimer = 0f;
        }
        
        protected abstract void Patrol();
        protected abstract void Investigate();
        protected abstract void Chase();
        protected abstract void Search();
        protected abstract void ReturnToPatrol();
        
        protected virtual void DetectPlayer()
        {
            if (playerTarget == null) return;
            
            Vector3 directionToPlayer = (playerTarget.Position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.Position);
            
            // Visual detection
            if (distanceToPlayer <= viewRadius)
            {
                float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
                
                if (angleToPlayer <= viewAngle * 0.5f)
                {
                    // Check for line of sight
                    if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, distanceToPlayer, obstacleLayer | playerLayer))
                    {
                        if (hit.collider.gameObject.layer == Mathf.Log(playerLayer.value, 2))
                        {
                            // Player is visible
                            IncreaseDetection(playerTarget.VisibilityLevel);
                            lastKnownPlayerPosition = playerTarget.Position;
                        }
                    }
                }
            }
            
            // Audio detection
            if (distanceToPlayer <= hearingRange && playerTarget.NoiseLevel > 0.3f)
            {
                IncreaseDetection(playerTarget.NoiseLevel * 0.5f);
                lastKnownPlayerPosition = playerTarget.Position;
            }
        }
        
        protected virtual void IncreaseDetection(float amount)
        {
            detectionLevel += amount * detectionRate * Time.deltaTime;
            detectionLevel = Mathf.Clamp01(detectionLevel);
            
            OnDetectionChanged?.Invoke(detectionLevel);
            
            if (detectionLevel >= 1f && currentState != EnemyState.Chasing)
            {
                OnPlayerDetected?.Invoke();
                GameEvents.TriggerPlayerCaught();
                ChangeState(EnemyState.Chasing);
            }
            else if (detectionLevel >= 0.5f && currentState == EnemyState.Patrolling)
            {
                ChangeState(EnemyState.Investigating);
            }
        }
        
        protected virtual void UpdateDetectionLevel()
        {
            if (currentState == EnemyState.Patrolling || currentState == EnemyState.Returning)
            {
                detectionLevel -= Time.deltaTime * 0.5f;
                detectionLevel = Mathf.Max(0f, detectionLevel);
                OnDetectionChanged?.Invoke(detectionLevel);
            }
        }
        
        public void SetPlayerTarget(IDetectable target)
        {
            playerTarget = target;
        }
        
        /// <summary>
        /// Makes the enemy react to a noise from a specific location.
        /// Can be called by external objects like thrown items.
        /// </summary>
        /// <param name="noiseLocation">The world position of the noise.</param>
        public virtual void HearNoise(Vector3 noiseLocation)
        {
            // Only react if not already in a high-alert state.
            if (currentState == EnemyState.Patrolling || currentState == EnemyState.Returning)
            {
                lastKnownPlayerPosition = noiseLocation;
                ChangeState(EnemyState.Investigating);
            }
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            // Draw view radius
            Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
            
            // Draw view angle
            Vector3 viewAngleA = DirectionFromAngle(-viewAngle * 0.5f, false);
            Vector3 viewAngleB = DirectionFromAngle(viewAngle * 0.5f, false);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
            Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);
            
            // Draw hearing range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, hearingRange);
        }
        
        private Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
    }
}

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
        
        private float searchTimer = 0f;
        private Vector3 investigationPoint;
        
        protected override void Start()
        {
            base.Start();
            agent.speed = patrolSpeed;
        }
        
        protected override void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            
            agent.speed = patrolSpeed;
            
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                waitTimer += Time.deltaTime;
                
                if (waitTimer >= waitTime)
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
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
                agent.SetDestination(lastKnownPlayerPosition);
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
        
        protected override void Chase()
        {
            agent.speed = chaseSpeed;
            
            if (playerTarget != null)
            {
                agent.SetDestination(playerTarget.Position);
                lastKnownPlayerPosition = playerTarget.Position;
            }
            
            // If we lose sight of the player, start searching
            if (detectionLevel < 0.8f)
            {
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
                // Move to a random point around the last known position
                Vector3 randomDirection = Random.insideUnitSphere * 5f;
                randomDirection += lastKnownPlayerPosition;
                randomDirection.y = transform.position.y;
                
                if (Physics.Raycast(randomDirection + Vector3.up * 2f, Vector3.down, 3f))
                {
                    agent.SetDestination(randomDirection);
                }
            }
            
            if (searchTimer >= searchDuration)
            {
                ChangeState(EnemyState.Returning);
                searchTimer = 0f;
            }
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
            
            agent.SetDestination(nearestPatrolPoint);
            
            if (!agent.pathPending && agent.remainingDistance < 1f)
            {
                currentPatrolIndex = nearestIndex;
                ChangeState(EnemyState.Patrolling);
            }
        }
    }
}

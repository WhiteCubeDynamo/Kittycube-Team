using System.Collections.Generic;
using UnityEngine;

namespace StealthHeist.Enemies
{
    /// <summary>
    /// Waypoint system for guard patrol routes
    /// </summary>
    public class WaypointSystem : MonoBehaviour
    {
        [Header("Waypoint Settings")]
        public List<Transform> waypoints = new List<Transform>();
        public bool isLooping = true;
        public bool reverseOnEnd = false;
        public float waitTimeAtWaypoint = 2f;
        
        [Header("Debug")]
        public bool showGizmos = true;
        public Color waypointColor = Color.yellow;
        public Color pathColor = Color.green;
        
        private void OnDrawGizmos()
        {
            if (!showGizmos || waypoints.Count < 2) return;
            
            // Draw waypoints
            Gizmos.color = waypointColor;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
                    
                    // Draw waypoint number
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(waypoints[i].position + Vector3.up, i.ToString());
#endif
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
        
        public Transform GetWaypoint(int index)
        {
            if (waypoints.Count == 0) return null;
            return waypoints[Mathf.Clamp(index, 0, waypoints.Count - 1)];
        }
        
        public int GetNextWaypointIndex(int currentIndex, bool isReversing)
        {
            if (waypoints.Count == 0) return 0;
            
            if (isReversing)
            {
                int nextIndex = currentIndex - 1;
                if (nextIndex < 0)
                {
                    return reverseOnEnd ? waypoints.Count - 1 : 0;
                }
                return nextIndex;
            }
            else
            {
                int nextIndex = currentIndex + 1;
                if (nextIndex >= waypoints.Count)
                {
                    return isLooping ? 0 : (reverseOnEnd ? waypoints.Count - 1 : waypoints.Count - 1);
                }
                return nextIndex;
            }
        }
        
        public Vector3 GetWaypointPosition(int index)
        {
            Transform waypoint = GetWaypoint(index);
            return waypoint != null ? waypoint.position : transform.position;
        }
        
        public int GetClosestWaypointIndex(Vector3 position)
        {
            if (waypoints.Count == 0) return 0;
            
            int closestIndex = 0;
            float closestDistance = Vector3.Distance(position, waypoints[0].position);
            
            for (int i = 1; i < waypoints.Count; i++)
            {
                float distance = Vector3.Distance(position, waypoints[i].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }
            
            return closestIndex;
        }
        
        public int WaypointCount => waypoints.Count;
    }
}

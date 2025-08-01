using UnityEngine;

namespace StealthHeist.Environment
{
    /// <summary>
    /// Example script showing how to connect pressure plates to sliding doors.
    /// This demonstrates the basic setup and can be used as a reference.
    /// </summary>
    public class PressurePlateExample : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("The pressure plate that triggers the door")]
        [SerializeField] private PressurePlate pressurePlate;
        
        [Tooltip("The sliding door to control")]
        [SerializeField] private SlidingDoor slidingDoor;
        
        [Header("Example Settings")]
        [Tooltip("Should the door close when the plate is released?")]
        [SerializeField] private bool closeOnRelease = true;
        
        private void Start()
        {
            // Automatically find components if not assigned
            if (pressurePlate == null)
                pressurePlate = GetComponent<PressurePlate>();
                
            if (slidingDoor == null)
                slidingDoor = GetComponent<SlidingDoor>();
            
            // Subscribe to pressure plate events
            if (pressurePlate != null)
            {
                pressurePlate.OnPlatePressed.AddListener(OnPlatePressed);
                pressurePlate.OnPlateReleased.AddListener(OnPlateReleased);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (pressurePlate != null)
            {
                pressurePlate.OnPlatePressed.RemoveListener(OnPlatePressed);
                pressurePlate.OnPlateReleased.RemoveListener(OnPlateReleased);
            }
        }
        
        private void OnPlatePressed()
        {
            Debug.Log("Pressure plate pressed - opening door");
            if (slidingDoor != null)
                slidingDoor.OpenDoor();
        }
        
        private void OnPlateReleased()
        {
            if (closeOnRelease)
            {
                Debug.Log("Pressure plate released - closing door");
                if (slidingDoor != null)
                    slidingDoor.CloseDoor();
            }
        }
    }
}

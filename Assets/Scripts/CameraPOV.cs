using UnityEngine;

public class CameraPOV : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 15, 0); // Y-offset for top-down view

    void Start()
    {
        // Set the camera to look straight down
        // transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // Update camera position to follow the player
            transform.position = player.position + offset;
        }
    }
}

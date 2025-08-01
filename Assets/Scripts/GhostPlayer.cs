using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostPlayer : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float ghostAlpha = 0.5f;
    public Color ghostColor = Color.blue;
    
    private List<PlayerPositionRecord> recordedMovements;
    private int currentRecordIndex = 0;
    private float playbackStartTime;
    private bool isPlaying = false;
    
    private Renderer[] renderers;
    private Animator animator;
    private Transform heldItemTransform;
    
    void Start()
    {
        // Get all renderers and make them semi-transparent
        renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null)
            {
                // Create a new material instance to avoid affecting other objects
                Material ghostMaterial = new Material(renderer.material);
                
                // Make it transparent
                if (ghostMaterial.HasProperty("_Color"))
                {
                    Color color = ghostMaterial.color;
                    color.a = ghostAlpha;
                    color = Color.Lerp(color, ghostColor, 0.3f);
                    ghostMaterial.color = color;
                }
                
                // Set rendering mode to transparent if possible
                if (ghostMaterial.HasProperty("_Mode"))
                {
                    ghostMaterial.SetInt("_Mode", 3); // Transparent mode
                    ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    ghostMaterial.SetInt("_ZWrite", 0);
                    ghostMaterial.DisableKeyword("_ALPHATEST_ON");
                    ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
                    ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    ghostMaterial.renderQueue = 3000;
                }
                
                renderer.material = ghostMaterial;
            }
        }
        
        // Get animator for animation playback
        animator = GetComponent<Animator>();
        
        // Disable any player controller scripts
        PhysicalPlayerController playerController = GetComponent<PhysicalPlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Disable rigidbody physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Disable colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }
    
    public void Initialize(List<PlayerPositionRecord> movements)
    {
        recordedMovements = new List<PlayerPositionRecord>(movements);
        currentRecordIndex = 0;
        playbackStartTime = Time.time;
        isPlaying = true;
        
        Debug.Log($"Ghost player initialized with {recordedMovements.Count} recorded movements");
    }
    
    void Update()
    {
        if (!isPlaying || recordedMovements == null || recordedMovements.Count == 0)
            return;
        
        float currentPlaybackTime = Time.time - playbackStartTime;
        
        // Find the appropriate record to play back
        while (currentRecordIndex < recordedMovements.Count - 1 && 
               recordedMovements[currentRecordIndex + 1].timestamp <= currentPlaybackTime)
        {
            currentRecordIndex++;
        }
        
        if (currentRecordIndex >= recordedMovements.Count)
        {
            // Playback finished
            isPlaying = false;
            return;
        }
        
        PlayerPositionRecord currentRecord = recordedMovements[currentRecordIndex];
        
        // Apply position and rotation
        transform.position = currentRecord.position;
        transform.rotation = currentRecord.rotation;
        
        // Apply animation states if animator exists
        if (animator != null)
        {
            animator.SetBool("IsCrouching", currentRecord.isCrouching);
            animator.SetBool("IsRunning", currentRecord.isRunning);
            
            // Calculate movement for blend tree (approximate)
            if (currentRecordIndex > 0)
            {
                Vector3 movement = currentRecord.position - recordedMovements[currentRecordIndex - 1].position;
                Vector3 localMovement = transform.InverseTransformDirection(movement);
                
                float speed = movement.magnitude / 0.1f; // Assuming 0.1s between records
                animator.SetFloat("Speed", speed);
                animator.SetFloat("MoveX", localMovement.x * 10f);
                animator.SetFloat("MoveZ", localMovement.z * 10f);
                animator.SetBool("IsMoving", speed > 0.1f);
            }
        }
        
        // Handle held items
        HandleHeldItem(currentRecord.heldItem);
    }
    
    private void HandleHeldItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            // No item should be held
            if (heldItemTransform != null)
            {
                Destroy(heldItemTransform.gameObject);
                heldItemTransform = null;
            }
        }
        else
        {
            // Item should be held
            if (heldItemTransform == null || heldItemTransform.name != itemName)
            {
                // Create or replace held item
                if (heldItemTransform != null)
                {
                    Destroy(heldItemTransform.gameObject);
                }
                
                CreateGhostItem(itemName);
            }
        }
    }
    
    private void CreateGhostItem(string itemName)
    {
        // Try to find the original item in the scene to copy its appearance
        GameObject originalItem = GameObject.Find(itemName);
        if (originalItem != null)
        {
            // Create a ghost version of the item
            GameObject ghostItem = Instantiate(originalItem);
            ghostItem.name = itemName + "_Ghost";
            
            // Find hand transform (similar to player)
            Transform hand = FindChildByName(transform, "Hand");
            if (hand == null)
            {
                // Create a simple hand position if not found
                GameObject handObj = new GameObject("Hand");
                handObj.transform.parent = transform;
                handObj.transform.localPosition = new Vector3(0.5f, 1.0f, 0.5f);
                hand = handObj.transform;
            }
            
            ghostItem.transform.parent = hand;
            ghostItem.transform.localPosition = Vector3.zero;
            ghostItem.transform.localRotation = Quaternion.identity;
            
            // Make it ghostly
            Renderer[] itemRenderers = ghostItem.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in itemRenderers)
            {
                if (renderer.material != null)
                {
                    Material ghostMaterial = new Material(renderer.material);
                    Color color = ghostMaterial.color;
                    color.a = ghostAlpha;
                    color = Color.Lerp(color, ghostColor, 0.5f);
                    ghostMaterial.color = color;
                    renderer.material = ghostMaterial;
                }
            }
            
            // Disable physics and colliders
            Rigidbody itemRb = ghostItem.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.isKinematic = true;
            }
            
            Collider[] itemColliders = ghostItem.GetComponentsInChildren<Collider>();
            foreach (Collider col in itemColliders)
            {
                col.enabled = false;
            }
            
            heldItemTransform = ghostItem.transform;
        }
    }
    
    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains(name))
            {
                return child;
            }
        }
        return null;
    }
    
    void OnDestroy()
    {
        if (heldItemTransform != null)
        {
            Destroy(heldItemTransform.gameObject);
        }
    }
}

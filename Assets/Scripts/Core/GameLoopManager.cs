using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using StealthHeist.Core.Enums;
using StealthHeist.Player;

[System.Serializable]
public class PlayerPositionRecord
{
    public Vector3 position;
    public Quaternion rotation;
    public float timestamp;
    public bool isCrouching;
    public bool isRunning;
    public string heldItem;
}

public class GameLoopManager : MonoBehaviour
{
    [Header("Game Loop Settings")]
    public float loopTime = 60f; // Loop duration in seconds
    public Vector3 playerStartPosition;
    public Quaternion playerStartRotation;
    
    [Header("Stealable Items")]
    public List<string> StealableItems;
    public List<string> CollectedItems;
    public List<string> InitialStealableItems;
    
    [Header("Player References")]
    public PhysicalPlayerController player;
    public GameObject ghostPlayerPrefab;
    
    [Header("UI Elements")]
    public Text timeLeftText;
    public Text loopCountText;
    
    [Header("Scene Management")]
    public string itemsSceneName; // Scene containing dynamic items to reload
    // public string itemsContainerTag = "ItemsContainer"; // Tag for objects that should be reset
    
    // Async scene loading setup
    bool itemsSceneLoaded = false;
    
    // Private fields
    private float currentTime;
    private int loopCount = 0;
    private bool gameWon = false;
    private List<PlayerPositionRecord> currentLoopRecording = new List<PlayerPositionRecord>();
    private List<List<PlayerPositionRecord>> previousLoopRecordings = new List<List<PlayerPositionRecord>>();
    private List<GameObject> ghostPlayers = new List<GameObject>();
    private Coroutine recordingCoroutine;
    private Scene loadedItemsScene;
    private List<GameObject> originalItems = new List<GameObject>();
    
    void Start()
    {
        InitialStealableItems = new List<string>(StealableItems);
        CollectedItems = new List<string>();
        
        // Store initial player position
        playerStartPosition = player.transform.position;
        playerStartRotation = player.transform.rotation;
        
        // Get scene name for reloading items
        // Async loading setup
        // if (string.IsNullOrEmpty(itemsSceneName))
        // {
        //     itemsSceneName = SceneManager.GetActiveScene().name + "_Items";
        // }
        
        // Store original item states
        StoreOriginalItemStates();
        
        StartLoop();
    }
    
    void Update()
    {
        if (gameWon) return;
        
        currentTime -= Time.deltaTime;
        
        // Update UI
        if (timeLeftText != null)
        {
            timeLeftText.text = $"Time Left: {Mathf.Max(0, currentTime):F1}s";
        }
        
        if (loopCountText != null)
        {
            loopCountText.text = $"Loop: {loopCount}";
        }
        
        // Check if time is up
        if (currentTime <= 0)
        {
            ResetLoop();
        }
    }
    
    private void StoreOriginalItemStates()
    {
        // Find objects by name from stealable items list
        foreach (string itemName in StealableItems)
        {
            GameObject item = GameObject.Find(itemName);
            if (item != null && !originalItems.Contains(item))
            {
                originalItems.Add(item);
            }
        }
        
        // To use tags instead:
        // GameObject[] taggedItems = GameObject.FindGameObjectsWithTag("ItemsContainer");
        // foreach (GameObject obj in taggedItems)
        // {
        //     originalItems.Add(obj);
        // }
        
        Debug.Log($"Stored {originalItems.Count} original item states");
    }
    
    // Async loading logic
    private IEnumerator LoadItemsScene()
    {
        Debug.Log($"Loading items scene: {itemsSceneName}");

        // Load the items scene additively
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(itemsSceneName, LoadSceneMode.Additive);

        yield return loadOperation;

        // Store reference to the loaded scene
        loadedItemsScene = SceneManager.GetSceneByName(itemsSceneName);

        if (loadedItemsScene.IsValid())
        {
            itemsSceneLoaded = true;
            Debug.Log($"Items scene {itemsSceneName} loaded successfully");
        }
        else
        {
            Debug.LogError($"Failed to load items scene: {itemsSceneName}");
        }
    }
    
    private void StartLoop()
    {

        // Load the items scene additively
        StartCoroutine(LoadItemsScene());
        currentTime = loopTime;
        loopCount++;
        
        // Start recording player movements
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
        }
        recordingCoroutine = StartCoroutine(RecordPlayerMovement());
        
        Debug.Log($"Starting loop {loopCount}");
    }
    
    private void ResetLoop()
    {
        Debug.Log($"Resetting loop {loopCount}");
        
        // Stop current recording
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
        }
        
        // Store current loop recording
        if (currentLoopRecording.Count > 0)
        {
            previousLoopRecordings.Add(new List<PlayerPositionRecord>(currentLoopRecording));
            currentLoopRecording.Clear();
        }
        
        // Reset stealable items
        StealableItems = new List<string>(InitialStealableItems);
        
        // Don't clear CollectedItems - keep them across loops for win condition
        
        // Reset dynamic items instead of reloading entire scene
        StartCoroutine(ResetDynamicItems());

        // Continue with post-reset setup without scene reload
        PostItemResetSetup();
    }

    private IEnumerator ResetDynamicItems()
    {
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(itemsSceneName);
        while (!asyncUnload.isDone)
        {
            yield return null;
        }

        // Debug.Log("Resetting dynamic items for loop reset...");
        
        // Reset items to their original states
        // ResetItemsToOriginalState();
        
        // Debug.Log("Dynamic items reset successfully");
        
    }
    
    // Async scene reloading logic
    // private IEnumerator ResetByReloadingItemsScene()
    // {
    //     // Unload the current items scene
    //     if (loadedItemsScene.IsValid())
    //     {
    //         Debug.Log($"Unloading items scene: {itemsSceneName}");
    //         yield return SceneManager.UnloadSceneAsync(loadedItemsScene);
    //         Debug.Log($"Items scene {itemsSceneName} unloaded successfully");
    //     }
    // 
    //     // Reset state
    //     itemsSceneLoaded = false;
    // 
    //     // Load the items scene again
    //     yield return StartCoroutine(LoadItemsScene());
    // }
    
    
    
    
    private void ResetItemsToOriginalState()
    {
        Debug.Log("Resetting items to original state (fallback method)");
        
        // Recreate items from stored original states
        foreach (GameObject originalItem in originalItems)
        {
            if (originalItem != null)
            {
                GameObject resetItem = Instantiate(originalItem);
                resetItem.name = originalItem.name.Replace("(Clone)", ""); // Remove (Clone) suffix
                Debug.Log($"Reset item: {resetItem.name}");
            }
        }
    }
    
    private void PostItemResetSetup()
    {
        // Reset player position and state
        ResetPlayer();
        
        // Spawn ghost players for previous loops
        SpawnGhostPlayers();
        
        // Start the new loop
        StartLoop();
    }
    
    private void ResetPlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PhysicalPlayerController>();
        }
        
        if (player == null) return;
        
        // Reset position and rotation
        player.transform.position = playerStartPosition;
        player.transform.rotation = playerStartRotation;
        
        // Reset physics
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }
        
        // Reset stealth state
        PlayerStealth playerStealth = player.GetComponent<PlayerStealth>();
        if (playerStealth != null)
        {
            playerStealth.SetCrouching(false);
        }
        
        // Drop any held items
        if (player.collectedItem != null)
        {
            player.DetachItem();
        }
        
        Debug.Log("Player reset to starting position");
    }
    
    private IEnumerator RecordPlayerMovement()
    {
        while (currentTime > 0 && !gameWon)
        {
            if (player != null)
            {
                PlayerPositionRecord record = new PlayerPositionRecord
                {
                    position = player.transform.position,
                    rotation = player.transform.rotation,
                    timestamp = loopTime - currentTime,
                    isCrouching = player.IsCrouching,
                    isRunning = player.IsRunning,
                    heldItem = player.collectedItem != null ? player.collectedItem.name : ""
                };
                
                currentLoopRecording.Add(record);
            }
            
            yield return new WaitForSeconds(0.1f); // Record 10 times per second
        }
    }
    
    private void SpawnGhostPlayers()
    {
        // Clean up existing ghost players
        foreach (GameObject ghost in ghostPlayers)
        {
            if (ghost != null)
            {
                Destroy(ghost);
            }
        }
        ghostPlayers.Clear();
        
        // Spawn ghost players for each previous loop
        for (int i = 0; i < previousLoopRecordings.Count; i++)
        {
            if (ghostPlayerPrefab != null && previousLoopRecordings[i].Count > 0)
            {
                GameObject ghost = Instantiate(ghostPlayerPrefab, playerStartPosition, playerStartRotation);
                ghost.name = $"GhostPlayer_Loop{i + 1}";
                
                // Start ghost playback
                GhostPlayer ghostComponent = ghost.GetComponent<GhostPlayer>();
                if (ghostComponent == null)
                {
                    ghostComponent = ghost.AddComponent<GhostPlayer>();
                }
                
                ghostComponent.Initialize(previousLoopRecordings[i]);
                ghostPlayers.Add(ghost);
            }
        }
    }
    
    public bool AttemptEscape(string carriedItem)
    {
        // Check if the carried item is one of the stealable items
        bool isValidItem = false;
        foreach (string stealableItem in InitialStealableItems)
        {
            if (carriedItem.Contains(stealableItem))
            {
                isValidItem = true;
                break;
            }
        }
        
        if (!isValidItem)
        {
            Debug.Log($"AttemptEscape failed: {carriedItem} is not a valid stealable item");
            return false;
        }
        
        // Add item to collected items if not already collected
        string itemName = "";
        foreach (string stealableItem in InitialStealableItems)
        {
            if (carriedItem.Contains(stealableItem))
            {
                itemName = stealableItem;
                break;
            }
        }
        
        if (!CollectedItems.Contains(itemName))
        {
            CollectedItems.Add(itemName);
            StealableItems.Remove(itemName);
            Debug.Log($"Successfully stole: {itemName}. Items remaining: {StealableItems.Count}");
        }
        
        // Check if all items have been collected
        if (CollectedItems.Count >= InitialStealableItems.Count)
        {
            Win();
            return true;
        }
        else
        {
            Debug.Log($"Escape successful with {itemName}, but still need {StealableItems.Count} more items.");
            Debug.Log($"Still need: {string.Join(", ", StealableItems)}");
            ResetLoop(); // Continue to next loop
            return true; // Return true because the escape was valid, just not complete
        }
    }
    
    private void Win()
    {
        gameWon = true;
        Debug.Log($"Victory! Completed in {loopCount} loops!");
        
        // Stop recording
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
        }
        
        // Here you could trigger victory UI, scene transition, etc.
        if (timeLeftText != null)
        {
            timeLeftText.text = "VICTORY!";
        }
    }
    
    // Public method called when player is caught by guard
    public void OnPlayerCaught()
    {
        Debug.Log("Player was caught by guard! Resetting loop...");
        
        // Update UI to show caught status
        if (timeLeftText != null)
        {
            timeLeftText.text = "CAUGHT!";
        }
        
        // Reset the loop immediately (same as timeout)
        ResetLoop();
    }
    
    // Public method to get current loop info
    public int GetCurrentLoop()
    {
        return loopCount;
    }
    
    public float GetTimeLeft()
    {
        return currentTime;
    }
}

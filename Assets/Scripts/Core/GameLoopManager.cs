using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using StealthHeist.Core.Interfaces;
using StealthHeist.Inventory;

namespace StealthHeist.Core
{
    /// <summary>
    /// Manages the main game loop, including the timer, win/lose conditions,
    /// and persistence of object states between loops.
    /// </summary>
    public class GameLoopManager : MonoBehaviour
    {
        public static GameLoopManager Instance { get; private set; }

        [Header("Loop Settings")]
        [SerializeField] private float _loopDurationSeconds = 60f;

        [Header("Win/Lose Conditions")]
        [SerializeField] private string _requiredItemName = "MainJewel"; // The item needed to win.
        [SerializeField] private string _winSceneName = "WinScreen";
        [SerializeField] private string _loseSceneName = "LoseScreen"; // Optional: if you want a final game over.

        [Header("UI (Optional)")]
        [SerializeField] private UnityEngine.UI.Text _timerText;
        [SerializeField] private UnityEngine.UI.Text _loopStatusText;

        private float _currentTimer;
        private bool _isLoopActive = true;

        // A static dictionary to hold object states between scene reloads.
        private static Dictionary<string, object> _persistentStates = new Dictionary<string, object>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to scene loaded event to restore state after a reload.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            StartNewLoop();
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerCaught += HandlePlayerCaught;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerCaught -= HandlePlayerCaught;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (!_isLoopActive) return;

            _currentTimer -= Time.deltaTime;
            if (_timerText != null)
            {
                _timerText.text = $"Time: {_currentTimer:F1}s";
            }

            if (_currentTimer <= 0)
            {
                RestartLoop("Time's up!");
            }
        }

        private void StartNewLoop()
        {
            _currentTimer = _loopDurationSeconds;
            _isLoopActive = true;
            if (_loopStatusText != null) _loopStatusText.text = "";
        }

        private void HandlePlayerCaught()
        {
            RestartLoop("You were caught!");
        }

        public void AttemptEscape()
        {
            if (InventoryManager.Instance.HasItem(_requiredItemName))
            {
                WinGame();
            }
            else
            {
                Debug.Log("Escape attempt failed: Missing the required item.");
                if (_loopStatusText != null) _loopStatusText.text = "You need the main jewel to escape!";
            }
        }

        private void WinGame()
        {
            _isLoopActive = false;
            Debug.Log("YOU WIN! You escaped with the loot!");
            // Clear persistence so a new game starts fresh.
            _persistentStates.Clear();
            SceneManager.LoadScene(_winSceneName);
        }

        public void RestartLoop(string reason)
        {
            if (!_isLoopActive) return;

            _isLoopActive = false;
            Debug.Log($"LOOP RESTARTING: {reason}");
            if (_loopStatusText != null) _loopStatusText.text = reason;

            // Capture the state of all persistent objects before reloading.
            CaptureAllStates();

            // Reload the current scene after a short delay.
            Invoke(nameof(ReloadScene), 2f);
        }

        private void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void CaptureAllStates()
        {
            var persistentObjects = FindObjectsOfType<MonoBehaviour>(true).OfType<IPersistent>();
            foreach (var p in persistentObjects)
            {
                _persistentStates[p.PersistenceID] = p.CaptureState();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // After the scene reloads, find all persistent objects and restore their state.
            var persistentObjects = FindObjectsOfType<MonoBehaviour>(true).OfType<IPersistent>();
            foreach (var p in persistentObjects)
            {
                if (_persistentStates.TryGetValue(p.PersistenceID, out object state))
                {
                    p.RestoreState(state);
                }
            }

            // Start the new loop's logic.
            StartNewLoop();
        }
    }
}
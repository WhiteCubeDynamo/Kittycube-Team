using UnityEngine;
using StealthHeist.Core.Interfaces;
using StealthHeist.Core.Enums;
using StealthHeist.Inventory;

namespace StealthHeist.Environment
{
    /// <summary>
    /// A component for objects in the world that can be stolen by the player.
    /// Implements both IStealable (to define its properties) and IInteractable (to be picked up).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StealableObject : MonoBehaviour, IStealable, IInteractable, IPersistent
    {
        [Header("Persistence")]
        [SerializeField] private string _persistenceID;

        [Header("Item Properties")]
        [SerializeField] private string _itemName = "Priceless Vase";
        [SerializeField] private ArtifactType _artifactType = ArtifactType.Sculpture;
        [SerializeField] private int _value = 1000;
        [SerializeField] private float _weight = 5.0f;
        [SerializeField] private Sprite _icon;

        [Header("Interaction")]
        [SerializeField] private string _pickupText = "Steal";

        #region IStealable Implementation

        public string Name => _itemName;
        public ArtifactType Type => _artifactType;
        public int Value => _value;
        public float Weight => _weight;
        public bool IsStolen { get; set; } = false;
        public Sprite Icon => _icon;

        public void OnPickup()
        {
            IsStolen = true;
            // Hide the object from the world once it's picked up.
            gameObject.SetActive(false);
        }

        public bool CanBeStolen()
        {
            return !IsStolen;
        }

        #endregion

        #region IInteractable Implementation

        public string InteractionText => $"{_pickupText} {Name}";
        public bool CanInteract => CanBeStolen();

        public void Interact()
        {
            if (!CanInteract) return;

            // Try to add this item to the player's inventory.
            bool wasAdded = InventoryManager.Instance.AddItem(this);

            if (wasAdded)
            {
                Debug.Log($"Picked up {Name}");
                OnPickup();
            }
            else
            {
                Debug.LogWarning($"Could not pick up {Name}. Inventory might be full or overweight.");
                // Here you could trigger a UI notification to the player.
            }
        }

        public void OnHighlight() { /* Optional: Add a visual effect like an outline. */ }
        public void OnUnhighlight() { /* Optional: Remove the visual effect. */ }

        #endregion

        #region IPersistent Implementation

        public string PersistenceID => _persistenceID;

        public object CaptureState()
        {
            return IsStolen;
        }

        public void RestoreState(object state)
        {
            IsStolen = (bool)state;
            gameObject.SetActive(!IsStolen);
        }

        #endregion
    }
}

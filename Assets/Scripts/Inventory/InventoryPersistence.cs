using System.Collections.Generic;
using UnityEngine;
using StealthHeist.Core.Interfaces;

namespace StealthHeist.Inventory
{
    /// <summary>
    /// Handles persistence of the inventory state across game loops.
    /// This component should be attached to the same GameObject as InventoryManager.
    /// </summary>
    public class InventoryPersistence : MonoBehaviour, IPersistent
    {
        [Header("Persistence")]
        [SerializeField] private string _persistenceID = "PlayerInventory";
        
        private InventoryManager _inventoryManager;

        #region IPersistent Implementation

        public string PersistenceID => _persistenceID;

        public object CaptureState()
        {
            if (_inventoryManager == null || _inventoryManager.Items == null)
                return null;

            var state = new InventoryPersistentState
            {
                items = new List<ItemData>()
            };

            foreach (var item in _inventoryManager.Items)
            {
                state.items.Add(new ItemData
                {
                    name = item.name,
                    type = item.type,
                    value = item.value,
                    weight = item.weight,
                    quantity = item.quantity,
                    isStackable = item.isStackable,
                    // Note: We can't serialize the Sprite icon directly, 
                    // so we'd need to handle that differently if needed
                });
            }

            return state;
        }

        public void RestoreState(object state)
        {
            if (state is InventoryPersistentState inventoryState && inventoryState.items != null)
            {
                // Clear current inventory
                _inventoryManager.ClearInventory();

                // Restore each item
                foreach (var itemData in inventoryState.items)
                {
                    // Create a temporary StealableObject-like structure to add to inventory
                    // This is a workaround since InventoryManager expects IStealable
                    var restoredItem = new RestoredInventoryItem(itemData);
                    
                    for (int i = 0; i < itemData.quantity; i++)
                    {
                        _inventoryManager.AddItem(restoredItem);
                    }
                }
            }
        }

        #endregion

        private void Awake()
        {
            _inventoryManager = GetComponent<InventoryManager>();
            if (_inventoryManager == null)
            {
                Debug.LogError("InventoryPersistence requires InventoryManager on the same GameObject!");
            }
        }

        [System.Serializable]
        private class InventoryPersistentState
        {
            public List<ItemData> items;
        }

        [System.Serializable]
        private class ItemData
        {
            public string name;
            public Core.Enums.ArtifactType type;
            public int value;
            public float weight;
            public int quantity;
            public bool isStackable;
        }

        /// <summary>
        /// A temporary implementation of IStealable to restore inventory items
        /// </summary>
        private class RestoredInventoryItem : IStealable
        {
            private ItemData _data;

            public RestoredInventoryItem(ItemData data)
            {
                _data = data;
            }

            public string Name => _data.name;
            public Core.Enums.ArtifactType Type => _data.type;
            public int Value => _data.value;
            public float Weight => _data.weight;
            public bool IsStolen { get; set; } = true; // Already in inventory
            public Sprite Icon => null; // Would need asset reference system to restore

            public void OnPickup() { }
            public bool CanBeStolen() => false;
        }
    }
}

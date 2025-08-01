using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using StealthHeist.Core.Interfaces;
using StealthHeist.Core;
using StealthHeist.Core.Enums;

namespace StealthHeist.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private int maxSlots = 20;
        [SerializeField] private float maxWeight = 50f;
        [SerializeField] private UIDocument inventoryUI;
        
        public event Action<InventoryItem> OnItemAdded;
        public event Action<InventoryItem> OnItemRemoved;
        public event Action<float, float> OnWeightChanged;
        public event Action<int> OnValueChanged;
        
        private List<InventoryItem> items = new List<InventoryItem>();
        private VisualElement rootElement;
        private ScrollView itemContainer;
        private Label weightLabel;
        private Label valueLabel;
        private Label slotsLabel;

        private float _currentWeight;
        private int _currentValue;
        
        public static InventoryManager Instance { get; private set; }
        public List<InventoryItem> Items => items;
        public float CurrentWeight => _currentWeight;
        public int CurrentValue => _currentValue;
        public int UsedSlots => items.Count;
        public bool IsFull => UsedSlots >= maxSlots;
        public bool IsOverweight => CurrentWeight > maxWeight;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (inventoryUI != null)
            {
                rootElement = inventoryUI.rootVisualElement;
                SetupUI();
            }
        }
        
        private void SetupUI()
        {
            itemContainer = rootElement.Q<ScrollView>("item-container");
            weightLabel = rootElement.Q<Label>("weight-label");
            valueLabel = rootElement.Q<Label>("value-label");
            slotsLabel = rootElement.Q<Label>("slots-label");
            
            UpdateUI();
        }
        
        public bool AddItem(IStealable stealable)
        {
            if (IsFull)
            {
                Debug.LogWarning("Inventory is full!");
                return false;
            }
            
            // Check if we already have this item and it's stackable
            var existingItem = items.FirstOrDefault(item => 
                item.name == stealable.Name && item.isStackable);
            
            if (existingItem != null)
            {
                existingItem.AddQuantity(1);
            }
            else
            {
                var newItem = new InventoryItem(stealable);
                items.Add(newItem);
                GameEvents.TriggerItemAdded(newItem);
                OnItemAdded?.Invoke(newItem);
            }
            NotifyInventoryChanged();
            
            return true;
        }
        
        public bool RemoveItem(string itemName, int quantity = 1)
        {
            var item = items.FirstOrDefault(i => i.name == itemName);
            if (item == null) return false;
            
            bool itemStillExists = item.RemoveQuantity(quantity);
            
            if (!itemStillExists)
            {
                items.Remove(item);
                GameEvents.TriggerItemRemoved(item.name, item.quantity);
                OnItemRemoved?.Invoke(item);
            }
            NotifyInventoryChanged();
            
            return true;
        }
        
        public bool HasItem(string itemName, int requiredQuantity = 1)
        {
            var item = items.FirstOrDefault(i => i.name == itemName);
            return item != null && item.quantity >= requiredQuantity;
        }
        
        public bool HasKeyForType(ArtifactType keyType)
        {
            return items.Any(item => item.type == ArtifactType.Key && item.name.Contains(keyType.ToString()));
        }
        
        public void ClearInventory()
        {
            items.Clear();
            NotifyInventoryChanged();
        }

        private void NotifyInventoryChanged()
        {
            _currentWeight = items.Sum(item => item.GetTotalWeight());
            _currentValue = items.Sum(item => item.GetTotalValue());

            OnWeightChanged?.Invoke(_currentWeight, maxWeight);
            OnValueChanged?.Invoke(_currentValue);
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (rootElement == null) return;
            
            // Clear existing items
            itemContainer?.Clear();
            
            // Add items to UI
            foreach (var item in items)
            {
                var itemElement = CreateItemElement(item);
                itemContainer?.Add(itemElement);
            }
            
            // Update labels
            if (weightLabel != null)
            {
                weightLabel.text = $"Weight: {CurrentWeight:F1}/{maxWeight:F1}";
                weightLabel.style.color = IsOverweight ? Color.red : Color.white;
            }
            
            if (valueLabel != null)
            {
                valueLabel.text = $"Value: ${CurrentValue:N0}";
            }
            
            if (slotsLabel != null)
            {
                slotsLabel.text = $"Slots: {UsedSlots}/{maxSlots}";
                slotsLabel.style.color = IsFull ? Color.red : Color.white;
            }
        }
        
        private VisualElement CreateItemElement(InventoryItem item)
        {
            var itemElement = new VisualElement();
            itemElement.AddToClassList("inventory-item");
            
            // Item icon
            var icon = new VisualElement();
            icon.AddToClassList("item-icon");
            if (item.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(item.icon);
            }
            itemElement.Add(icon);
            
            // Item info
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("item-info");
            
            var nameLabel = new Label(item.name);
            nameLabel.AddToClassList("item-name");
            infoContainer.Add(nameLabel);
            
            if (item.quantity > 1)
            {
                var quantityLabel = new Label($"x{item.quantity}");
                quantityLabel.AddToClassList("item-quantity");
                infoContainer.Add(quantityLabel);
            }
            
            var valueLabel = new Label($"${item.GetTotalValue():N0}");
            valueLabel.AddToClassList("item-value");
            infoContainer.Add(valueLabel);
            
            var weightLabel = new Label($"{item.GetTotalWeight():F1}kg");
            weightLabel.AddToClassList("item-weight");
            infoContainer.Add(weightLabel);
            
            itemElement.Add(infoContainer);
            
            return itemElement;
        }
        
        public void ToggleInventory()
        {
            if (rootElement != null)
            {
                rootElement.style.display = rootElement.style.display == DisplayStyle.None ? 
                    DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        public void ShowInventory()
        {
            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.Flex;
            }
        }
        
        public void HideInventory()
        {
            if (rootElement != null)
            {
                rootElement.style.display = DisplayStyle.None;
            }
        }
    }
}

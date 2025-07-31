using UnityEngine;
using StealthHeist.Core.Interfaces;
using StealthHeist.Core.Enums;

namespace StealthHeist.Inventory
{
    [System.Serializable]
    public class InventoryItem
    {
        public string name;
        public ArtifactType type;
        public int value;
        public float weight;
        public Sprite icon;
        public int quantity;
        public bool isStackable;
        
        public InventoryItem(IStealable stealable)
        {
            name = stealable.Name;
            type = stealable.Type;
            value = stealable.Value;
            weight = stealable.Weight;
            icon = stealable.Icon;
            quantity = 1;
            isStackable = type == ArtifactType.Jewel || type == ArtifactType.Key;
        }
        
        public void AddQuantity(int amount)
        {
            if (isStackable)
            {
                quantity += amount;
            }
        }
        
        public bool RemoveQuantity(int amount)
        {
            if (quantity >= amount)
            {
                quantity -= amount;
                return quantity > 0;
            }
            return false;
        }
        
        public int GetTotalValue()
        {
            return value * quantity;
        }
        
        public float GetTotalWeight()
        {
            return weight * quantity;
        }
    }
}

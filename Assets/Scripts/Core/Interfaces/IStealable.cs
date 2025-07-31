using UnityEngine;
using StealthHeist.Core.Enums;

namespace StealthHeist.Core.Interfaces
{
    public interface IStealable
    {
        string Name { get; }
        ArtifactType Type { get; }
        int Value { get; }
        float Weight { get; }
        bool IsStolen { get; set; }
        Sprite Icon { get; }
        
        void OnPickup();
        bool CanBeStolen();
    }
}

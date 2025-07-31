using UnityEngine;
using StealthHeist.Core.Enums;

namespace StealthHeist.Core.Interfaces
{
    public interface IDetectable
    {
        Vector3 Position { get; }
        StealthState CurrentStealthState { get; }
        float NoiseLevel { get; }
        float VisibilityLevel { get; }
        
        void ChangeStealthState(StealthState newState);
        void MakeNoise(float level);
    }
}

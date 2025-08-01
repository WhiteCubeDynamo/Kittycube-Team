using UnityEngine;
using StealthHeist.Core.Enums;

namespace StealthHeist.Core.Interfaces
{
    public interface IDetectable
    {
        public Vector3 Position { get; }
        public StealthState CurrentStealthState { get; }
        public float NoiseLevel { get; }
        public float VisibilityLevel { get; }
        
        void ChangeStealthState(StealthState newState);
        void MakeNoise(float level);
    }
}

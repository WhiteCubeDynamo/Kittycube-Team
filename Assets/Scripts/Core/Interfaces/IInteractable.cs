using UnityEngine;

namespace StealthHeist.Core.Interfaces
{
    public interface IInteractable
    {
        string InteractionText { get; }
        bool CanInteract { get; }
        void Interact();
        void OnHighlight();
        void OnUnhighlight();
    }
}

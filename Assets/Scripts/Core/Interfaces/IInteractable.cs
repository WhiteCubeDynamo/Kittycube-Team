using UnityEngine;

namespace StealthHeist.Core.Interfaces
{
    public interface IInteractable
    {
        public string InteractionText { get; }
        public bool CanInteract { get; }
        void Interact();
        void OnHighlight();
        void OnUnhighlight();
    }
}

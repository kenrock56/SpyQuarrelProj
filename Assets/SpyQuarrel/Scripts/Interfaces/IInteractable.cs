using UnityEngine;

namespace SpyQuarrelRuntime
{
    public interface IInteractable
    {
        public PlayerRole RequiredRole { get; }
        
        Transform UIAnchorPoint => null;
        bool HoldInteraction { get; }

        float HoldInteractionTime { get; }

        string InteractName { get; }

        string InteractDescription { get; }

        bool IsInteractable { get; }

        bool IsWorldSpaceUI { get; }

        void OnInteractEnter(Interactor interactor);

        void OnInteractHover(Interactor interactor);

        void OnInteractExit(Interactor interactor);

        void Interact(Interactor interactor);
    }
}
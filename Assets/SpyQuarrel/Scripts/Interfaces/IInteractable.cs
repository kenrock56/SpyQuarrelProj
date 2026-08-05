using UnityEngine;

namespace SpyQuarrelRuntime
{
    public interface IInteractable
    {
        bool HoldInteraction { get; }

        float HoldInteractionTime { get; }

        string InteractName { get; }

        string InteractDescription { get; }

        bool IsInteractable { get; }

        bool IsWorldSpaceUI { get; }

        void OnInteractEnter(Interactor interactor)
        {
            Debug.Log("OnInteractEnter");
        }

        void OnInteractHover(Interactor interactor);

        void OnInteractExit(Interactor interactor);

        void Interact(Interactor interactor);
    }
}
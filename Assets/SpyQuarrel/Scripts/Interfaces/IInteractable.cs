using System;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public interface IInteractable
    {
        string InteractName { get; }
        string InteractDescription { get; }

        bool IsInteractable { get; }

        bool IsWorldSpaceUI { get; }

        void OnInteractEnter(Interactor interactor)
        {
            Debug.Log("OnInteractEnter");
        }

        void OnInteractHover(Interactor interactor)
        {
            Debug.Log("OnInteractHover");
        }

        void OnInteractExit(Interactor interactor)
        {
            Debug.Log("OnInteractExit");
        }

        void Interact(Interactor interactor)
        {
            Debug.Log("Interact");
        }
        
    }
}
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class InteractDoor : MonoBehaviour, IInteractable
    {
        public bool HoldInteraction { get; }
        public float HoldInteractionTime { get; }
        public string InteractName { get; }
        public string InteractDescription { get; }
        public bool IsInteractable { get; }
        public bool IsWorldSpaceUI { get; }
        public void OnInteractHover(Interactor interactor)
        {
            
        }

        public void OnInteractExit(Interactor interactor)
        {
            
        }

        public void Interact(Interactor interactor)
        {
            
        }


        void Start()
        {
        
        }

        
        void Update()
        {
        
        }

       
    }
}

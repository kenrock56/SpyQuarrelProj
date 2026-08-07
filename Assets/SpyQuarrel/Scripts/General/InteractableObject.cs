using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        public PlayerRole RequiredRole { get; set; } =  PlayerRole.None;
        public bool HoldInteraction { get; }
        public float HoldInteractionTime { get; }
        [field:SerializeField]public string InteractName { get; set; }
        [field:SerializeField]public string InteractDescription { get; set; }
        [field: SerializeField] public bool IsInteractable { get; set; } = true;
        public bool IsWorldSpaceUI { get; set; }  = false;


        [SerializeField]private bool _isSelected = false;
        

        public void OnInteractEnter(Interactor interactor)
        {
            _isSelected = true;
        }

        public void OnInteractHover(Interactor interactor)
        {
            
        }


        public void OnInteractExit(Interactor interactor)
        {
            _isSelected = false;
        }

        public void Interact(Interactor interactor)
        {
            Debug.Log("interact held");
        }
    }
}

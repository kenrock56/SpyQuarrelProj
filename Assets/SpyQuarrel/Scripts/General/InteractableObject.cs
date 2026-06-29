using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        [field:SerializeField]public string InteractName { get; set; }
        [field:SerializeField]public string InteractDescription { get; set; }
        [field: SerializeField] public bool IsInteractable { get; set; } = true;

        
        [SerializeField]private bool _isSelected = false;
        
        public void OnInteractEnter(Interactor interactor)
        {
            _isSelected = true;
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

using UnityEngine;
using UnityEngine.UIElements;


namespace SpyQuarrelRuntime 
{
    public class InteractUI : MonoBehaviour
    {
        private UIDocument _document;
        [SerializeField]private Interactor _interactor;

        #region UI Elements

        private VisualElement _interactRoot;
        private Label _interactName;
        private Label _interactDescription;
        private Label _interactButton;
        private ProgressBar _interactProgress;

        private bool _validUI = false;
        #endregion
        
        void Awake()
        {
            SetupInteractor();
            SetupUI();
        }

        void Start()
        {
            SetInteractable(null);
        }

        private void SetupInteractor()
        {
            var root = transform.root;

            var interact = root.GetComponentInChildren<Interactor>();

            Debug.Log("Looking for interactor");

            if (interact == null) return;

            _interactor = interact;

            Debug.Log("Interactor Successfuly found");

            _interactor.OnInteractableChanged += HandleInteractChange;
        }

        private void HandleInteractChange(IInteractable interactable)
        {
            SetInteractable(interactable);
        }

        private void SetupUI()
        {
            _document = GetComponent<UIDocument>();
            
            if(_document == null) return;

            var root = _document.rootVisualElement;
            
            _interactRoot = root.Q<VisualElement>("interact-root");
            _interactName = root.Q<Label>("interact-name");
            _interactDescription = root.Q<Label>("interact-description");
            _interactButton = root.Q<Label>("interact-button");
            _interactProgress = root.Q<ProgressBar>("interact-progress");
            
            if (_interactProgress != null)
            {
                _interactProgress.lowValue = 0;
                _interactProgress.highValue = 1;
            }
            
            _validUI = true;
        }

        private void SetInteractable(IInteractable interactable)
        {
            var validInteractable = interactable is { IsWorldSpaceUI: false };
            
            SetRootVisibility(validInteractable);
            
            if (!validInteractable)
            {
                ClearUI();
                return;
            }

            _interactName.text = interactable.InteractName;
            
            _interactDescription.text = interactable.InteractDescription;
            
            _interactButton.text = "Interact [E]";
            
            Debug.Log("Interacting " + interactable);
        }

        void Update()
        {
            if (!_validUI)return;
            
            if(_interactor == null) return;

            var interactable = _interactor.CurrentInteractable;
            
            if(interactable == null) return;
            
            if(interactable.IsWorldSpaceUI)return;

            var holdInteraction = interactable.HoldInteraction;
            
            SetProgressBarVisibility(holdInteraction);

            if (holdInteraction)
            {
                var progress = _interactor.HoldProgress;
            
                SetProgressValue(progress);
            }
            
        }

        private void ClearUI()
        {
            if(!_validUI)return;
            
            
            _interactName.text = "";
            _interactDescription.text = "";
            _interactButton.text = "SCRIPT [E]";
        }

        private void SetRootVisibility(bool show)
        {
            if (_interactRoot == null) return;
            _interactRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetProgressBarVisibility(bool show)
        {
            if(_interactProgress == null) return;
            _interactProgress.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetProgressValue(float progress)
        {
            if(_interactProgress == null) return;
            _interactProgress.value = progress;
        }
    }
}

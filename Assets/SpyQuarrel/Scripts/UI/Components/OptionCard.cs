using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    [UxmlElement]
    public partial class OptionCardElement : UxmlView
    {
  
        private string _title;
        private string _description;
        private Texture2D _imageDisplayTexture;

        public event Action OnClick;
        

        private Button _rootButton;
        private VisualElement _imageDisplay;
        private Label _mainLabel;
        private Label _descriptionLabel;

        protected override string AssetPath => "OptionCard";

       

        [UxmlAttribute]
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                SetTitle();
            }
        }

        [UxmlAttribute]
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                SetDescription();
            }
        }

        [UxmlAttribute]
        public Texture2D ImageDisplay
        {
            get => _imageDisplayTexture;
            set
            {
                _imageDisplayTexture = value;
                SetImage();
            }
        }
        

        protected override void OnInitialize()
        {
            InitElements();
            RegisterCallbacks();
            
            SetAll();
        }

        private void InitElements()
        {
            _rootButton = GetElement<Button>("option-card-button");
            _imageDisplay = GetElement<VisualElement>("image-display");
            _mainLabel = GetElement<Label>("option-main-label");
            _descriptionLabel = GetElement<Label>("description-label");
        }

        private void RegisterCallbacks()
        {
            if (_rootButton != null)
                _rootButton.clicked += InvokeClick;
        }

        private void InvokeClick()
        {
            OnClick?.Invoke();
        }
        

        private void SetAll()
        {
            SetTitle();
            SetDescription();
            SetImage();
        }

        private void SetTitle()
        {
            if (_mainLabel != null)
                _mainLabel.text = _title;
        }

        private void SetDescription()
        {
            if (_descriptionLabel != null)
                _descriptionLabel.text = _description;
        }

        private void SetImage()
        {
            if (_imageDisplay != null && _imageDisplayTexture != null)
                _imageDisplay.style.backgroundImage = new StyleBackground(_imageDisplayTexture);
        }

        

        protected override void OnDispose()
        {
            if (_rootButton != null)
                _rootButton.clicked -= InvokeClick;

            _rootButton = null;
            _imageDisplay = null;
            _mainLabel = null;
            _descriptionLabel = null;
        }

        protected override VisualElement GetBaseElement(VisualElement root)
        {
            return root.Q("option-card-button");
        }
    }
}
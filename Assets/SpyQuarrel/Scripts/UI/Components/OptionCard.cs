using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    [UxmlElement]
    public partial class OptionCardElement : UxmlView
    {
        private string _title = "Title";
        private string _description = "Description will go here";
        private Texture2D _imageDisplayTexture = Texture2D.whiteTexture;

        public event Action OnClick;

        private VisualElement _imageDisplay;
        private Label _mainLabel;
        private Label _descriptionLabel;

        private bool _initialized;
        private bool _dirty;

        protected override string AssetPath => "OptionCard";

        #region UXML ATTRIBUTES

        [UxmlAttribute]
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                MarkDirtyAndApply();
            }
        }

        [UxmlAttribute]
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                MarkDirtyAndApply();
            }
        }

        [UxmlAttribute]
        public Texture2D ImageDisplay
        {
            get => _imageDisplayTexture;
            set
            {
                _imageDisplayTexture = value;
                MarkDirtyAndApply();
            }
        }

        #endregion

        public OptionCardElement() : base()
        {
        }
        
        public OptionCardElement(string title, string description, Texture2D imageDisplay) : base()
        {
            _title = title;
            _description = description;
            _imageDisplayTexture = imageDisplay;
        }

        protected override void OnInitialize()
        {
            style.height = Length.Percent(100);

            InitElements();
            RegisterCallbacks();

            _initialized = true;

            ApplyAll();
        }

        private void InitElements()
        {
            _imageDisplay = GetElement<VisualElement>("image-display");
            _mainLabel = GetElement<Label>("option-main-label");
            _descriptionLabel = GetElement<Label>("description-label");
        }

        private void RegisterCallbacks()
        {
            if (_rootElement != null)
                _rootElement.RegisterCallback<FocusEvent>(OnElementFocus, TrickleDown.TrickleDown);
        }

        private async void OnElementFocus(FocusEvent evt)
        {
            var delay = _rootElement.resolvedStyle.transitionDelay.First().value;
            var duration = _rootElement.resolvedStyle.transitionDuration.First().value;

            var maxTime = delay + duration - 0.05f;

            if (Application.isPlaying)
                await Awaitable.WaitForSecondsAsync(maxTime);
            else
                await Task.Delay(Mathf.FloorToInt(maxTime * 1000));

            _rootElement.Blur();
            InvokeClick();
        }

        private void InvokeClick()
        {
            OnClick?.Invoke();
            Debug.Log("boop");
        }

        #region DIRTY / APPLY SYSTEM

        private void MarkDirtyAndApply()
        {
            if (!_initialized)
            {
                _dirty = true;
                return;
            }

            ApplyAll();
        }

        private void ApplyAll()
        {
            if (_mainLabel != null)
                _mainLabel.text = _title;

            if (_descriptionLabel != null)
                _descriptionLabel.text = _description;

            if (_imageDisplay != null && _imageDisplayTexture != null)
                _imageDisplay.style.backgroundImage = new StyleBackground(_imageDisplayTexture);

            _dirty = false;
        }

        #endregion

        protected override void OnDispose()
        {
            _imageDisplay = null;
            _mainLabel = null;
            _descriptionLabel = null;
        }

        protected override VisualElement GetBaseElement(VisualElement root)
        {
            return root.Q<VisualElement>("option-card-button");
        }
    }
}
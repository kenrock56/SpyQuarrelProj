using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    [UxmlElement]
    public partial class StartMenuElement : PageView
    { 
        protected override string AssetPath => "StartMenu";
        
        private Button _playButton;
        private Button _quitButton;


        protected override void OnInitialize()
        {
            InitElements();
            RegisterCallbacks();
        }

        protected override VisualElement GetBaseElement(VisualElement root)
        {
            return root.Q("root");
        }

        private void InitElements()
        {
            _playButton = GetElement<Button>("play-button");
            _quitButton = GetElement<Button>("quit-game-button");
        }

        private void RegisterCallbacks()
        {
            if(_playButton != null)
                _playButton.clicked += OnPlayButtonClicked;
            
            if(_quitButton != null)
                _quitButton.clicked += OnQuitButtonClicked;
        }

        private void OnPlayButtonClicked()
        {
            Debug.Log("Play Button clicked");
        }

        private void OnQuitButtonClicked()
        {
            Debug.Log("Quit Button clicked");
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    [UxmlElement]
    public partial class RoleSelectPageElement : MenuPageView
    {
        protected override string AssetPath => "RoleSelectPage";
        public override MenuPages MenuPageDefinition => MenuPages.RoleSelect;
        
        private Button _spySelectButton;
        private Button _sniperSelectButton;

        protected override void OnInitialize()
        {
            InitElements();
            RegisterCallbacks();
        }

        private void InitElements()
        {
            _spySelectButton = GetElement<Button>("spy-select-button");
            _sniperSelectButton = GetElement<Button>("sniper-select-button");
        }

        private void RegisterCallbacks()
        {
            if (_spySelectButton != null)
                _spySelectButton.clicked += SelectSpy;
            if (_sniperSelectButton != null)
                _sniperSelectButton.clicked += SelectSniper;

            if (Application.isPlaying)
            {
                GameNetworkManager.Instance.OnSuccessfulSpawn += SuccessfulSpawn;
            }
            
            
        }

        private void SuccessfulSpawn()
        {
            Hide();
        }

        private void SelectSpy()
        {
            if (Application.isPlaying && GameNetworkManager.HasInstance)
            {
                GameNetworkManager.Instance.SpawnAsRole(PlayerRole.Spy);
            }
        }

        private void SelectSniper()
        {
            if (Application.isPlaying && GameNetworkManager.HasInstance)
            {
                GameNetworkManager.Instance.SpawnAsRole(PlayerRole.Sniper);
            }
        }

        protected override VisualElement GetBaseElement(VisualElement root)
        {
            return root.Q<VisualElement>("root");
        }

        protected override void OnDispose()
        {
            if (_spySelectButton != null)
                _spySelectButton.clicked -= SelectSpy;
            if (_sniperSelectButton != null)
                _sniperSelectButton.clicked -= SelectSniper;
            
            _spySelectButton = null;
            _sniperSelectButton = null;
        }
    }
}

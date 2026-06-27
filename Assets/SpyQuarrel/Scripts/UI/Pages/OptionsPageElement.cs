using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    [UxmlElement]
    public partial class OptionsPageElement : MenuPageView

    { 
        public override MenuPages MenuPageDefinition => MenuPages.PlayOptions;
        
        protected override string AssetPath => "PlayOptionMenu";

        private VisualElement _buttonContainer;
        
        private OptionCardElement _startHostButton;
        private OptionCardElement _startClientButton;
        
        private OptionCardElement _createSessionButton;
        private OptionCardElement _startServerButton;
        
        
        protected override void OnInitialize()
        {
            GetElements();

            if (_buttonContainer != null)
            {
                _startHostButton = new OptionCardElement("Start Host", "Start a game as host", Texture2D.blackTexture);
                _startClientButton = new OptionCardElement("Start Client", "Start game as a Client", Texture2D.blackTexture);
                _createSessionButton = new OptionCardElement("Create Session", "Create a Session", Texture2D.blackTexture);
                _startServerButton = new OptionCardElement("Start Server", "Start a Server /// Quick Join for now", Texture2D.blackTexture);
                
                _startHostButton.OnClick += StartHost;
                _startClientButton.OnClick += StartClient;
                _createSessionButton.OnClick += StartSession;
                _startServerButton.OnClick += QuickJoinSession;
                
                _buttonContainer.Add(_startHostButton);
                _buttonContainer.Add(_startClientButton);
                _buttonContainer.Add(_createSessionButton);
                _buttonContainer.Add(_startServerButton);
            }

            if (Application.isPlaying && NetworkManager.Singleton is not null)
            {
                NetworkManager.Singleton.OnConnectionEvent += OnConnection;
            }
            //_startHostButton = new OptionCardElement();
        }

      

        private void StartClient()
        {
            if(!Application.isPlaying)return;
            if(NetworkManager.Singleton == null)return;
            
            if (NetworkManager.Singleton.StartClient())
            {
                
            }
            else
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        private void OnConnection(NetworkManager arg1, ConnectionEventData connectionEventData)
        {
            var success = connectionEventData.EventType == ConnectionEvent.ClientConnected;

            if (success)
            {
                Hide();
            }
        }

        private void StartHost()
        {
            if(!Application.isPlaying)return;
            if(NetworkManager.Singleton == null)return;
            
            if (NetworkManager.Singleton.StartHost())
            {
                
            }
            
        }
        
        private void StartSession()
        {
            if(!Application.isPlaying)return;
            if(NetworkManager.Singleton == null)return;
            
            SessionManager.Instance.StartSessionAsHost();
        }
        
        private void QuickJoinSession()
        {
            if(!Application.isPlaying)return;
            if(NetworkManager.Singleton == null)return;
            
            SessionManager.Instance.QuickJoinSession();
        }

        private void GetElements()
        {
            _buttonContainer = _rootElement.Q<VisualElement>("button-container");
        }

        protected override VisualElement GetBaseElement(VisualElement root)
        {
            return root.Q("root");
        }

        protected override void OnDispose()
        {
            if(_startHostButton != null) 
                _startHostButton.OnClick -= StartHost;
            if(_startClientButton != null)
                _startClientButton.OnClick -= StartClient;
            
            if (Application.isPlaying && NetworkManager.Singleton is not null)
            {
                NetworkManager.Singleton.OnConnectionEvent += OnConnection;
            }
        }
    }
}

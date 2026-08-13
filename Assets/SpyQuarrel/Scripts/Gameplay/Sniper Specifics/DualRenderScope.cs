using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    public class DualRenderScope : MonoBehaviour
    {
        [SerializeField]private RenderTexture _renderTexture;
        [SerializeField]private Camera _camera;
        
        [SerializeField]private UIDocument _document;

        public RenderScopeElement RenderScope => _renderScopeElement;
        private RenderScopeElement _renderScopeElement;

        async void Awake()
        {
            await Awaitable.EndOfFrameAsync();
            await Awaitable.EndOfFrameAsync();
            await Awaitable.EndOfFrameAsync();
            
            InitialiseCamera();
        }

        private void InitialiseCamera()
        {
            if (_camera == null)
            {
                var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);

                var finalCam = cams.FirstOrDefault(cam => cam.CompareTag("DualRender"));

                if (finalCam != null)
                {
                    _camera = finalCam;
                    Debug.Log("Camera found");
                }
                else
                {
                    _camera = Camera.main;
                    Debug.Log("Camera not found");
                }
            }
        }

        void Start()
        {
            Debug.LogError($"Null Scope {_renderScopeElement == null}");
            
            if (_document != null)
            {
                _renderScopeElement = new RenderScopeElement(_document.rootVisualElement);
            }
            
            //_renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            if (_camera != null)
            {
                _camera.targetTexture = _renderTexture;
            }
        }
        
        
    }
}

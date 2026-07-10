using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class TeleportPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameObject _uiContainer;

        public bool IsInteractable => true;

        public string InteractName => "Sniper Point";
        public string InteractDescription => "Teleport";

        private Transform _cameraTransform;
        private Transform _localPlayerTransform;
        

        private void Awake()
        {
            if (_uiContainer)
            {
                _uiContainer.SetActive(false);
            }
        }

        private void Start()
        {
            CacheCameraTransform();
            CacheLocalPlayerTransform();
        }

        private void Update()
        {
            UpdateUiRotation();
        }

        public void OnInteractEnter(Interactor interactor)
        {
            if (interactor == null)
                return;

            if (interactor.PlayerRole != PlayerRole.Sniper)
                return;

            if (!_uiContainer)
                return;

            if (!_uiContainer.activeSelf)
            {
                _uiContainer.SetActive(true);
            }

            UpdateUiRotation();
        }

        public void Interact(Interactor interactor)
        {
            Debug.Log("Interact");
            
            // if(interactor.PlayerRole != PlayerRole.Sniper)
            //     return;
            
            if (GameNetworkManager.HasInstance)
            {
                // var player = GameNetworkManager.Instance.LocalPlayer;
                // player.Teleport(transform.position);
                GameNetworkManager.Instance.RequestTeleport(transform.position);
            }
        }

        public void OnInteractHover(Interactor interactor)
        {
            if (interactor == null)
                return;

            Debug.Log($"Player Role {interactor.PlayerRole}");

            if (interactor.PlayerRole != PlayerRole.Sniper)
                return;

            UpdateUiRotation();
        }
        

        public void OnInteractExit(Interactor interactor)
        {
            if (interactor == null)
                return;

            if (interactor.PlayerRole != PlayerRole.Sniper)
                return;

            if (!_uiContainer)
                return;

            if (_uiContainer.activeSelf)
            {
                _uiContainer.SetActive(false);
            }
        }

        private void UpdateUiRotation()
        {
            if (!_uiContainer || !_uiContainer.activeSelf)
                return;

            Transform targetTransform = GetLookTargetTransform();

            if (!targetTransform)
                return;

            Vector3 directionToTarget = targetTransform.position - _uiContainer.transform.position;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude <= 0.001f)
                return;

            _uiContainer.transform.rotation = Quaternion.LookRotation(-directionToTarget.normalized, Vector3.up);
        }

        private Transform GetLookTargetTransform()
        {
            CacheLocalPlayerTransform();

            if (_localPlayerTransform)
            {
                return _localPlayerTransform;
            }

            CacheCameraTransform();

            return _cameraTransform;
        }

        private void CacheLocalPlayerTransform()
        {
            if (_localPlayerTransform)
                return;

            if (!GameNetworkManager.HasInstance)
                return;

            if (GameNetworkManager.Instance.LocalPlayer == null)
                return;

            _localPlayerTransform = GameNetworkManager.Instance.LocalPlayer.transform;
        }

        private void CacheCameraTransform()
        {
            if (_cameraTransform)
                return;

            Camera mainCamera = Camera.main;

            if (!mainCamera)
                return;

            _cameraTransform = mainCamera.transform;
        }
    }
}
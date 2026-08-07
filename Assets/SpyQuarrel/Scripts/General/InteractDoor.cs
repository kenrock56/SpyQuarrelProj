using System;
using System.Threading;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class InteractDoor : MonoBehaviour, IInteractable
    {
        public PlayerRole RequiredRole { get; set; } = PlayerRole.Spy;
        [field:SerializeField]public Transform UIAnchorPoint { get; set; }
        public bool HoldInteraction => false;
        public float HoldInteractionTime => 0f;
        public string InteractName => _isOpen ? "Close Door" : "Open Door";
        public string InteractDescription => "Interact with door";
        public bool IsInteractable => !_isLerping;
        public bool IsWorldSpaceUI => true;

        [Header("Side Positions")]
        [SerializeField] private Transform _frontLocalPos;
        [SerializeField] private Transform _backLocalPos;

        [Header("Door Settings")]
        [SerializeField] private float _openAngle = 100f;

        [Header("Door Lerp")]
        [SerializeField] private float _lerpTime = 0.25f;
        [SerializeField] private bool _isLerping;

        private Quaternion _closedRotation;
        private CancellationToken _destroyCancellationToken;

        private float _openDirection = 1f;
        private bool _isOpen;

        private void Awake()
        {
            _closedRotation = transform.localRotation;
            _destroyCancellationToken = destroyCancellationToken;
        }

        public void OnInteractEnter(Interactor interactor)
        {
            
        }

        public void OnInteractHover(Interactor interactor)
        {
        }

        public void OnInteractExit(Interactor interactor)
        {
        }

        public void Interact(Interactor interactor)
        {
            if (!interactor || !interactor.Owner || _isLerping)
                return;

            if (!_isOpen)
                SetOpenDirection(interactor.Owner.transform.position);

            ToggleDoor();
        }

        private void SetOpenDirection(Vector3 playerPosition)
        {
            if (!_frontLocalPos || !_backLocalPos)
            {
                Debug.LogWarning(
                    "[InteractDoor] Front or back position is not assigned.",
                    this);

                _openDirection = 1f;
                return;
            }

            float frontDistance = GetHorizontalSqrDistance(
                playerPosition,
                _frontLocalPos.position);

            float backDistance = GetHorizontalSqrDistance(
                playerPosition,
                _backLocalPos.position);

            bool openedFromFront = frontDistance <= backDistance;

            _openDirection = openedFromFront ? -1f : 1f;

            Debug.Log(
                openedFromFront
                    ? "Player interacted from the front."
                    : "Player interacted from the back.",
                this);
        }

        private float GetHorizontalSqrDistance(Vector3 positionA, Vector3 positionB)
        {
            positionA.y = 0f;
            positionB.y = 0f;

            return (positionA - positionB).sqrMagnitude;
        }

        private void ToggleDoor()
        {
            _isOpen = !_isOpen;

            Quaternion targetRotation = _isOpen
                ? _closedRotation * Quaternion.Euler(0f, _openAngle * _openDirection, 0f)
                : _closedRotation;

            LerpDoor(targetRotation, _lerpTime, _destroyCancellationToken);
        }

        private async void LerpDoor(Quaternion targetRotation, float lerpTime, CancellationToken cancellationToken = default)
        {
            if (_isLerping)
                return;

            if (!cancellationToken.CanBeCanceled)
                cancellationToken = _destroyCancellationToken;

            _isLerping = true;

            Quaternion startRotation = transform.localRotation;

            if (lerpTime <= 0f)
            {
                transform.localRotation = targetRotation;
                _isLerping = false;
                return;
            } 
            
            float timeElapsed = 0f; 
            
            while (timeElapsed < lerpTime) 
            { 
                cancellationToken.ThrowIfCancellationRequested();

                timeElapsed += Time.deltaTime;

                float t = Mathf.Clamp01(timeElapsed / lerpTime);
                
                var rot =  Quaternion.Slerp(startRotation, targetRotation, t);
                
                transform.localRotation = rot;
                
                await Awaitable.EndOfFrameAsync(cancellationToken);
            } 
            
            transform.localRotation = targetRotation;
                
            _isLerping = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(transform.position, 0.08f);

            if (_frontLocalPos)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_frontLocalPos.position, 0.12f);
                Gizmos.DrawLine(transform.position, _frontLocalPos.position);
            }

            if (_backLocalPos)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_backLocalPos.position, 0.12f);
                Gizmos.DrawLine(transform.position, _backLocalPos.position);
            }
        }
    }
}
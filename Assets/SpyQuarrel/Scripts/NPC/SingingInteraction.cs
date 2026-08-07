using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace SpyQuarrelRuntime
{
    public class SingingInteraction : MonoBehaviour, IInteractable
    {
        
        [Header("Interaction Settings")]
        [field:SerializeField]public Transform UIAnchorPoint { get; set; }
        public PlayerRole RequiredRole { get; set; } = PlayerRole.Spy;
        public bool HoldInteraction { get; set; } = false;
        public float HoldInteractionTime { get; set; } = 0f;
        public string InteractName { get; set; } = "Sing";
        public string InteractDescription { get; set; } = "Sing your heart out";
        public bool IsInteractable { get; set; } = true;
        public bool IsWorldSpaceUI { get; set; } = true;
        
        [Header("Other Settings")]
        
        private static readonly int _singAnim = Animator.StringToHash("StandingSing");

        [SerializeField]private PatrolPoint _patrolPoint;

        [SerializeField] private bool _isOccupied;
        
        void Awake()
        {
            
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out NPCharacter character))
            {
                if (!_isOccupied)
                {
                    _ = StartNpcSingingInteraction(character);
                }
                
            } 
            if (other.TryGetComponent(out PlayerCharacter playerCharacter) && playerCharacter.transform.root.TryGetComponent(out SpyCharacter spyChar) )
            {
                spyChar.NpcIdentityProvider.SetAnimation(NpcAnimState.Sit);
            }
            
            Debug.Log($"{other.name} has entered the SingingInteraction");
            
        }
        


        private async Task StartNpcSingingInteraction(NPCharacter character)
        {
            _isOccupied = true;
            var pos = _patrolPoint ? _patrolPoint.transform.position : transform.position;
            character.StopPatrol();
            character.RequestDestination(pos);
            
                
            character.SetAnimation(NpcAnimState.Sit);

            var time = UnityEngine.Random.Range(10f, 11f);
            
            var timeElapsed = 0f;
            
            
            
            while (timeElapsed <= time)
            {
                timeElapsed += Time.deltaTime;
                
                Debug.Log($"{time - timeElapsed}");
                await Awaitable.EndOfFrameAsync();
            }
            
            
            
            character.SetAnimation(NpcAnimState.Move);
            
            await Awaitable.WaitForSecondsAsync(2f);
            
            _ = DelayedReset();
            
            var patrolRef = new PatrolRouteReference(4);
            
            character.SetRouteRpc(patrolRef);
            //character.StartPatrol();
        }

        private void OnTriggerStay(Collider other)
        {
            Debug.Log($"SING: {other.name} is inside the SingingInteraction");
            
        }

        private async Task DelayedReset()
        {
            await Awaitable.WaitForSecondsAsync(4f);
            _isOccupied = false;
        }

        private void OnDrawGizmos()
        {
            if (!_patrolPoint) return;

            if(true)return;
            
            if(false)return;
            var pos = _patrolPoint.transform.position;
            var rot = _patrolPoint.transform.rotation;
            
            //Handles.ArrowHandleCap();
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
            
        }
    }
}

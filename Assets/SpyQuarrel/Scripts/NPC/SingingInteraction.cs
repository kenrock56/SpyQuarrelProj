using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace SpyQuarrelRuntime
{
    public class SingingInteraction : MonoBehaviour
    {
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
            
            Debug.Log($"{other.name} has entered the SingingInteraction");
            
            
        }


        private async Task StartNpcSingingInteraction(NPCharacter character)
        {
            _isOccupied = true;
            var pos = _patrolPoint ? _patrolPoint.transform.position : transform.position;
            character.StopPatrol();
            character.RequestDestination(pos);
            
                
            character.SetAnimation(NpcAnimState.Sing);

            var time = UnityEngine.Random.Range(4f, 5f);
            
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
            //Debug.Log($"{other.name} is inside the SingingInteraction");
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
        
    }
}

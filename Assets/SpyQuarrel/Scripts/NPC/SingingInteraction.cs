using System;
using UnityEditor;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class SingingInteraction : MonoBehaviour
    {
        private static readonly int _singAnim = Animator.StringToHash("StandingSing");

        [SerializeField]private PatrolPoint _patrolPoint;
        
        
        void Awake()
        {
            
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out NPCharacter character))
            {
                Debug.Log($"{character.name} has entered the SingingInteraction");
                
                var pos = _patrolPoint ? _patrolPoint.transform.position : transform.position;
                character.StopPatrol();
                
                character.transform.position = pos;
                character.transform.forward = _patrolPoint.transform.forward;
                
                character.SetAnimation(NpcAnimState.Sing);
            }
            
            Debug.Log($"{other.name} has entered the SingingInteraction");
            
            
        }

        private void OnTriggerStay(Collider other)
        {
            //Debug.Log($"{other.name} is inside the SingingInteraction");
        }

        private void OnDrawGizmos()
        {
            if (!_patrolPoint) return;
            
            var pos = _patrolPoint.transform.position;
            var rot = _patrolPoint.transform.rotation;
            
            //Handles.ArrowHandleCap();
        }
        
    }
}

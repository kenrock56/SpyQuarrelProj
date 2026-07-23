using System;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class SingingInteraction : MonoBehaviour
    {
        [SerializeField] private Animation _singAnim;

        private int _singAnimHash;

        void Awake()
        {
            if(_singAnim == null)return;
            _singAnimHash =  Animator.StringToHash(_singAnim.name);
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out NPCharacter character))
            {
                Debug.Log($"{character.name} has entered the SingingInteraction");
            }
            
            Debug.Log($"{other.name} has entered the SingingInteraction");
            
            
        }

        private void OnTriggerStay(Collider other)
        {
            Debug.Log($"{other.name} is inside the SingingInteraction");
        }
    }
}

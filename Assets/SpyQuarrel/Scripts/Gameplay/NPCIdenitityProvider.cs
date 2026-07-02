    using System;
    using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class NPCIdenitityProvider : MonoBehaviour
    {
       [field:SerializeField]public NpcType NpcIdentityType { get;private set; }
       
       private GameObject _root;
       private GameObject _currentIdentity;
       
       [SerializeField]private Transform _followTransform;
       
       [SerializeField]private Animator _animator;

       void Awake()
       {
           InitialiseIdentity();
       }

       void Start()
       {
           BuildNpc();
       }

       void InitialiseIdentity()
       {
           if(_root != null)return;
           
           _root = new GameObject("root");
           _root.transform.parent = transform;
           _root.transform.localPosition = Vector3.zero;
       }

       private void FixedUpdate()
       {
           UpdatePosition();
       }

       private void Update()
       {
          UpdatePosition();
       }

       private void LateUpdate()
       {
           UpdatePosition();
       }


       private void UpdatePosition()
       {
           if(_followTransform == null)return;
           if(_currentIdentity == null)return;
           
           this.transform.position = _followTransform.position;
       }

       void BuildNpc()
       {
           
           if (NpcDictionary.Entries[NpcIdentityType] is { } identity)
           {
               if (_currentIdentity != null)
               {
                   Destroy(_currentIdentity);
               }
               
               var disguise = Instantiate(identity, identity.transform.position, _root.transform.rotation);
               
               disguise.transform.parent = _root.transform;
               disguise.transform.localPosition = Vector3.zero;
               
               _currentIdentity = disguise;
               
               _animator = _currentIdentity.GetComponent<Animator>();
           }
       }

       public void SetAppearance(NpcType npcIdentityType)
       {
           NpcIdentityType = npcIdentityType;
           BuildNpc();
       }

       private void OnValidate()
       {
           BuildNpc();
       }
    }
}

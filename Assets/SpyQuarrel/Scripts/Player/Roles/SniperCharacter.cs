using System;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class SniperCharacter : Player
    {
        [SerializeField] private LineRenderer _laserRenderer;

        private Interactor _interactor;
        
        public readonly NetworkVariable<SniperLaserData> LaserData = new NetworkVariable<SniperLaserData>(writePerm: NetworkVariableWritePermission.Owner, readPerm:  NetworkVariableReadPermission.Everyone);
        protected override void Awake()
        {
            base.Awake();
            
            _interactor = GetComponent<Interactor>();
            if (_interactor == null)
            {
                _interactor = GetComponentInChildren<Interactor>();
            }

            if(_laserRenderer != null)
            {
                _laserRenderer.positionCount = 2;
            }
            Debug.Log(_interactor ? "Interactor found"  : "Interactor not found");
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (IsOwner)
            {
                if(_interactor == null)return;
                var startPos = _interactor.InteractStartPos;
                var endPos = _interactor.InteractEndPos;
                SetStartPos(startPos);
                SetEndPos(endPos);
                
                Debug.DrawLine(startPos, endPos, Color.blue);

                var dataStart = startPos.y;
                
                var data = new SniperLaserData(endPos, dataStart);
                
                LaserData.Value = data;
            }
            else
            {
                var startPos = Character.transform.position;
                startPos.y = LaserData.Value.StartYPos;

                var endPos = LaserData.Value.EndPos;
                
                SetStartPos(startPos);
                SetEndPos(endPos);
            }
        }


        private void SetStartPos(Vector3 pos)
        {
            SetPos(0, pos);
        }
        private void SetEndPos(Vector3 pos)
        {
            SetPos(1, pos);
        }

        private void SetPos(int index, Vector3 pos)
        {
            if(_laserRenderer == null)return;
            _laserRenderer.SetPosition(index, pos);
        }

        private void OnDrawGizmos()
        {
            // if (Application.isPlaying)
            // {
            //     var ray = new Ray(transform.position, transform.forward);
            //     Gizmos.DrawRay(ray);
            // }
            
        }
    }
    
    
}

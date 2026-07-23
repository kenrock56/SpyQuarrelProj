using System.Collections.Generic;
using System.Linq;
using AutoSingleton;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Singleton]
    public class PatrolManager : NetworkBehaviour
    {
        public static PatrolManager Instance => Singleton<PatrolManager>.Instance;
        public static bool HasInstance => Singleton<PatrolManager>.HasInstance;

        public IEnumerable<PatrolRoute> PatrolPoints => _patrolRoutes;
        [SerializeField]private List<PatrolRoute> _patrolRoutes =  new List<PatrolRoute>();

        private void Awake()
        {
            _patrolRoutes = FindObjectsByType<PatrolRoute>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        }
    }
}
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

        public bool TryGetRandomRoute(PatrolRoute exception, out PatrolRoute route)
        {
            route = null;

            if (_patrolRoutes == null || _patrolRoutes.Count == 0)
                return false;

            var availableRoutes = _patrolRoutes.ToList();

            if (exception != null)
            {
                availableRoutes.Remove(exception);
            }

            if (availableRoutes.Count == 0)
                return false;
            
            var randomIndex = Random.Range(0, availableRoutes.Count);
            
            route = availableRoutes[randomIndex];

            return true;
        }

        public bool TryGetIndexOfRoute(PatrolRoute route, out int index)
        {
            index = -1;
            if (route == null)
                return false;
            
            if (_patrolRoutes.Contains(route))
            {
                index = _patrolRoutes.IndexOf(route);
                return true;
            }
            
            return false;
            
        }

        public bool TryGetRouteFromIndex(int index, out PatrolRoute route)
        {
            route = null;
            if (_patrolRoutes == null || _patrolRoutes.Count == 0)
                return false;

            var pat = _patrolRoutes.ElementAt(index);
            
            if (pat != null)
            {
                route = _patrolRoutes.ElementAt(index);
                return true;
            }
            
            return false;
        }
       
    }
}
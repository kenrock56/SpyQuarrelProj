using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class PatrolRoute : MonoBehaviour
    {
        public PatrolPoint[] PatrolPoints => _patrolPoints;
        [SerializeField]private PatrolPoint[] _patrolPoints = Array.Empty<PatrolPoint>();

        private void Awake()
        {
            AnnounceNewPoint();
        }

        private void OnValidate()
        {
            AnnounceNewPoint();
        }

        private void AnnounceNewPoint()
        {
            if (!Application.isPlaying)
            {
                var route = transform.root.GetComponent<PatrolRoute>();
                if (route == null) return;
                route.GetPoints();
                route.ValidatePoints();
            }
        }

        [ContextMenu("Get Points")]
        private void GetPoints()
        {
            _patrolPoints = GetComponentsInChildren<PatrolPoint>();
        }
        
        [ContextMenu("Validate Patrol Points")]
        private void ValidatePoints()
        {
            if (_patrolPoints is { Length: <= 0 })return;

            int valiadtes = 0;
            foreach (var patrolPoint in _patrolPoints)
            {
                patrolPoint.ValidatePoint();
                valiadtes++;
            }
            
            Debug.Log($"Validated {valiadtes} points");
        }


        private void OnDrawGizmos()
        {
            if (_patrolPoints is { Length: <= 0 })return;
            DrawLineToLine(_patrolPoints);
        }

        private void DrawLineToLine(PatrolPoint[] points)
        {
            var locs = points.Select(point => point.transform.position).ToList();
            locs.Add(points[0].transform.position);
            var locArray = locs.ToArray();
            
            Handles.DrawPolyLine(locArray);
        }
    }
}
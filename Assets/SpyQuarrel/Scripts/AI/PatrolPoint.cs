using System;
using UnityEngine;
using UnityEngine.AI;

namespace SpyQuarrelRuntime
{
    public class PatrolPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.25f);
        }

        [ContextMenu("Validate Patrol")]
        public void ValidatePoint()
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                transform.position = new Vector3(
                    transform.position.x,
                    hit.position.y,
                    transform.position.z);
            }
        }
    }
}

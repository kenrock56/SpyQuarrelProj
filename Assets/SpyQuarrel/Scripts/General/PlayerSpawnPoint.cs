using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [field: SerializeField]
        public PlayerRole SpawnRole { get; private set; } = PlayerRole.None;

        public Vector3 SpawnPosition { get; private set; }

        private void Awake()
        {
            CachePosition();
        }

        private void OnValidate()
        {
            CachePosition();
        }

        private void CachePosition()
        {
            SpawnPosition = transform.position;
        }

        private void OnDrawGizmos()
        {
            Vector3 position = transform.position;

            Gizmos.color = GetRoleColor();
            Gizmos.DrawSphere(position, 0.5f);

            // Optional direction marker.
            Gizmos.DrawLine(position, position + transform.forward);
        }

        private Color GetRoleColor()
        {
            return SpawnRole switch
            {
                PlayerRole.Sniper => Color.red,
                PlayerRole.Spy => Color.blue,
                _ => Color.black
            };
        }
    }
}
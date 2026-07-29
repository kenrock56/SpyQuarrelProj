using System;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public struct PatrolRouteReference :
        INetworkSerializable,
        IEquatable<PatrolRouteReference>
    {
        public int IndexInList { get; private set; }

        public PatrolRoute Route { get; private set; }

        public bool IsValid => IndexInList >= 0 && Route != null;

        public PatrolRouteReference(PatrolRoute route)
        {
            IndexInList = -1;
            Route = route;

            PatrolManager manager =
                PatrolManager.Instance;

            if (route != null && manager != null && manager.TryGetIndexOfRoute(route, out int index))
            {
                IndexInList = index;
            }
        }

        public PatrolRouteReference(int index)
        {
            IndexInList = index;
            Route = null;

            ResolveRoute();
        }

        public void SetIndex(int index)
        {
            IndexInList = index;
            ResolveRoute();
        }

        public void SetRoute(PatrolRoute route)
        {
            Route = route;
            IndexInList = -1;

            PatrolManager manager =
                PatrolManager.Instance;

            if (route != null && manager != null && manager.TryGetIndexOfRoute(route, out int index))
            {
                IndexInList = index;
            }
        }

        private void ResolveRoute()
        {
            Route = null;

            if (IndexInList < 0) return;

            PatrolManager manager = PatrolManager.Instance;

            if (manager == null) return;

            if (manager.TryGetRouteFromIndex(IndexInList, out PatrolRoute route))
            {
                Route = route;
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            int index = IndexInList;

            serializer.SerializeValue(ref index);
                
            IndexInList = index;

            ResolveRoute();
        }

        public bool Equals(PatrolRouteReference other)
        {
            return IndexInList == other.IndexInList;
        }

        public override bool Equals(object obj)
        {
            return obj is PatrolRouteReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return IndexInList;
        }
    }
}
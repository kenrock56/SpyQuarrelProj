using UnityEngine;

namespace SpyQuarrelRuntime
{
    public interface IEmptyDefault<T>
    { 
        public static T Empty { get; }
    }
}

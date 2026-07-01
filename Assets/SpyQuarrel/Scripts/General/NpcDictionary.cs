using System.Collections.Generic;
using AutoSingleton;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Singleton]
    public class NpcDictionary : ScriptableDictionary<NpcType, GameObject>
    {
        public static NpcDictionary Instance => Singleton<NpcDictionary>.Instance;
        public static bool HasInstance => Singleton<NpcDictionary>.HasInstance;

        public static IReadOnlyDictionary<NpcType, GameObject> Entries => Instance.Dictionary;
    }
}

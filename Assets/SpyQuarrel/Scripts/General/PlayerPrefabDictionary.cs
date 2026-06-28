using System;
using AutoSingleton;
using SpyQuarrelRuntime;
using UnityEngine;

namespace SpyQuarrelProject
{
    [CreateAssetMenu(fileName = "PlayerPrefabDictionary", menuName = "Scriptable Objects/PlayerPrefabDictionary")]
    [Singleton]
    public class PlayerPrefabDictionary : ScriptableDictionary<PlayerRole, Player>
    {
        public static PlayerPrefabDictionary Instance => Singleton<PlayerPrefabDictionary>.Instance;
        public static bool HasInstance => Singleton<PlayerPrefabDictionary>.HasInstance;
        
        void Awake()
        {
            BuildDictionary();
        }
        
        private void OnValidate()
        {
            BuildDictionary();
        }
        
    }

}


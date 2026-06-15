using System.Collections.Generic;
using UnityEngine;

namespace AutoSingleton
{
    // [CreateAssetMenu(fileName = AssetName, menuName = AssetName)]
    class SingletonCatalogue : ScriptableObject
    {
        public const string AssetName = "Auto Singleton List";

        static SingletonCatalogue _instance = null;
        static SingletonCatalogue Instance => (_instance == null ? _instance = Resources.Load<SingletonCatalogue>(AssetName) : _instance);

        public static Object Asset => Instance;

        public static List<ToggleableSingleton<Object>> MonoBehaviours => Instance.monoBehaviours;
        public static List<ToggleableSingleton<Object>> ScriptableObjects => Instance.scriptableObjects;

        [SerializeField] internal List<ToggleableSingleton<Object>> monoBehaviours = new List<ToggleableSingleton<Object>>();
        [SerializeField] internal List<ToggleableSingleton<Object>> scriptableObjects = new List<ToggleableSingleton<Object>>();
    }
}

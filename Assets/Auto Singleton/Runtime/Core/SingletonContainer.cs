using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AutoSingleton
{
    static class SingletonContainer
    {
        const string RootParentName = "Auto Singleton";

        static readonly Dictionary<Type, object> collection = new Dictionary<Type, object>();

        static readonly List<object> added = new List<object>();

        public static IReadOnlyDictionary<Type, object> Collection => collection;

        public static IReadOnlyList<object> Added => added;

        public static event Action OnCollectionReset;

        public static event Action<object> OnAdd;

        public static event Action<object> OnRemove;

        [HideInCallstack]
        public static void Add<T>(T singleton) where T : class
        {
            if (singleton == null)
                throw new ArgumentNullException(nameof(singleton));

            Type type = singleton.GetType();
            if (collection.ContainsKey(type))
                throw new ArgumentException($"Attempted to add singleton of type '{type.FullName}' but one already exists.");

            collection.Add(type, singleton);
            added.Add(singleton);

            OnAdd?.Invoke(singleton);
        }

        [HideInCallstack]
        public static void Remove<T>(T singleton) where T : class
        {
            if (singleton == null)
                throw new ArgumentNullException(nameof(singleton));

            Type type = singleton.GetType();
            if (collection.ContainsKey(type) == false || collection[type] != singleton)
                throw new ArgumentException($"Attempted to remove singleton of type '{type.FullName}' but it is not in the singleton list.");

            if (added.Contains(singleton) == false)
                throw new ArgumentException($"Attempted to remove singleton of type '{type.FullName}' but it is managed automatically.");

            collection.Remove(type);
            added.Remove(singleton);

            OnRemove?.Invoke(singleton);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InstantiateSingletonsFromCatalogue()
        {
            collection.Clear();
            added.Clear();

            bool anyNull = false;

            InstantiateScriptableObjects(ref anyNull);

            InstantiateMonoBehaviours(ref anyNull);

            if (anyNull)
                Debug.LogWarning($"[Auto Singleton] Found an empty entry in {SingletonCatalogue.AssetName}.");

            OnCollectionReset?.Invoke();
        }

        static void InstantiateScriptableObjects(ref bool anyNull)
        {
            foreach (ToggleableSingleton<Object> soToggleSingleton in SingletonCatalogue.ScriptableObjects)
            {
                if (soToggleSingleton.enabled == false)
                    continue;

                Object soSingleton = soToggleSingleton.value;
                if (soSingleton != null)
                    collection.Add(soSingleton.GetType(), soSingleton);
                else
                    anyNull = true;
            }
        }

        static void InstantiateMonoBehaviours(ref bool anyNull)
        {
            GameObject rootParent = null;

            foreach (ToggleableSingleton<Object> mbToggleSingleton in SingletonCatalogue.MonoBehaviours)
            {
                if (mbToggleSingleton.enabled == false)
                    continue;

                Object mbSingleton = mbToggleSingleton.value;
                if (mbSingleton != null)
                {
                    if (rootParent == null)
                    {
                        rootParent = new GameObject(RootParentName);
                        Object.DontDestroyOnLoad(rootParent);
                    }

                    GameObject prefab = (mbSingleton as MonoBehaviour).gameObject;

                    GameObject instance = Object.Instantiate(prefab, rootParent.transform);
                    instance.name = prefab.name;

                    MonoBehaviour mb = instance.GetComponent(mbSingleton.GetType()) as MonoBehaviour;
                    if (mb == null)
                        throw new InvalidOperationException($"Could not get the MonoBehaviour of type '{mbSingleton.GetType().Name}'.");

                    collection.Add(mbSingleton.GetType(), mb);
                }
                else
                    anyNull = true;
            }
        }
    }
}

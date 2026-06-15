/// Uncomment the line below to check the script in build.
// #undef UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoSingleton
{
    /// <summary>
    /// Generic access <see langword="class"/> for singleton instances.
    /// </summary>
    /// <typeparam name="T"> Singleton type/parent type/interface. </typeparam>
    public static class Singleton<T> where T : class
    {
        static T _instance;

        static List<T> _instances;

        static Singleton()
        {
            RetrieveFromContainer();

            SingletonContainer.OnCollectionReset += RetrieveFromContainer;
            SingletonContainer.OnAdd += OnSingletonContainerAdd;
            SingletonContainer.OnRemove += OnSingletonContainerRemove;
        }

        static void RetrieveFromContainer()
        {
            if (SingletonContainer.Collection.TryGetValue(typeof(T), out object obj))
                _instance = obj as T;

            List<T> instancesList = new List<T>();
            foreach (object o in SingletonContainer.Collection.Values)
                if (o is T instance)
                    instancesList.Add(instance);
            _instances = instancesList;
        }

        static void OnSingletonContainerAdd(object singleton)
        {
            if (singleton is not T instance)
                return;

            if (_instance == null && singleton.GetType() == typeof(T))
                _instance = instance;

            _instances.Add(instance);
        }

        static void OnSingletonContainerRemove(object singleton)
        {
            if (singleton is not T instance)
                return;

            if (_instance == instance)
                _instance = null;

            _instances.Remove(instance);
        }

        /// <summary> Singleton instance of type <typeparamref name="T"/> or the one derived of <typeparamref name="T"/> selected using <see cref="SelectInstance()"/>. Throws in the editor if no instance exists or was selected; returns <see langword="null"/> in builds. </summary>
        public static T Instance =>
#if UNITY_EDITOR
             EditorGetInstance();
#else
            _instance;
#endif

        /// <summary> All singleton instances derived from <typeparamref name="T"/>. </summary>
        public static IReadOnlyList<T> Instances =>
#if UNITY_EDITOR
             EditorGetInstances();
#else
            _instances;
#endif

        /// <summary> True if <see cref="Instance"/> has a value, false if it would throw an exception. </summary>
        public static bool HasInstance =>
#if UNITY_EDITOR
            EditorHasInstance();
#else
            (_instance != null);
#endif

        /// <summary>
        /// Get the singleton instance without throwing. Safe alternative to <see cref="Instance"/> in builds.
        /// </summary>
        /// <returns> True if an instance is available. </returns>
        public static bool TryGetInstance(out T instance)
        {
#if UNITY_EDITOR
            Assert.IsPlaying(nameof(TryGetInstance));
#endif

            instance = _instance;
            return (_instance != null);
        }

        /// <summary>
        /// Return a new array of all singleton instances that match the given <paramref name="predicate"/>.
        /// </summary>
        public static T[] Find(Predicate<T> predicate)
        {
            Assert.IsPlaying(nameof(Find));

            List<T> retList = null;

            foreach (T singleton in _instances)
                if (predicate(singleton))
                    (retList ??= new List<T>()).Add(singleton);

            return (retList != null) ? retList.ToArray() : Array.Empty<T>();
        }

        /// <summary>
        /// Set <see cref="Instance"/> to the unique singleton in <see cref="Instances"/> that match the given <paramref name="predicate"/>.
        /// </summary>
        /// <returns> Returns whether we found an instance to select. </returns>
        public static bool SelectInstance(Predicate<T> predicate)
        {
            Assert.IsPlaying(nameof(SelectInstance));

            bool duplicateMatch = false;
            T selectedInstance = null;

            foreach (T singleton in _instances)
                if (predicate(singleton))
                {
                    if (selectedInstance != null)
                        duplicateMatch = true;
                    else
                        selectedInstance = singleton;
                }

            if (duplicateMatch == true)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[Auto Singleton] Singleton<{typeof(T).Name}>.SelectInstance failed: multiple instances matched the predicate.");
#endif

                return false;
            }

            if (selectedInstance == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[Auto Singleton] Singleton<{typeof(T).Name}>.SelectInstance failed: no instances matched the predicate.");
#endif

                return false;
            }

            _instance = selectedInstance;
            return true;
        }

        /// <summary>
        /// Set <see cref="Instance"/> to the unique singleton in <see cref="Instances"/> that have the highest given <paramref name="priority"/>.
        /// </summary>
        /// <returns> <inheritdoc cref="SelectInstance(Predicate{T})" path="/returns"/> </returns>
        public static bool SelectInstance(Func<T, int> priority) => SelectInstanceFromPriority(priority, (a, b) => a > b);
        /// <inheritdoc cref="SelectInstance(Func{T, int})"/>
        public static bool SelectInstance(Func<T, float> priority) => SelectInstanceFromPriority(priority, (a, b) => a > b);

        /// <summary>
        /// Set <see cref="Instance"/> to the given <paramref name="instance"/>.
        /// </summary>
        /// <returns> <inheritdoc cref="SelectInstance(Predicate{T})" path="/returns"/> </returns>
        public static bool SelectInstance(T instance)
        {
            Assert.IsPlaying(nameof(SelectInstance));

            foreach (T singleton in _instances)
                if (singleton == instance)
                {
                    _instance = singleton;
                    return true;
                }

#if UNITY_EDITOR
            Debug.LogWarning($"[Auto Singleton] Singleton<{typeof(T).Name}>.SelectInstance failed: the given instance is not registered.");
#endif
            return false;
        }

        /// <summary>
        /// Set <see cref="Instance"/> to the singleton of type <typeparamref name="SubT"/>.
        /// </summary>
        /// <returns> <inheritdoc cref="SelectInstance(Predicate{T})" path="/returns"/> </returns>
        public static bool SelectInstance<SubT>() where SubT : T
        {
            Assert.IsPlaying(nameof(SelectInstance));

            foreach (T singleton in _instances)
                if (singleton.GetType() == typeof(SubT))
                {
                    _instance = singleton;
                    return true;
                }

#if UNITY_EDITOR
            Debug.LogWarning($"[Auto Singleton] Singleton<{typeof(T).Name}>.SelectInstance<{typeof(SubT).Name}> failed: no registered instance of type '{typeof(SubT).Name}' was found.");
#endif
            return false;
        }

        /// <summary>
        /// Set <see cref="Instance"/> to the unique instance derived from <typeparamref name="T"/>.
        /// </summary>
        /// <returns> <inheritdoc cref="SelectInstance(Predicate{T})" path="/returns"/> </returns>
        public static bool SelectInstance()
        {
            Assert.IsPlaying(nameof(SelectInstance));

            if (_instances.Count == 1)
            {
                _instance = _instances[0];
                return true;
            }

#if UNITY_EDITOR
            if (_instances.Count == 0)
                Debug.LogWarning($"[Auto Singleton] Singleton<{typeof(T).Name}>.SelectInstance failed: no instances are registered.");
            else
                Debug.LogWarning($"[Auto Singleton] Singleton<{typeof(T).Name}>.SelectInstance failed: {_instances.Count} instances are registered, expected exactly 1.");
#endif

            return false;
        }

        [HideInCallstack]
        static bool SelectInstanceFromPriority<TValue>(Func<T, TValue> priority, Func<TValue, TValue, bool> isSuperior) where TValue : struct
        {
            Assert.IsPlaying(nameof(SelectInstance));

            bool foundAny = false;
            TValue highestPriority = default;
            bool duplicateHighest = false;
            T selectedInstance = null;

            foreach (T instance in _instances)
            {
                TValue instancePriority = priority(instance);

                if (foundAny == false || isSuperior(instancePriority, highestPriority))
                {
                    highestPriority = instancePriority;
                    duplicateHighest = false;
                    selectedInstance = instance;
                    foundAny = true;
                }
                else if (Equals(instancePriority, highestPriority))
                    duplicateHighest = true;
            }

            if (foundAny == false || duplicateHighest)
            {
#if UNITY_EDITOR
                if (foundAny == false)
                    Debug.LogWarning($"[Auto Singleton] Singleton<{typeof(T).Name}>.SelectInstance failed: no instances are registered.");
                else
                    Debug.LogWarning($"[Auto Singleton] Singleton<{typeof(T).Name}>.SelectInstance failed: multiple instances share the highest priority.");
#endif
                return false;
            }

            _instance = selectedInstance;
            return true;
        }

#if UNITY_EDITOR
        [HideInCallstack]
        static T EditorGetInstance()
        {
            Assert.IsPlaying(nameof(Instance));

            if (_instance != null)
                return _instance;

            if (_instances.Count >= 1)
                throw new InvalidOperationException($"No singleton of type '{typeof(T).Name}' was selected. Call Singleton<{typeof(T).Name}>.SelectInstance() to pick one from the {_instances.Count} available instance(s).");
            else
                throw new InvalidOperationException($"No singleton of type '{typeof(T).Name}' exists.");
        }

        [HideInCallstack]
        static IReadOnlyList<T> EditorGetInstances()
        {
            Assert.IsPlaying(nameof(Instances));
            return _instances;
        }

        [HideInCallstack]
        static bool EditorHasInstance()
        {
            Assert.IsPlaying(nameof(HasInstance));
            return (_instance != null);
        }
#endif
    }
}

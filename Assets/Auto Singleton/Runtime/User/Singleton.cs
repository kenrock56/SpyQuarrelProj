using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoSingleton
{
    /// <summary>
    /// Utility to manually add and remove singleton instances at runtime.
    /// </summary>
    public static class Singleton
    {
        /// <summary>
        /// <para> Add the given <typeparamref name="T"/> to the list of singleton for this play session.                   </para>
        /// <para> This can be used with ANY reference type (<see langword="class"/>).                                      </para>
        /// <para> Note that using it with a <see cref="MonoBehaviour"/> will NOT make it a child of the singleton parent,
        ///        therefore it will be destroyed on load unless you use <see cref="Object.DontDestroyOnLoad(Object)"/>.    </para>
        /// </summary>
        /// <param name="singleton"> The singleton instance to add. </param>
        public static void Add<T>(T singleton) where T : class
        {
            Assert.IsPlaying(nameof(Add));

            SingletonContainer.Add(singleton);
        }
        /// <summary><inheritdoc cref="Add{T}(T)"/></summary>
        /// <param name="singletons"> Singleton instances to add. </param>
        public static void Add<T>(IEnumerable<T> singletons) where T : class
        {
            Assert.IsPlaying(nameof(Add));

            if (singletons == null)
                throw new ArgumentNullException(nameof(singletons));

            foreach (T singleton in singletons)
                SingletonContainer.Add(singleton);
        }
        /// <summary><inheritdoc cref="Add{T}(T)"/></summary>
        /// <param name="singletons"><inheritdoc cref="Add{T}(IEnumerable{T})"/></param>
        public static void Add<T>(params T[] singletons) where T : class => Add((IEnumerable<T>)singletons);

        /// <summary>
        /// <para> Remove the given <typeparamref name="T"/> from the list of singleton for this play session.  </para>          
        /// <para> This is only valid for singleton previously added using <see cref="Add{T}(T)"/>              </para>
        /// </summary>
        /// <param name="singleton"> The singleton instance to remove. </param>
        public static void Remove<T>(T singleton) where T : class
        {
            Assert.IsPlaying(nameof(Remove));

            SingletonContainer.Remove(singleton);
        }
        /// <summary><inheritdoc cref="Remove{T}(T)"/></summary>
        /// <param name="singletons"> Singleton instances to remove. </param>
        public static void Remove<T>(IEnumerable<T> singletons) where T : class
        {
            Assert.IsPlaying(nameof(Remove));

            if (singletons == null)
                throw new ArgumentNullException(nameof(singletons));

            foreach (T singleton in singletons)
                SingletonContainer.Remove(singleton);
        }
        /// <summary><inheritdoc cref="Remove{T}(T)"/></summary>
        /// <param name="singletons"><inheritdoc cref="Remove{T}(IEnumerable{T})"/></param>
        public static void Remove<T>(params T[] singletons) where T : class => Remove((IEnumerable<T>)singletons);
    }
}

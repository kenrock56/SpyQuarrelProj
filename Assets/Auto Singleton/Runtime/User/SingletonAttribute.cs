using System;
using UnityEngine;

namespace AutoSingleton
{
    /// <summary>
    /// Put this attribute over a class derived from <see cref="ScriptableObject"/> or <see cref="MonoBehaviour"/> to make it a singleton that will be managed automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SingletonAttribute : Attribute
    {
        /// <summary> Name given to this singleton in the Assets folder. </summary>
        public string displayName;
        /// <summary> Where to create this singleton, or derived classes if <see cref="inherited"/> is true. Relative to the Assets folder. </summary>
        public string folderPath;
        /// <summary> Should this attribute apply to derived classes. </summary>
        public bool inherited;
    }
}
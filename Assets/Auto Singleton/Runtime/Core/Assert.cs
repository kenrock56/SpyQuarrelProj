using System;
using System.Diagnostics;
using UnityEngine;

namespace AutoSingleton
{
    static class Assert
    {
        [Conditional("UNITY_EDITOR")][HideInCallstack]
        public static void IsPlaying(string memberName)
        {
            if (Application.isPlaying == false)
                throw new InvalidOperationException($"'{memberName}' cannot be used outside of play mode.");
        }
    }
}

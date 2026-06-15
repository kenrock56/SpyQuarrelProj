using UnityEditor;

namespace AutoSingletonEditor
{
    static class Options
    {
        const string AutomaticRefreshPrefKey = "[Auto Singleton] Automatic Refresh";
        const string LogChangesPrefKey = "[Auto Singleton] Log Changes";
        const string ProjectIconsPrefKey = "[Auto Singleton] Project Icons";

        static bool? automaticRefreshCache = null;
        static bool? logChangesCache = null;
        static bool? projectIconsCache = null;

        public static bool AutomaticRefresh
        {
            get => Get(ref automaticRefreshCache, AutomaticRefreshPrefKey, true);
            set => Set(value, ref automaticRefreshCache, AutomaticRefreshPrefKey);
        }

        public static bool LogChanges
        {
            get => Get(ref logChangesCache, LogChangesPrefKey, true);
            set => Set(value, ref logChangesCache, LogChangesPrefKey);
        }

        public static bool ProjectIcons
        {
            get => Get(ref projectIconsCache, ProjectIconsPrefKey, true);
            set => Set(value, ref projectIconsCache, ProjectIconsPrefKey);
        }

        static bool Get(ref bool? cache, string prefKey, bool defaultValue)
        {
            if (cache == null)
                cache = EditorPrefs.GetBool(prefKey, defaultValue);

            return cache.Value;
        }

        static void Set(bool value, ref bool? cache, string prefKey)
        {
            cache = value;
            EditorPrefs.SetBool(prefKey, value);
        }
    }
}

using AutoSingleton;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoSingletonEditor
{
    static class IconDisplay
    {
        const string IconPath = "Icons/SingletonIcon.png";

        static Texture2D icon;
        static HashSet<string> singletonGuids;

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            EditorApplication.projectWindowItemOnGUI -= ItemOnGUI;
            EditorApplication.projectWindowItemOnGUI += ItemOnGUI;

            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(ToolUtility.RelativePath, IconPath).Replace('\\', '/'));

            RefreshSingletonList();
        }

        public static void RefreshSingletonList()
        {
            if (SingletonCatalogue.Asset == null)
            {
                singletonGuids = new HashSet<string>();
                return;
            }

            IEnumerable<Object> monoBehaviours = SingletonCatalogue.MonoBehaviours.Select(mb => (mb.value as MonoBehaviour))
                                                                                  .Where(mb => mb != null)
                                                                                  .Select(mb => mb.gameObject);
            IEnumerable<Object> scriptableObjects = SingletonCatalogue.ScriptableObjects.Select(so => so.value);

            IEnumerable<string> guids = monoBehaviours.Concat(scriptableObjects)
                                                      .Select(o => (o != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long _) ? guid : null))
                                                      .Where(guid => guid != null);

            singletonGuids = new HashSet<string>(guids);
        }

        static void ItemOnGUI(string guid, Rect rect)
        {
            if (Options.ProjectIcons == false)
                return;

            if (icon == null)
                return;

            if (singletonGuids == null || singletonGuids.Contains(guid) == false)
                return;

            ColumnLayout columnLayout = ProjectWindowUtility.ColumnLayout;
            if (columnLayout == ColumnLayout.Unknown)
                return;

            if (columnLayout == ColumnLayout.One)
                rect.width = rect.height;
            else // (columnLayout == ColumnLayout.Two)
            {
                float gridSize = ProjectWindowUtility.GridSize;

                if (rect.width > rect.height)
                    rect.width = rect.height;

                if (gridSize <= 18f)
                    rect.x += 3f;

                rect.height = rect.width;
            }

            GUI.DrawTexture(rect, icon);
        }
    }
}

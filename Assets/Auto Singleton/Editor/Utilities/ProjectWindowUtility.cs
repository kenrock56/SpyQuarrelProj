using System;
using System.Reflection;
using UnityEditor;

namespace AutoSingletonEditor
{
    static class ProjectWindowUtility
    {
        const string ProjectBrowserTypeName = "UnityEditor.ProjectBrowser";
        const string HasOpenInstancesMethodName = "HasOpenInstances";
        const string ColumnLayoutFieldName = "m_ViewMode";
        const string GridSizeFieldName = "m_LastFoldersGridSize";

        static readonly Type projectBrowserType;
        static readonly MethodInfo hasOpenInstancesOfProjectBrowserMethod;
        static readonly FieldInfo columnLayoutField;
        static readonly FieldInfo gridSizeField;

        static bool computedForThisFrame;
        static EditorWindow projectWindow;

        static ColumnLayout _columnLayout;
        static float _gridSize;

        static ProjectWindowUtility()
        {
            projectBrowserType = typeof(EditorWindow).Assembly.GetType(ProjectBrowserTypeName);
            if (projectBrowserType == null)
                return;

            MethodInfo hasOpenInstancesMethod = typeof(EditorWindow).GetMethod(HasOpenInstancesMethodName, BindingFlags.Static | BindingFlags.Public);
            if (hasOpenInstancesMethod == null)
                return;

            hasOpenInstancesOfProjectBrowserMethod = hasOpenInstancesMethod.MakeGenericMethod(projectBrowserType);
            columnLayoutField = projectBrowserType.GetField(ColumnLayoutFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            gridSizeField = projectBrowserType.GetField(GridSizeFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        public static ColumnLayout ColumnLayout
        {
            get
            {
                Compute();
                return _columnLayout;
            }
        }

        public static float GridSize
        {
            get
            {
                Compute();
                return _gridSize;
            }
        }

        static void EditorUpdate()
        {
            computedForThisFrame = false;
        }

        static void Compute()
        {
            if (computedForThisFrame)
                return;

            if (hasOpenInstancesOfProjectBrowserMethod == null || columnLayoutField == null || gridSizeField == null)
            {
                computedForThisFrame = true;
                return;
            }

            if (projectWindow == null && (bool)hasOpenInstancesOfProjectBrowserMethod.Invoke(null, null))
                projectWindow = EditorWindow.GetWindow(projectBrowserType, false, null, false);

            _columnLayout = FindColumnLayout(projectWindow);
            _gridSize = FindGridSize(projectWindow);

            computedForThisFrame = true;
        }

        static ColumnLayout FindColumnLayout(EditorWindow projectWindow)
        {
            if (projectWindow == null)
                return ColumnLayout.Unknown;

            int listViewMode = (int)columnLayoutField.GetValue(projectWindow);

            return listViewMode switch
            {
                0 => ColumnLayout.One,
                1 => ColumnLayout.Two,
                _ => ColumnLayout.Unknown,
            };
        }

        static float FindGridSize(EditorWindow projectWindow)
        {
            if (projectWindow == null)
                return default;

            return (float)gridSizeField.GetValue(projectWindow);
        }
    }
}
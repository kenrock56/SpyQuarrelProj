using Codice.CM.Common.Tree;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    public class MyEditorWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<MyEditorWindow>();
            window.titleContent = new GUIContent("My Editor");
            window.minSize = new Vector2(600, 400);
        }

        private void Initialize()
        {
            VisualElement root = this.rootVisualElement;
            
            //example finding ur elements
            
            
        }
    }
}

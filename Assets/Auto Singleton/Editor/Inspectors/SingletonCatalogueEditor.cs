using UnityEditor;
using UnityEngine;
using AutoSingleton;
using static UnityEditor.EditorGUILayout;

namespace AutoSingletonEditor
{
    [CustomEditor(typeof(SingletonCatalogue))]
    class SingletonCatalogueEditor : Editor
    {
        const string MonoBehaviourTitle = "Mono Behaviour";
        const string ScriptableObjectTitle = "Scriptable Object";
        const string AddedInPlayModeTitle = "Added In Play Mode";

        const string NoneText = "(none)";
        static readonly string NullEntryText = $"This is null, you may want to use '{nameof(Object)}.{nameof(DontDestroyOnLoad)}' to keep the object alive.";

        const float ToggleWidth = 14f;

        SerializedProperty monoBehavioursProp;
        SerializedProperty scriptableObjectsProp;

        void OnEnable()
        {
            monoBehavioursProp = serializedObject.FindProperty(nameof(SingletonCatalogue.monoBehaviours));
            scriptableObjectsProp = serializedObject.FindProperty(nameof(SingletonCatalogue.scriptableObjects));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ConstantGUI();

            Space();

            if (Application.isPlaying)
                PlayModeGUI();

            serializedObject.ApplyModifiedProperties();
        }

        void ConstantGUI()
        {
            LabelField(MonoBehaviourTitle, EditorStyles.boldLabel);

            DrawCatalogueList(monoBehavioursProp);

            Space();

            LabelField(ScriptableObjectTitle, EditorStyles.boldLabel);

            DrawCatalogueList(scriptableObjectsProp);
        }

        void PlayModeGUI()
        {
            LabelField(AddedInPlayModeTitle, EditorStyles.boldLabel);

            if (SingletonContainer.Added.Count == 0)
            {
                using (new GUIBlock.ReadOnly())
                    LabelField(NoneText);

                return;
            }

            foreach (object singleton in SingletonContainer.Added)
                DrawAddedSingleton(singleton);
        }

        void DrawCatalogueList(SerializedProperty listProp)
        {
            if (listProp.arraySize == 0)
            {
                using (new GUIBlock.ReadOnly())
                    LabelField(NoneText);

                return;
            }

            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty enabledProp = listProp.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ToggleableSingleton<object>.enabled));
                SerializedProperty valueProp = listProp.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ToggleableSingleton<object>.value));

                using (new HorizontalScope())
                {
                    using (new GUIBlock.ReadOnly(Application.isPlaying))
                        PropertyField(enabledProp, GUIContent.none, GUILayout.Width(ToggleWidth));

                    using (new GUIBlock.ReadOnly(halfAlpha: (enabledProp.boolValue == false)))
                        PropertyField(valueProp, GUIContent.none);
                }
            }
        }

        void DrawAddedSingleton(object singleton)
        {
            if (singleton is Object unityObj)
            {
                using (new GUIBlock.ReadOnly(halfAlpha: false))
                    ObjectField(GUIContent.none, unityObj, typeof(Object), true);

                if (unityObj == null)
                    HelpBox(NullEntryText, MessageType.Warning);
            }
            else
            {
                using (new GUIBlock.ReadOnly(halfAlpha: false))
                    LabelField(singleton.ToString(), EditorStyles.textArea);
            }
        }
    }
}

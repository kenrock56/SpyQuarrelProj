
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
[CustomPropertyDrawer(typeof(GenericEntryBase<,>), true)]
public class GenericEntryDrawer : PropertyDrawer
{
    
    private const float Spacing = 10f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        
        EditorGUI.BeginProperty(position, label, property);

        
        float half = (position.width - Spacing) * 0.5f;
        
        var keyProp = property.FindPropertyRelative("Key");
        var valueProp = property.FindPropertyRelative("Value");
        
        var keyRect   = new Rect(position.x, position.y, half, position.height);
        var valueRect = new Rect(position.x + half + Spacing, position.y, half, position.height);
        
        EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
        EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
#endif


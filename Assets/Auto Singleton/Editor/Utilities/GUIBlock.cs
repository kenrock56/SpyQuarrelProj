using System;
using UnityEditor;
using UnityEngine;

namespace AutoSingletonEditor
{
    static class GUIBlock
    {
        /// <summary>
        /// <para><em> Make sure to instantiate it in a <see langword="using"/> statement. </em></para>
        /// </summary>
        public abstract class BlockBase : IDisposable
        {
            bool disposed;

            protected abstract void Destructor();

            void IDisposable.Dispose()
            {
                if (disposed == false)
                    Destructor();

                disposed = true;
            }
        }

        /// <summary>
        /// Edit the value of <see cref="GUI.enabled"/>, while optionally compensating for the half alpha effect.
        /// <inheritdoc/>
        /// </summary>
        public class ReadOnly : BlockBase
        {
            readonly bool wasEnabled;
            readonly bool gottaResetColor;

            public ReadOnly(bool readOnly = true, bool halfAlpha = true)
            {
                wasEnabled = GUI.enabled;
                GUI.enabled = !readOnly;

                if (readOnly && halfAlpha == false)
                {
                    GUI.color = new UnityEngine.Color(GUI.color.r, GUI.color.g, GUI.color.b, GUI.color.a * 2f);
                    gottaResetColor = true;
                }
            }

            protected override void Destructor()
            {
                GUI.enabled = wasEnabled;

                if (gottaResetColor)
                    GUI.color = new UnityEngine.Color(GUI.color.r, GUI.color.g, GUI.color.b, GUI.color.a / 2f);
            }
        }

        /// <summary>
        /// Edit the values of <see cref="GUI.color"/>, <see cref="GUI.backgroundColor"/> and/or <see cref="GUI.contentColor"/>.
        /// <inheritdoc/>
        /// </summary>
        public class Color : BlockBase
        {
            readonly UnityEngine.Color? oldColor;
            readonly UnityEngine.Color? oldBackgroundColor;
            readonly UnityEngine.Color? oldContentColor;

            public Color(UnityEngine.Color? color = null, UnityEngine.Color? backgroundColor = null, UnityEngine.Color? contentColor = null)
            {
                if (color != null)
                {
                    oldColor = GUI.color;
                    GUI.color = color.Value;
                }

                if (backgroundColor != null)
                {
                    oldBackgroundColor = GUI.backgroundColor;
                    GUI.backgroundColor = backgroundColor.Value;
                }

                if (contentColor != null)
                {
                    oldContentColor = GUI.contentColor;
                    GUI.contentColor = contentColor.Value;
                }
            }

            protected override void Destructor()
            {
                if (oldColor != null)
                    GUI.color = oldColor.Value;
                if (oldBackgroundColor != null)
                    GUI.backgroundColor = oldBackgroundColor.Value;
                if (oldContentColor != null)
                    GUI.contentColor = oldContentColor.Value;
            }
        }

        public enum IndentBlockType
        {
            /// <summary> Add the given value to the indent level. </summary>
            Add,
            /// <summary> Set the indent level to the given value. </summary>
            Set,
            /// <summary> Make sure the indent level is superior or equal to the given value. </summary>
            Floor,
            /// <summary> Make sure the indent level is inferior or equal to the given value. </summary>
            Ceil,
        }

        /// <summary>
        /// Edit the value of <see cref="EditorGUI.indentLevel"/>.
        /// <inheritdoc/>
        /// </summary>
        public class Indent : BlockBase
        {
            readonly int oldIndent;

            public Indent(IndentBlockType type, int indent = 1)
            {
                oldIndent = EditorGUI.indentLevel;

                switch (type)
                {
                    case IndentBlockType.Add:       EditorGUI.indentLevel += indent;                                    break;
                    case IndentBlockType.Set:       EditorGUI.indentLevel = indent;                                     break;
                    case IndentBlockType.Floor:     EditorGUI.indentLevel = Math.Max(EditorGUI.indentLevel, indent);    break;
                    case IndentBlockType.Ceil:      EditorGUI.indentLevel = Math.Min(EditorGUI.indentLevel, indent);    break;
                }
            }
            public Indent(int addedIndent = 1) : this(IndentBlockType.Add, addedIndent) { }

            protected override void Destructor()
            {
                EditorGUI.indentLevel = oldIndent;
            }
        }

        /// <summary>
        /// <para><b> Layout only!</b> </para>
        /// <para>    Elements displayed in this block will be centered horizontally. </para>
        /// <inheritdoc/>
        /// </summary>
        public class Centered : BlockBase
        {
            public Centered()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
            }

            protected override void Destructor()
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}

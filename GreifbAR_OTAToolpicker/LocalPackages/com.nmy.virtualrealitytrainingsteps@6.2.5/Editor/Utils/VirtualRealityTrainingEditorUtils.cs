using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace NMY.VirtualRealityTraining.Editor
{
    /// <summary>
    /// Some useful editor utils to draw foldouts, events in foldouts, script drawing and so on. 
    /// </summary>
    public static class VirtualRealityTrainingEditorUtils
    {
        /// <summary>
        /// Creates a stylish foldout editor widget.
        /// <br/><br/>
        /// Source: http://tips.hecomi.com/entry/2016/10/15/004144
        /// </summary>
        /// <param name="title">The title to be shown in the foldout.</param>
        /// <param name="display">Whether the foldout is expanded or not.</param>
        /// <returns></returns>
        public static bool FoldoutStylish(string title, bool display)
        {
            EditorGUILayout.Space();

            EditorGUI.indentLevel = 0;

            var style = new GUIStyle("ShurikenModuleTitle")
            {
                font          = new GUIStyle(EditorStyles.label).font,
                border        = new RectOffset(15, 7, 4, 4),
                fixedHeight   = 22,
                contentOffset = new Vector2(20f, -2f),
                margin = new RectOffset((EditorGUI.indentLevel + 1) * 16, 0, 0, 0)
            };

            var rect = GUILayoutUtility.GetRect(16f, 22f, style);
            GUI.Box(rect, title, style);

            var e = Event.current;

            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }
            else if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }

            return display;
        }
        
        /// <summary>
        /// Creates an indented editor widget for <see cref="UnityEvent"/>.
        /// </summary>
        /// <param name="propertyField">Instance of <see cref="SerializedProperty"/> that represents a <see cref="UnityEvent"/>.</param>
        /// <param name="guiContent">Instance of <see cref="GUIContent"/>.</param>
        public static void IndentUnityEventsInFoldout(SerializedProperty propertyField, GUIContent guiContent = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(10, false);
            EditorGUILayout.PropertyField(propertyField, guiContent);
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the script as disabled scoped.
        /// </summary>
        /// <param name="target">Instance of either <see cref="MonoBehaviour"/> or <see cref="ScriptableObject"/> to draw the script name from.</param>
        public static void DrawScript(object target)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                switch (target)
                {
                    case MonoBehaviour behaviour:
                        EditorGUILayout.ObjectField(EditorGUIUtility.TrTempContent("Script"),
                                                    MonoScript.FromMonoBehaviour(behaviour), typeof(MonoBehaviour),
                                                    false);
                        break;
                    case ScriptableObject scriptableObject:
                        EditorGUILayout.ObjectField(EditorGUIUtility.TrTempContent("Script"),
                                                    MonoScript.FromScriptableObject(scriptableObject),
                                                    typeof(ScriptableObject),
                                                    false);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Gets and returns all serialized property names in derived classes. 
        /// </summary>
        /// <param name="editor">Instance of <see cref="Editor"/> to be used.</param>
        /// <returns>A list of all names of serialized properties in the derived classes.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static List<string> GetDerivedSerializedPropertyNames(UnityEditor.Editor editor)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            var fields = editor.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var propertyNames = new List<string> { "m_Script" };
            foreach (var field in fields)
            {
                var value = field.GetValue(editor);
                if (value is SerializedProperty serializedProperty)
                {
                    propertyNames.Add(serializedProperty.name);
                }
            }

            return propertyNames;
        }
    }
}

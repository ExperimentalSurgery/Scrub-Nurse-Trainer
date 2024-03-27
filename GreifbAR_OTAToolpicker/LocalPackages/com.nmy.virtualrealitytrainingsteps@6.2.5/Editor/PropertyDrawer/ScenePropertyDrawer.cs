using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

namespace NMY.VirtualRealityTraining.Editor
{
    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class ScenePropertyDrawer : PropertyDrawer
    {
        private const string SCENE_LIST_ITEM               = "{0} ({1})";
        private const string SCENE_PATTERN                = @".+\/(.+)\.unity";
        private const string TYPE_WARNING_MESSAGE          = "{0} must be a string";
        private const string BUILD_SETTINGS_WARNING_MESSAGE = "No scenes in the build settings";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var validPropertyType = property.propertyType is SerializedPropertyType.String; //or SerializedPropertyType.Integer;
            var anySceneInBuildSettings = GetScenes().Length > 0;
            
            return (validPropertyType && anySceneInBuildSettings)
                ? EditorGUI.GetPropertyHeight(property)
                : EditorGUI.GetPropertyHeight(property) + GetHelpBoxHeight();
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var scenes                  = GetScenes();
            var anySceneInBuildSettings = scenes.Length > 0;
            if (!anySceneInBuildSettings)
            {
                DrawDefaultPropertyAndHelpBox(rect, property, BUILD_SETTINGS_WARNING_MESSAGE, MessageType.Warning);
                return;
            }

            var sceneOptions = GetSceneOptions(property, scenes);
            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    DrawPropertyForString(rect, property, label, scenes, sceneOptions);
                    break;
                // case SerializedPropertyType.Integer:
                //     DrawPropertyForInt(rect, property, label, sceneOptions);
                //     break;
                default:
                    var message = string.Format(TYPE_WARNING_MESSAGE, property.name);
                    DrawDefaultPropertyAndHelpBox(rect, property, message, MessageType.Warning);
                    break;
            }

            EditorGUI.EndProperty();
        }

        private static string[] GetScenes()
        {
            return EditorBuildSettings.scenes
                                      .Where(scene => scene.enabled)
                                      .Select(scene => Regex.Match(scene.path, SCENE_PATTERN).Groups[1].Value)
                                      .ToArray();
        }

        private string[] GetSceneOptions(SerializedProperty property, IReadOnlyList<string> scenes)
        {
            var displayOptions = new string[scenes.Count + 1];
            displayOptions[0] = SceneInBuildSettings(property) || property.stringValue == "" ? "(None)" : $"{property.stringValue} (-1)";

            for (var i = 0; i < scenes.Count; i++)
            {
                displayOptions[i + 1] = string.Format(SCENE_LIST_ITEM, scenes[i], i);
            }

            return displayOptions;
        }

        private bool SceneInBuildSettings(SerializedProperty property)
        {
            return GetScenes().Contains(property.stringValue);
        }

        private static void DrawPropertyForString(Rect rect, SerializedProperty property, GUIContent label,
                                                  string[] scenes, string[] sceneOptions)
        {
            var index    = IndexOf(scenes, property.stringValue);
            var newIndex = EditorGUI.Popup(rect, label.text, index + 1, sceneOptions);

            var newScene = "";

            if (index == -1 && newIndex == 0) newScene     = property.stringValue;
            else if (newIndex > 0) newScene = scenes[newIndex - 1];

            if (!property.stringValue.Equals(newScene, StringComparison.Ordinal))
            {
                property.stringValue = newScene;
            }
        }

        private static void DrawPropertyForInt(Rect rect, SerializedProperty property, GUIContent label,
                                               string[] sceneOptions)
        {
            var index    = property.intValue;
            var newIndex = EditorGUI.Popup(rect, label.text, index, sceneOptions);

            if (property.intValue != newIndex)
            {
                property.intValue = newIndex;
            }
        }

        private static int IndexOf(string[] scenes, string scene)
        {
            return Array.IndexOf(scenes, scene);
        }

        protected virtual float GetHelpBoxHeight()
        {
            return EditorGUIUtility.singleLineHeight * 2.0f;
        }

        private void DrawDefaultPropertyAndHelpBox(Rect rect, SerializedProperty property, string message,
                                                   MessageType messageType)
        {
            var indentLength = GetIndentLength(rect);
            var helpBoxRect = new Rect(
                rect.x + indentLength,
                rect.y,
                rect.width - indentLength,
                GetHelpBoxHeight());

            EditorGUI.HelpBox(helpBoxRect, message, MessageType.Warning);

            var propertyRect = new Rect(
                rect.x,
                rect.y + GetHelpBoxHeight(),
                rect.width,
                EditorGUI.GetPropertyHeight(property));

            EditorGUI.PropertyField(propertyRect, property, new GUIContent(property.displayName), true);
        }

        private static float GetIndentLength(Rect sourceRect)
        {
            var  indentRect   = EditorGUI.IndentedRect(sourceRect);
            var indentLength = indentRect.x - sourceRect.x;

            return indentLength;
        }
    }
}
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorParamAttribute))]
    public class AnimatorParamPropertyDrawer : PropertyDrawer
    {
        private const string InvalidAnimatorControllerWarningMessage = "Target animator controller is null";
        private const string InvalidTypeWarningMessage               = "{0} must be an object";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var animatorParamAttribute  = PropertyUtility.GetAttribute<AnimatorParamAttribute>(property);
            var validAnimatorController = GetAnimatorController(property, animatorParamAttribute.animatorName) != null;
            var validPropertyType =
                property.propertyType is SerializedPropertyType.Integer or SerializedPropertyType.String;

            return (validAnimatorController && validPropertyType)
                ? EditorGUI.GetPropertyHeight(property)
                : EditorGUI.GetPropertyHeight(property) + GetHelpBoxHeight();
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var animatorParamAttribute = (AnimatorParamAttribute)attribute;
            var animatorController     = GetAnimatorController(property, animatorParamAttribute.animatorName);
            if (animatorController == null)
            {
                DrawDefaultPropertyAndHelpBox(rect, property, InvalidAnimatorControllerWarningMessage,
                                              MessageType.Warning);
                return;
            }

            var paramCount         = animatorController.parameters.Length;
            var animatorParameters = new List<AnimatorControllerParameter>(paramCount);
            for (var i = 0; i < paramCount; i++)
            {
                var parameter = animatorController.parameters[i];
                if (animatorParamAttribute.animatorParamType == null ||
                    parameter.type == animatorParamAttribute.animatorParamType)
                {
                    animatorParameters.Add(parameter);
                }
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    DrawPropertyForInt(rect, property, label, animatorParameters);
                    break;
                case SerializedPropertyType.String:
                    DrawPropertyForString(rect, property, label, animatorParameters);
                    break;
                default:
                    DrawDefaultPropertyAndHelpBox(rect, property,
                                                  string.Format(InvalidTypeWarningMessage, property.name),
                                                  MessageType.Warning);
                    break;
            }

            EditorGUI.EndProperty();
        }

        private static void DrawPropertyForInt(Rect rect, SerializedProperty property, GUIContent label,
                                               List<AnimatorControllerParameter> animatorParameters)
        {
            int paramNameHash = property.intValue;
            int index         = 0;

            for (int i = 0; i < animatorParameters.Count; i++)
            {
                if (paramNameHash == animatorParameters[i].nameHash)
                {
                    index = i + 1; // +1 because the first option is reserved for (None)
                    break;
                }
            }

            string[] displayOptions = GetDisplayOptions(animatorParameters);

            int newIndex = EditorGUI.Popup(rect, label.text, index, displayOptions);
            int newValue = newIndex == 0 ? 0 : animatorParameters[newIndex - 1].nameHash;

            if (property.intValue != newValue)
            {
                property.intValue = newValue;
            }
        }

        private static void DrawPropertyForString(Rect rect, SerializedProperty property, GUIContent label,
                                                  List<AnimatorControllerParameter> animatorParameters)
        {
            string paramName = property.stringValue;
            int    index     = 0;

            for (int i = 0; i < animatorParameters.Count; i++)
            {
                if (paramName.Equals(animatorParameters[i].name, System.StringComparison.Ordinal))
                {
                    index = i + 1; // +1 because the first option is reserved for (None)
                    break;
                }
            }

            string[] displayOptions = GetDisplayOptions(animatorParameters);

            int    newIndex = EditorGUI.Popup(rect, label.text, index, displayOptions);
            string newValue = newIndex == 0 ? null : animatorParameters[newIndex - 1].name;

            if (!property.stringValue.Equals(newValue, System.StringComparison.Ordinal))
            {
                property.stringValue = newValue;
            }
        }

        private static string[] GetDisplayOptions(IReadOnlyList<AnimatorControllerParameter> animatorParameters)
        {
            var displayOptions = new string[animatorParameters.Count + 1];
            displayOptions[0] = "(None)";

            for (int i = 0; i < animatorParameters.Count; i++)
            {
                displayOptions[i + 1] = animatorParameters[i].name;
            }

            return displayOptions;
        }

        private static AnimatorController GetAnimatorController(SerializedProperty property, string animatorName)
        {
            var target = PropertyUtility.GetTargetObjectWithProperty(property);

            var animatorFieldInfo = ReflectionUtility.GetField(target, animatorName);
            if (animatorFieldInfo != null &&
                animatorFieldInfo.FieldType == typeof(Animator))
            {
                var animator = animatorFieldInfo.GetValue(target) as Animator;
                if (animator != null)
                {
                    AnimatorController animatorController = animator.runtimeAnimatorController as AnimatorController;
                    return animatorController;
                }
            }

            var animatorPropertyInfo = ReflectionUtility.GetProperty(target, animatorName);
            if (animatorPropertyInfo != null &&
                animatorPropertyInfo.PropertyType == typeof(Animator))
            {
                var animator = animatorPropertyInfo.GetValue(target) as Animator;
                if (animator != null)
                {
                    AnimatorController animatorController = animator.runtimeAnimatorController as AnimatorController;
                    return animatorController;
                }
            }

            var animatorGetterMethodInfo = ReflectionUtility.GetMethod(target, animatorName);
            if (animatorGetterMethodInfo != null &&
                animatorGetterMethodInfo.ReturnType == typeof(Animator) &&
                animatorGetterMethodInfo.GetParameters().Length == 0)
            {
                var animator = animatorGetterMethodInfo.Invoke(target, null) as Animator;
                if (animator != null)
                {
                    var animatorController = animator.runtimeAnimatorController as AnimatorController;
                    return animatorController;
                }
            }

            return null;
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

        protected virtual float GetHelpBoxHeight()
        {
            return EditorGUIUtility.singleLineHeight * 2.0f;
        }

        private static float GetIndentLength(Rect sourceRect)
        {
            Rect  indentRect   = EditorGUI.IndentedRect(sourceRect);
            float indentLength = indentRect.x - sourceRect.x;

            return indentLength;
        }
    }
}
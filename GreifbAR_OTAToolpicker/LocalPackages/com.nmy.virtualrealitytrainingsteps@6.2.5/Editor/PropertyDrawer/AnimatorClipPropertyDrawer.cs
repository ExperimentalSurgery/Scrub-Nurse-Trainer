using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorClipAttribute))]
    public class AnimatorClipPropertyDrawer : PropertyDrawer
    {
        private const string InvalidAnimatorControllerWarningMessage = "Target animator controller is null";
        private const string InvalidTypeWarningMessage               = "{0} must be an object";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var animatorClipAttribute   = PropertyUtility.GetAttribute<AnimatorClipAttribute>(property);
            var validAnimatorController = GetAnimatorController(property, animatorClipAttribute.animatorName) != null;
            var validPropertyType       = property.propertyType == SerializedPropertyType.ObjectReference;

            return (validAnimatorController && validPropertyType)
                ? EditorGUI.GetPropertyHeight(property)
                : EditorGUI.GetPropertyHeight(property) + GetHelpBoxHeight();
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var animatorClipAttribute = (AnimatorClipAttribute)attribute;
            var animatorController    = GetAnimatorController(property, animatorClipAttribute.animatorName);
            if (animatorController == null)
            {
                DrawDefaultPropertyAndHelpBox(rect, property, InvalidAnimatorControllerWarningMessage,
                                              MessageType.Warning);
                return;
            }

            var clipCount     = animatorController.animationClips.Length;
            var animatorClips = new List<AnimationClip>(clipCount);
            for (var i = 0; i < clipCount; i++)
            {
                var clip = animatorController.animationClips[i];
                if (!animatorClips.Contains(clip)) animatorClips.Add(clip);
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    DrawPropertyForObject(rect, property, label, animatorClips);
                    break;
                default:
                    DrawDefaultPropertyAndHelpBox(rect, property,
                                                  string.Format(InvalidTypeWarningMessage, property.name),
                                                  MessageType.Warning);
                    break;
            }

            EditorGUI.EndProperty();
        }

        private void DrawPropertyForObject(Rect rect, SerializedProperty property, GUIContent label,
                                           List<AnimationClip> animatorClips)
        {
            var oldValue = property.objectReferenceValue;
            var index    = 0;

            for (var i = 0; i < animatorClips.Count; i++)
            {
                if (oldValue == animatorClips[i])
                {
                    index = i + 1; // +1 because the first option is reserved for (None)
                    break;
                }
            }

            var displayOptions = GetDisplayOptions(animatorClips);

            var newIndex = EditorGUI.Popup(rect, label.text, index, displayOptions);
            var newValue = newIndex == 0 ? null : animatorClips[newIndex - 1];

            if (oldValue != newValue)
            {
                property.objectReferenceValue = newValue;
            }
        }

        private string[] GetDisplayOptions(IReadOnlyList<AnimationClip> animatorClips)
        {
            var displayOptions = new string[animatorClips.Count + 1];
            displayOptions[0] = "(None)";

            for (int i = 0; i < animatorClips.Count; i++)
            {
                displayOptions[i + 1] = animatorClips[i].name;
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
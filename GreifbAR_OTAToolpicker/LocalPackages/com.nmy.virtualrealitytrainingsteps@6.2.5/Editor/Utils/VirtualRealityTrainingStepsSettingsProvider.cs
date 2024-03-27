using NMY.VirtualRealityTraining.Steps;
using UnityEditor;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Editor
{
    /// <summary>
    /// Settings provider for  Virtual Reality Training Steps.
    /// <remarks>
    /// <para>You can define in the Preferences if you want to add 
    /// <see cref="BaseTrainingStepUnityEvents"/> and <see cref="BaseTrainingStepDebug"/> automatically if a
    /// derived class of <see cref="BaseTrainingStep"/> is added to a <see cref="GameObject"/>.</para>
    /// </remarks>
    /// </summary>
    public class VirtualRealityTrainingStepsSettingsProvider : SettingsProvider
    {
        private static readonly GUIContent AddStepUnityEvents  = new(
            "Step Unity Events", 
            "Adds automatically a BaseTrainingStepUnityEvent component if a new step was added to a GameObject.");
        private static readonly  GUIContent AddStepDebug       = new(
            "Step Debug", 
            "Adds automatically a BaseTrainingStepDebug component if a new step was added to a GameObject.");

        /// <summary>
        /// Creates a new SettingsProvider.
        /// </summary>
        /// <returns></returns>
        [SettingsProvider]
        private static SettingsProvider CreateProjectSettingsProvider() => new VirtualRealityTrainingStepsSettingsProvider();

        private VirtualRealityTrainingStepsSettingsProvider() : 
            base("Preferences/NMY/Virtual Reality Training Steps", SettingsScope.User, new[] {"NMY", "Training", "Debug", "Events"})
        {
        }

        /// <summary>
        /// <inheritdoc cref="SettingsProvider.OnGUI"/>
        /// </summary>
        /// <param name="searchContext"><inheritdoc cref="SettingsProvider.OnGUI"/></param>
        public override void OnGUI(string searchContext)
        {
            GUILayout.Space(10);

            var rect = GetControlRect();
            var enableUnityEvents = EditorPrefs.GetBool("NMY.AddBaseTrainingStepUnityEvents", defaultValue: true);
            enableUnityEvents = EditorGUI.Toggle(rect, AddStepUnityEvents, enableUnityEvents);
            EditorPrefs.SetBool("NMY.AddBaseTrainingStepUnityEvents", enableUnityEvents);
            
            rect = GetControlRect();
            var enableDebug = EditorPrefs.GetBool("NMY.AddBaseTrainingStepDebug", defaultValue: true);
            enableDebug = EditorGUI.Toggle(rect, AddStepDebug, enableDebug);
            EditorPrefs.SetBool("NMY.AddBaseTrainingStepDebug", enableDebug);
        }

        /// <summary>
        /// Calculates and returns the control rect.
        /// </summary>
        /// <returns>Control Rect for the Settings.</returns>
        private static Rect GetControlRect()
        {
            // Small indent to match the other preference editors.
            const float indent = 8;

            var rect = EditorGUILayout.GetControlRect();
            rect.xMin += indent;
            return rect;
        }
    }
}
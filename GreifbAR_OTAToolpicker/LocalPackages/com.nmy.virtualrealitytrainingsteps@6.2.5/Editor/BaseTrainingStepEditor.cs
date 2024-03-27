using NMY.VirtualRealityTraining.Steps;
using UnityEditor;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Editor
{
    /// <summary>
    /// Custom Editor for <see cref="BaseTrainingStep"/>.
    /// It provides custom drawings for step state, Activatables and timeouts.
    /// </summary>
    [CustomEditor(typeof(BaseTrainingStep), true)]
    [CanEditMultipleObjects]
    public class BaseTrainingStepEditor : UnityEditor.Editor
    {
        protected string[] knownSerializedPropertyNames { get; set; }

        private bool _initializedKnownSerializedPropertyNames;

        protected SerializedProperty developerDescription;
        protected SerializedProperty stepActivatables;
        protected SerializedProperty persistantActivatables;
        protected SerializedProperty persistantActivatablesDeactivationSteps;
        protected SerializedProperty nextSteps;

        protected SerializedProperty timeoutBeforeStepStart;
        protected SerializedProperty timeoutBeforeStepFinish;
        protected SerializedProperty timeoutBeforeStepComplete;
        
        protected SerializedProperty timeoutActivatablesActivating;
        protected SerializedProperty timeoutActivatablesActivated;
        protected SerializedProperty timeoutActivatablesDeactivating;
        protected SerializedProperty timeoutActivatablesDeactivated;
        
        protected SerializedProperty timeoutPersistantActivatablesActivating;
        protected SerializedProperty timeoutPersistantActivatablesActivated;
        protected SerializedProperty timeoutPersistantActivatablesDeactivating;
        protected SerializedProperty timeoutPersistantActivatablesDeactivated;

        private static bool _activatablesActive;
        private static bool _activatablesTimeouts;
 
        protected virtual void OnEnable()
        {
            developerDescription = serializedObject.FindProperty("_developerDescription");
            stepActivatables     = serializedObject.FindProperty("_stepActivatables");
            nextSteps            = serializedObject.FindProperty("_nextSteps");
            
            persistantActivatables = serializedObject.FindProperty("_persistantActivatables");
            persistantActivatablesDeactivationSteps = serializedObject.FindProperty("_persistantActivatablesDeactivationSteps");

            timeoutBeforeStepStart    = serializedObject.FindProperty("_timeoutBeforeStepStart");
            timeoutBeforeStepFinish   = serializedObject.FindProperty("_timeoutBeforeStepFinish");
            timeoutBeforeStepComplete = serializedObject.FindProperty("_timeoutBeforeStepComplete");

            timeoutActivatablesActivating   = serializedObject.FindProperty("_timeoutActivatablesActivating");
            timeoutActivatablesActivated    = serializedObject.FindProperty("_timeoutActivatablesActivated");
            timeoutActivatablesDeactivating = serializedObject.FindProperty("_timeoutActivatablesDeactivating");
            timeoutActivatablesDeactivated  = serializedObject.FindProperty("_timeoutActivatablesDeactivated");
            
            timeoutPersistantActivatablesActivating   = serializedObject.FindProperty("_timeoutPersistantActivatablesActivating");
            timeoutPersistantActivatablesActivated    = serializedObject.FindProperty("_timeoutPersistantActivatablesActivated");
            timeoutPersistantActivatablesDeactivating = serializedObject.FindProperty("_timeoutPersistantActivatablesDeactivating");
            timeoutPersistantActivatablesDeactivated  = serializedObject.FindProperty("_timeoutPersistantActivatablesDeactivated");
        }

        /// <summary>
        /// <inheritdoc cref="Editor"/>
        /// </summary>
        public override void OnInspectorGUI()
        {
            InitializeKnownSerializedPropertyNames();

            serializedObject.Update();

            DrawInspector();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the inspector.
        /// </summary>
        protected virtual void DrawInspector()
        {
            // Show the script
            VirtualRealityTrainingEditorUtils.DrawScript(target);
            // Draw the properties of BaseTrainingStep and all other derived
            DrawProperties();
            // Draw all other properties that are not drawn in DrawProperties()
            DrawPropertiesExcluding(serializedObject, knownSerializedPropertyNames);
        }

        /// <summary>
        /// Draws the properties of <see cref="BaseTrainingStep"/> stylized.
        /// This method should be used if derived classes want to stylize some properties.
        /// All other  properties that are not stylized in this method will be drawn afterwards.
        /// </summary>
        protected virtual void DrawProperties()
        {
            EditorGUILayout.PropertyField(developerDescription);

            DrawStepState();
            DrawActivatables();
            DrawTimeouts();

            EditorGUILayout.PropertyField(nextSteps);
        }

        /// <summary>
        /// Draws all Timeouts as foldout list.
        /// </summary>
        private void DrawTimeouts()
        {
            var timeoutsTitle =
                $"Timeouts ({timeoutBeforeStepStart.floatValue} | {timeoutBeforeStepFinish.floatValue} | " +
                $"{timeoutBeforeStepComplete.floatValue} " +
                $"| {timeoutActivatablesActivating.floatValue} | {timeoutActivatablesActivated.floatValue} " +
                $"| {timeoutActivatablesDeactivating.floatValue} | {timeoutActivatablesDeactivated.floatValue} " +
                $"| {timeoutPersistantActivatablesActivating.floatValue} | {timeoutPersistantActivatablesActivated.floatValue} " +
                $"| {timeoutPersistantActivatablesDeactivating.floatValue} | {timeoutPersistantActivatablesDeactivated.floatValue}" +
                $")";
            _activatablesTimeouts =
                VirtualRealityTrainingEditorUtils.FoldoutStylish(timeoutsTitle, _activatablesTimeouts);

            if (!_activatablesTimeouts) return;

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(timeoutBeforeStepStart);
            EditorGUILayout.PropertyField(timeoutBeforeStepFinish);
            EditorGUILayout.PropertyField(timeoutBeforeStepComplete);
            
            EditorGUILayout.PropertyField(timeoutActivatablesActivating);
            EditorGUILayout.PropertyField(timeoutActivatablesActivated);
            EditorGUILayout.PropertyField(timeoutActivatablesDeactivating);
            EditorGUILayout.PropertyField(timeoutActivatablesDeactivated);
            
            EditorGUILayout.PropertyField(timeoutPersistantActivatablesActivating);
            EditorGUILayout.PropertyField(timeoutPersistantActivatablesActivated);
            EditorGUILayout.PropertyField(timeoutPersistantActivatablesDeactivating);
            EditorGUILayout.PropertyField(timeoutPersistantActivatablesDeactivated);
            --EditorGUI.indentLevel;
        }

        /// <summary>
        /// Draws the all Activatables as foldout list.
        /// </summary>
        private void DrawActivatables()
        {
            var activatablesTitle =
                $"Activatables ({stepActivatables.arraySize} " +
                $"| {persistantActivatables.arraySize} " +
                $"| {persistantActivatablesDeactivationSteps.arraySize})";
            _activatablesActive =
                VirtualRealityTrainingEditorUtils.FoldoutStylish(activatablesTitle, _activatablesActive);

            if (!_activatablesActive) return;

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(stepActivatables);
            EditorGUILayout.PropertyField(persistantActivatables);
            EditorGUILayout.PropertyField(persistantActivatablesDeactivationSteps);
            --EditorGUI.indentLevel;
        }

        /// <summary>
        /// Draws the state of the step.
        /// </summary>
        private void DrawStepState()
        {
            var stateStyle = new GUIStyle();

            var stepState = ((BaseTrainingStep)serializedObject.targetObject).stepState;
            stateStyle.normal.textColor = stepState switch
            {
                BaseTrainingStep.StepState.StepWaiting   => Color.yellow,
                BaseTrainingStep.StepState.StepStarted   => Color.green,
                BaseTrainingStep.StepState.StepFinished  => Color.cyan,
                BaseTrainingStep.StepState.StepCompleted => Color.magenta,
                BaseTrainingStep.StepState.StepStopped   => Color.red,
                _                                        => Color.white
            };

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("Step State", stepState.ToString(), stateStyle);
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Saves all known serialized properties to be used within derived classes.
        /// 
        /// This method is automatically called by <see cref="OnInspectorGUI"/> to
        /// initialize <see cref="knownSerializedPropertyNames"/> if necessary.
        /// </summary>
        protected virtual void InitializeKnownSerializedPropertyNames()
        {
            if (_initializedKnownSerializedPropertyNames) return;

            knownSerializedPropertyNames =
                VirtualRealityTrainingEditorUtils.GetDerivedSerializedPropertyNames(this).ToArray();
            _initializedKnownSerializedPropertyNames = true;
        }
    }
}
using System;
using UnityEditor;

namespace NMY.VirtualRealityTraining.Editor
{
    /// <summary>
    /// Custom editor for the <see cref="BaseTrainingStepUnityEvents"/> class.
    /// It draws the events sorted in foldables.
    /// </summary>
    [CustomEditor(typeof(BaseTrainingStepUnityEvents), true)]
    [CanEditMultipleObjects]
    public class BaseTrainingStepUnityEventsEditor : UnityEditor.Editor
    {
        private SerializedProperty _stepStartedEvent;
        private SerializedProperty _stepFinishedEvent;
        private SerializedProperty _stepCompletedEvent;
        
        private SerializedProperty _stepActionStartedEvent;
        private SerializedProperty _stepActionFinishedEvent;
        
        private SerializedProperty _activatablesActivating;
        private SerializedProperty _activatablesActivated;
        
        private SerializedProperty _activatablesDeactivating;
        private SerializedProperty _activatablesDeactivated;
        
        private SerializedProperty _persistantActivatablesActivating;
        private SerializedProperty _persistantActivatablesActivated;
        
        private SerializedProperty _persistantActivatablesDeactivating;
        private SerializedProperty _persistantActivatablesDeactivated;

        private BaseTrainingStepUnityEvents events => (BaseTrainingStepUnityEvents)target;

        /// <summary>
        /// Finds all properties.
        /// </summary>
        private void OnEnable()
        {
            _stepStartedEvent        = serializedObject.FindProperty(nameof(events.stepStartedEvent));
            _stepFinishedEvent       = serializedObject.FindProperty(nameof(events.stepFinishedEvent));
            _stepCompletedEvent      = serializedObject.FindProperty(nameof(events.stepCompletedEvent));
            
            _stepActionStartedEvent  = serializedObject.FindProperty(nameof(events.stepActionStartedEvent));
            _stepActionFinishedEvent = serializedObject.FindProperty(nameof(events.stepActionFinishedEvent));

            _activatablesActivating   = serializedObject.FindProperty(nameof(events.activatablesActivating));
            _activatablesActivated    = serializedObject.FindProperty(nameof(events.activatablesActivated));
            _activatablesDeactivating = serializedObject.FindProperty(nameof(events.activatablesDeactivating));
            _activatablesDeactivated  = serializedObject.FindProperty(nameof(events.activatablesDeactivated));

            _persistantActivatablesActivating   = serializedObject.FindProperty(nameof(events.persistantActivatablesActivating));
            _persistantActivatablesActivated    = serializedObject.FindProperty(nameof(events.persistantActivatablesActivated));
            _persistantActivatablesDeactivating = serializedObject.FindProperty(nameof(events.persistantActivatablesDeactivating));
            _persistantActivatablesDeactivated  = serializedObject.FindProperty(nameof(events.persistantActivatablesDeactivated));
        }

        /// <summary>
        /// <inheritdoc cref="Editor"/>
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            VirtualRealityTrainingEditorUtils.DrawScript(target);

            DrawStepEvents();
            DrawActionEvents();
            DrawActivatableEvents();
            DrawPersistantEvents();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws step events (started, finished, completed).
        /// </summary>
        private void DrawStepEvents()
        {
            var title = $"Step Events ({EventListenerCount(_stepStartedEvent)} " +
                        $"| {EventListenerCount(_stepFinishedEvent)} " +
                        $"| {EventListenerCount(_stepCompletedEvent)})";

            _stepStartedEvent.isExpanded =
                VirtualRealityTrainingEditorUtils.FoldoutStylish(title, _stepStartedEvent.isExpanded);

            if (!_stepStartedEvent.isExpanded) return;
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_stepStartedEvent);
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_stepFinishedEvent);
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_stepCompletedEvent);
        }

        /// <summary>
        /// Draws step action events (action started, action finished).
        /// </summary>
        private void DrawActionEvents()
        {
            var title = $"Action Events ({EventListenerCount(_stepActionStartedEvent)} " +
                        $"| {EventListenerCount(_stepActionFinishedEvent)})";
            _stepActionStartedEvent.isExpanded =
                VirtualRealityTrainingEditorUtils.FoldoutStylish(title, _stepActionStartedEvent.isExpanded);

            if (!_stepActionStartedEvent.isExpanded) return;
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_stepActionStartedEvent);
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_stepActionFinishedEvent);
        }

        /// <summary>
        /// Draws step activatable events (activatables activated, activatables deactivated).
        /// </summary>
        private void DrawActivatableEvents()
        {
            var title = $"Activatable Events (" +
                        $"{EventListenerCount(_activatablesActivating)} " +
                        $"| {EventListenerCount(_activatablesActivated)} " +
                        $"| {EventListenerCount(_activatablesDeactivating)} " +
                        $"| {EventListenerCount(_activatablesDeactivated)}" +
                        $")";
            _activatablesActivated.isExpanded =
                VirtualRealityTrainingEditorUtils.FoldoutStylish(title, _activatablesActivated.isExpanded);

            if (!_activatablesActivated.isExpanded) return;
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_activatablesActivating);
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_activatablesActivated);
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_activatablesDeactivating);
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_activatablesDeactivated);
        }

        /// <summary>
        /// Draws step persistant events (persistant activatables activated, persistant activatables deactivated).
        /// </summary>
        private void DrawPersistantEvents()
        {
            var title = $"Persistant Activatable Events (" +
                        $"{EventListenerCount(_persistantActivatablesActivating)} " +
                        $"| {EventListenerCount(_persistantActivatablesActivated)} " +
                        $"| {EventListenerCount(_persistantActivatablesDeactivating)} " +
                        $"| {EventListenerCount(_persistantActivatablesDeactivated)}" +
                        $")";
            _persistantActivatablesActivated.isExpanded =
                VirtualRealityTrainingEditorUtils.FoldoutStylish(title, _persistantActivatablesActivated.isExpanded);

            if (!_persistantActivatablesActivated.isExpanded) return;
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_persistantActivatablesActivating);
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_persistantActivatablesActivated);
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_persistantActivatablesDeactivating);
            VirtualRealityTrainingEditorUtils.IndentUnityEventsInFoldout(_persistantActivatablesDeactivated);
        }

        /// <summary>
        /// Calculates and returns the number of event listener for the given <paramref name="serializedProperty"></paramref>.
        /// </summary>
        /// <param name="serializedProperty">The <see cref="SerializedProperty"/> instance from which the number of
        /// event listener is calculated.</param>
        /// <returns></returns>
        private static int EventListenerCount(SerializedProperty serializedProperty)
        {
            var persistentCalls = serializedProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
            return persistentCalls.arraySize;
        }
    }
}
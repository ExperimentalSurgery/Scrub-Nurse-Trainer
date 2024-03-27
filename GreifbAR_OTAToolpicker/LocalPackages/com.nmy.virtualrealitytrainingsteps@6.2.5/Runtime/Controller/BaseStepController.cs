using NMY.VirtualRealityTraining.Steps;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
#endif

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// Abstract implementation of a StepController. It should be used as base for alle StepController instances.
    /// Inherits from <see cref="NetworkBehaviour"/>.
    /// <remarks>
    /// <para>It provides functionality to update all <see cref="BaseTrainingStep._nextSteps"/> automatically
    /// if <see cref="_rootTrainingStep"/> is provided and Alt+A is pressed.</para>
    /// </remarks>
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public abstract class BaseStepController : NetworkBehaviour
    {
        /// <summary>
        /// The root node of this step sequence.
        /// </summary>
        [Tooltip("The root node of this step sequence.")]
        [SerializeField] private BaseTrainingStep _rootTrainingStep;

        /// <summary>
        /// The start node within the root sequence.
        /// </summary>
        [Tooltip("The start node within the root sequence.")]
        [SerializeField] private BaseTrainingStep _startTrainingStep;

        /// <summary>
        /// Property to get read access to <see cref="_rootTrainingStep"/>.
        /// </summary>
        protected BaseTrainingStep rootTrainingStep  => _rootTrainingStep;
        
        /// <summary>
        /// Property to get read and write access to <see cref="_startTrainingStep"/>.
        /// </summary>
        protected BaseTrainingStep startTrainingStep {
            get => _startTrainingStep;
            set => _startTrainingStep = value;
        }
        
        // TODO: Remove this when NGO 1.7.1 is released; just a workaround for a bug in 1.7.0
        // https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/2755
        [ServerRpc] private void Workaround_ServerRpc() {}

#if UNITY_EDITOR
        /// <summary>
        /// Editor feature to update all NextStep entries according to current step hierarchy,
        /// taking into account inactive <see cref="GameObject"/>.
        /// Support for Undo, Prefab overrides, and editing in PrefabMode.
        /// </summary>
        [UnityEditor.MenuItem("Tools/Editor/Auto-assign all Next Steps &a", false, 2000)]
        private static void UpdateAllNextStepsEntries()
        {
            if (Application.isPlaying)
            {
                Debug.LogError($"{typeof(BaseStepController)}: Updating NextSteps is not allowed in Play mode!");
                return;
            }

            var rootObjects = new List<GameObject>();
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() is { } prefabMode)
            {
                // currently in PrefabMode, so only check associated objects
                if (prefabMode.prefabContentsRoot && prefabMode.prefabContentsRoot.activeSelf)
                {
                    if (prefabMode.prefabContentsRoot.GetComponent<BaseTrainingStep>() is { } step)
                    {
                        if (step != null)
                        {
                            // prefab root has a BaseTrainingStep
                            FillNextStepsRecursively(step);
                            Debug.Log($"{typeof(BaseStepController)}: Updated all NextSteps of \"{prefabMode.prefabContentsRoot.name}\" prefab.");
                        }
                        else
                        {
                            Debug.LogWarning($"{typeof(BaseStepController)}: NextSteps of \"{prefabMode.prefabContentsRoot.name}\" could not be updated; step is null!");
                        }
                    }
                    else
                    {
                        // otherwise, check direct children
                        foreach (var child in prefabMode.prefabContentsRoot.transform.Cast<Transform>())
                            rootObjects.Add(child.gameObject);
                    }
                }
            }
            else if (FindObjectsOfType<BaseStepController>() is { } stepControllers)
            {
                // not in PrefabMode? check for TrainingStepController and use its _rootTrainingStep
                foreach (var stepController in stepControllers)
                {
                    if (stepController.rootTrainingStep != null)
                    {
                        FillNextStepsRecursively(stepController.rootTrainingStep);
                        Debug.Log($"{typeof(BaseStepController)}: Updated all NextSteps of \"{stepController.rootTrainingStep}\" root.");
                    }
                    else
                    {
                        Debug.LogWarning($"{typeof(BaseStepController)}: NextSteps of \"{stepController}\" could not be updated; rootTrainingStep is null!");
                    }
                }
            }
            else
            {
                // otherwise, check all root GOs of scene
                rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().ToList();
            }

            // if we found any rootObjects that need checking (either in PrefabMode, or in the current Scene), process them
            if (rootObjects.Count > 0)
            {
                var numStepRootsFound = 0;
                // check all candidate objects for a BaseTrainingStep
                foreach (var rootObj in rootObjects)
                {
                    if (rootObj.activeSelf && rootObj.GetComponent<BaseTrainingStep>() is { } step)
                    {
                        if (step != null)
                        {
                            Debug.Log($"{typeof(BaseStepController)}: Updating root object \"{rootObj.name}\"...");
                            FillNextStepsRecursively(step);
                            numStepRootsFound++;
                        }
                        else
                        {
                            Debug.LogWarning($"{typeof(BaseStepController)}: NextSteps could not be updated; step is null!");
                        }
                    }
                }

                if (numStepRootsFound == 0)
                    Debug.LogWarning($"{typeof(BaseStepController)}: No active root GameObjects with a BaseTrainingStep found!");
                else if (numStepRootsFound > 1)
                    Debug.LogWarning(
                        $"{typeof(BaseStepController)}: Found multiple active root GameObjects with a BaseTrainingStep attached! Updated all of them.");
                else
                    Debug.Log($"{typeof(BaseStepController)}: Updated all NextSteps of current scene.");
            }
        }

        /// <summary>
        /// Recursively fills the next steps from <paramref name="step"/>.
        /// </summary>
        /// <param name="step">Instance of <see cref="BaseTrainingStep"/> where its next steps should be filled.</param>
        private static void FillNextStepsRecursively(BaseTrainingStep step)
        {
            var nextSteps = new List<BaseTrainingStep>();
            // check all direct children for BaseTrainingSteps
            Transform nextStepsNamedChild = null;
            foreach (var child in step.transform.Cast<Transform>())
            {
                if (!child.gameObject.activeInHierarchy) continue;
                if (child.GetComponent<BaseTrainingStep>() is { } childStep)
                {
                    if (!childStep.enabled) continue;
                    
                    nextSteps.Add(childStep);
                    // recurse depth-first
                    FillNextStepsRecursively(childStep);
                }
                else if (!nextStepsNamedChild &&
                         child.name == "[Next Steps]") // == first object explicitly named "[Next Steps]"
                    nextStepsNamedChild = child;
                else
                {
                    // check children of all OTHER children (which may be disconnected DecisionStep objects etc.),
                    // but just update THEIR nextSteps, without adding them to THIS object
                    foreach (var grandChild in child.Cast<Transform>())
                    {
                        if (grandChild.gameObject.activeInHierarchy &&
                            grandChild.GetComponent<BaseTrainingStep>() is { enabled: true } grandChildStep)
                        {
                            // recurse depth-first
                            FillNextStepsRecursively(grandChildStep);
                        }
                    }
                }
            }

            if (nextSteps.Count == 0 && nextStepsNamedChild)
            {
                // none found? check "[Next Steps]"
                // check direct children of a GO explicitly named "[Next Steps]", if we have one
                foreach (var child in nextStepsNamedChild.Cast<Transform>())
                {
                    if (child.gameObject.activeInHierarchy &&
                        child.GetComponent<BaseTrainingStep>() is { enabled: true } childStep)
                    {
                        nextSteps.Add(childStep);
                        // recurse depth-first
                        FillNextStepsRecursively(childStep);
                    }
                }
            }

            // override current list
            step.OverrideNextSteps(nextSteps);
        }
#endif
    }
}
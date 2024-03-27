using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

namespace NMY.Editor {
	
	public class SimpleAnimatorActivatableStateGenerator : EditorWindow  {


		public Object parentFolderObj;
		private string parentFolderTooltip = "A folder named 'Animator name' containing Animator and animations will be created below this parent object.";

		public string animatorName = "Foobar";
		private string animatorNameTooltip = "The name of the Animator to create. Example: If the name is 'Foobar' an animator named FoobarAnimator will be created.";

		public string shownParameterName = "show";
		private string shownParameterNameTooltip = "The name of the boolean parameter in the animator.";

		public string shownStateName = "Shown";
		public string initialStateName = "Initial";
		public string hiddenStateName = "Hidden";

		public Object animatorTemplate;

		private string parentFolder = "Assets/_GeneralLighting";

		[MenuItem ("NMY/SimpleAnimatorActivatable/SimpleAnimator wizard..")]
		static void Init () {
			// Get existing open window or if none, make a new one:
			SimpleAnimatorActivatableStateGenerator window = EditorWindow.GetWindow<SimpleAnimatorActivatableStateGenerator>();
			window.titleContent =  new GUIContent("SAA State Generator");
		}	

		void OnGUI() {
			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			animatorName = EditorGUILayout.TextField(new GUIContent("Animator name", animatorNameTooltip), animatorName);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			parentFolderObj = EditorGUILayout.ObjectField(new GUIContent("ParentFolder", parentFolderTooltip), parentFolderObj, typeof(Object), false);
			if (parentFolderObj!=null) {
				// Debug.Log("Folder path: " + );
				parentFolder = AssetDatabase.GetAssetOrScenePath(parentFolderObj);
			}
				
			EditorGUILayout.Space();

			shownParameterName = EditorGUILayout.TextField( new GUIContent("Shown parameter name", shownParameterNameTooltip), shownParameterName);
			initialStateName = EditorGUILayout.TextField("Initial state name", initialStateName);
			hiddenStateName = EditorGUILayout.TextField("Hidden state name", hiddenStateName);
			shownStateName = EditorGUILayout.TextField("Shown state name", shownStateName);

			if(parentFolderObj != null) {
				string infoText = "The following structure and assets will be created:\n" + animatorName + " [Folder]\n" +
                    "    " + animatorName + "Animator [Animator]\n" +
					"    " + animatorName + "_" + initialStateName + " [Animation]\n" +
					"    " + animatorName + "_" + hiddenStateName + " [Animation]\n" +
					"    " + animatorName + "_" + shownStateName + " [Animation]";
				EditorGUILayout.HelpBox(infoText , MessageType.Info);

				EditorGUILayout.BeginHorizontal();
				if(GUILayout.Button(new GUIContent("Create Animator and animations"), GUILayout.Width(250), GUILayout.Height(35))) {
					CreateFolder();
				}
				EditorGUILayout.EndHorizontal();
			}
			else {
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.HelpBox("Assign a parent object to enable create button", MessageType.Info);
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndVertical();
		}

		public void CreateFolder() {
			// string guid = AssetDatabase.CreateFolder(parentFolder, animatorName);
			AssetDatabase.CreateFolder(parentFolder, animatorName);

			// Create the controller
			string locName = parentFolder + "/" + animatorName + "/" + animatorName + "Animator.controller";
			Debug.Log("locName=" + locName);

			var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(locName);

			// add parameter
			controller.AddParameter(shownParameterName, AnimatorControllerParameterType.Bool);

			// Add StateMachines
			var rootStateMachine = controller.layers[0].stateMachine;

			// Add States
			var stateI = rootStateMachine.AddState(initialStateName);
			var stateS = rootStateMachine.AddState(shownStateName);
			var stateH = rootStateMachine.AddState(hiddenStateName);

			// Add Transitions
			var initToShownTransition = stateI.AddTransition(stateS);
			initToShownTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 1f, shownParameterName);
			initToShownTransition.duration = 0.25f;

			var initToHiddenTransition = stateI.AddTransition(stateH);
			initToHiddenTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 1f, shownParameterName);
			initToHiddenTransition.duration = 0.25f;

			var showToHiddenTransition = stateS.AddTransition(stateH);
			showToHiddenTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 1f, shownParameterName);
			showToHiddenTransition.duration = 0.25f;

			var hiddenToShownTransition = stateH.AddTransition(stateS);
			hiddenToShownTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 1f, shownParameterName);
			hiddenToShownTransition.duration = 0.25f;

			string clipAssetIName = parentFolder + "/" + animatorName + "/" + animatorName + "_" + initialStateName + ".anim";
			var clipI = new AnimationClip();
			AssetDatabase.CreateAsset(clipI, clipAssetIName);
			var curveI = new AnimationCurve();
			curveI.AddKey(0f, 0f);
			clipI.SetCurve("", typeof(CanvasGroup), "m_Alpha", curveI);
			clipI.SetCurve("", typeof(CanvasGroup), "m_Interactable", curveI);
			clipI.SetCurve("", typeof(CanvasGroup), "m_BlocksRaycasts", curveI);

			string clipAssetHName = parentFolder + "/" + animatorName + "/" + animatorName + "_" + hiddenStateName + ".anim";
			var clipH = new AnimationClip();
			AssetDatabase.CreateAsset(clipH, clipAssetHName);
			clipH.SetCurve("", typeof(CanvasGroup), "m_Alpha", curveI);
			clipH.SetCurve("", typeof(CanvasGroup), "m_Interactable", curveI);
			clipH.SetCurve("", typeof(CanvasGroup), "m_BlocksRaycasts", curveI);

			string clipAssetSName = parentFolder + "/" + animatorName + "/" + animatorName + "_" + shownStateName + ".anim";
			var clipS = new AnimationClip();
			AssetDatabase.CreateAsset(clipS, clipAssetSName);
			var curveShown = new AnimationCurve();
			curveShown.AddKey(0f, 1f);
			clipS.SetCurve("", typeof(CanvasGroup), "m_Alpha", curveShown);
			clipS.SetCurve("", typeof(CanvasGroup), "m_Interactable", curveShown);
			clipS.SetCurve("", typeof(CanvasGroup), "m_BlocksRaycasts", curveShown);

			stateI.motion = clipI;
			stateH.motion = clipH;
			stateS.motion = clipS;

		}
	}

} // namespace

#endif // UNITY_EDITOR

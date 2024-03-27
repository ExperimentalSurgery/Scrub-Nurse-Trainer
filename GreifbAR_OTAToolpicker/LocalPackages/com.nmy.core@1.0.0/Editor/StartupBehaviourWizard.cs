using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.CodeDom.Compiler;
using System.IO;
using System;

namespace NMY.Editor {

	public class StartupBehaviourWizard : EditorWindow{
		string behaviourName;
		string behaviourNamespace = "NMY";
		UnityEngine.Object locationObject;
		Type behaviourType;
		string filePath;

		static Type[] behaviourTypes = new Type[]{typeof(StartupBehaviour), typeof(ActivatableStartupBehaviour), typeof(SimpleAnimatorActivatable),typeof(SingletonStartupBehaviour<>)};
		static string[] availableBehaviourTypes;
		int behaviourTypeIndex;

		readonly GUIContent behaviourNameLabel = new GUIContent("Behaviour Name", "This is the name that will represent the behaviour. Please choose a class name, matching C# naming conventions.");
		readonly GUIContent behaviourNamespaceLabel = new GUIContent("Namespace", "The behaviour's namespace.");
		readonly GUIContent folderLabel = new GUIContent("Location", "The behaviour's location in the project.");

		const string defaultNamespace = "NMY";
		const string tab = "\t";
		const float k_ScreenSizeWindowBuffer = 64;
		const float k_WindowWidth = 400;
		const float k_MaxWindowHeight = 300;

		[MenuItem("NMY/Startup/Startup Behaviour Wizard...")]
		static void CreateWindow(){
			GetWindow<StartupBehaviourWizard>(false, "StartupCreator", true);
			Init();
		}

		static void Init(){
			availableBehaviourTypes = new string[behaviourTypes.Length];
			for(int i=0; i < behaviourTypes.Length; i++){
				availableBehaviourTypes[i] = behaviourTypes[i].ToString();
			}
		}

		void OnGUI(){
			if(availableBehaviourTypes == null)
				Init();
			
			// Behaviour name
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			behaviourName = EditorGUILayout.TextField (behaviourNameLabel, behaviourName);

			bool isBehaviourNameNotEmpty = !string.IsNullOrEmpty(behaviourName);
			if (!isBehaviourNameNotEmpty){
				EditorGUILayout.HelpBox("The script needs a name which starts with a capital letter and contains no spaces or special characters.", MessageType.Error);
				return;
			}

			bool isBehaviourNameCorrectlyFormatted = false;
#if NET_4_6			
			isBehaviourNameCorrectlyFormatted = CodeGenerator.IsValidLanguageIndependentIdentifier(behaviourName);
			if (!isBehaviourNameCorrectlyFormatted){
				EditorGUILayout.HelpBox("The script needs a name which starts with a capital letter and contains no spaces or special characters.", MessageType.Error);
				return;
			}
#else
			// add a RegEx check for the classname here
			isBehaviourNameCorrectlyFormatted = true;				
#endif			

			EditorGUILayout.EndVertical();

			// Behaviour namespace
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			behaviourNamespace = EditorGUILayout.TextField(behaviourNamespaceLabel, behaviourNamespace);
			EditorGUILayout.EndVertical();

			// Path
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			locationObject = EditorGUILayout.ObjectField(folderLabel, locationObject, typeof(UnityEngine.Object), false);
			if(locationObject != null){
				filePath = AssetDatabase.GetAssetPath(locationObject);
				if(!System.IO.Directory.Exists(filePath) && System.IO.File.Exists(filePath))
					filePath = System.IO.Directory.GetParent(filePath).ToString();	// If object is a file instead of a directory, get its location
				EditorGUILayout.HelpBox("The script will be created at "+filePath, MessageType.Info);
			}
			else{
				filePath = "";
				EditorGUILayout.HelpBox("The script will be created at the root folder if not specified otherwise.", MessageType.Warning);
			}
			EditorGUILayout.EndVertical();

			// Activatable selection
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			behaviourTypeIndex = EditorGUILayout.Popup("Type", behaviourTypeIndex, availableBehaviourTypes);
			behaviourType = behaviourTypes[behaviourTypeIndex];
			EditorGUILayout.EndVertical();

			// Button
			if(isBehaviourNameNotEmpty && isBehaviourNameCorrectlyFormatted && !string.IsNullOrEmpty(behaviourNamespace)){
				EditorGUILayout.Space();
				EditorGUILayout.Space();

				if(GUILayout.Button("Create", GUILayout.Width(60))){
					// Create script on click
					CreateScript(StartupBehaviourContent());
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
				} 
			}
		}

		void CreateScript(string content){
			string assetPath;

			if(!string.IsNullOrEmpty(filePath)){
				string projectPath = Directory.GetParent(Application.dataPath).FullName;
				assetPath =  Path.Combine(projectPath, filePath) + "/" + behaviourName + ".cs";
			}
			else{
				assetPath = Application.dataPath + "/" + behaviourName + ".cs";
			}

			if(!File.Exists(assetPath)){
				using (StreamWriter writer = File.CreateText(assetPath))
					writer.Write(content);
				Debug.Log("Created "+behaviourName+" script at "+assetPath);
			}
			else{
				Debug.LogError("File already exists: "+assetPath);
			}
		}

		#region Script content
		string StartupBehaviourContent(){
			return
				"using System.Collections;\r\n" +
				"using System.Collections.Generic;\r\n" +
				"using UnityEngine;\r\n" +
				"\r\n" +
				"namespace "+GetNamespace()+"{\r\n" +
				tab+"public class "+behaviourName+": "+GetBase()+"{\r\n" +
				tab+tab+"protected override void StartupEnter(){\r\n" +
				tab+tab+tab+(DerivesFromAbstractBase() ? NotImplementedContent() : "base.StartupEnter();")+"\r\n" +
				tab+tab+"}\r\n" +
				GetTypeFunctionsContent() +
				tab+"}\r\n" +
				"}";
		}

		string ActivatableContent(){
			return
				tab+tab+"#region IActivatable\r\n" +
				tab+tab+"protected override void ActivateEnter(){\r\n" +
				tab+tab+tab+(DerivesFromAbstractBase() ? NotImplementedContent() : "base.ActivateEnter();")+"\r\n" +
				tab+tab+"}\r\n\r\n" +
				tab+tab+"protected override void ActivateImmediatelyEnter(){\r\n" +
				tab+tab+tab+(DerivesFromAbstractBase() ? NotImplementedContent() : "base.ActivateImmediatelyEnter();")+"\r\n" +
				tab+tab+"}\r\n\r\n" +
				tab+tab+"protected override void DeactivateEnter(){\r\n" +
				tab+tab+tab+(DerivesFromAbstractBase() ? NotImplementedContent() : "base.DeactivateEnter();")+"\r\n" +
				tab+tab+"}\r\n\r\n" +
				tab+tab+"protected override void DeactivateImmediatelyEnter(){\r\n" +
				tab+tab+tab+(DerivesFromAbstractBase() ? NotImplementedContent() : "base.DeactivateImmediatelyEnter();")+"\r\n" +
				tab+tab+"}\r\n" +
				tab+tab+"#endregion";
		}

		string GetTypeFunctionsContent(){
			string content = "";
			if(IsActivatable())
				content = "\r\n" + ActivatableContent() + "\r\n";
			return content;
		}

		string GetNamespace(){
			return string.IsNullOrEmpty(behaviourNamespace) ? defaultNamespace : behaviourNamespace;
		}

		string GetBase(){
			string baseName = "";
			if(behaviourType == typeof(StartupBehaviour))
				baseName = "StartupBehaviour";
			else if(behaviourType == typeof(ActivatableStartupBehaviour))
				baseName = "ActivatableStartupBehaviour";
			else if(behaviourType == typeof(SimpleAnimatorActivatable))
				baseName = "SimpleAnimatorActivatable";
			else if(behaviourType == typeof(SingletonStartupBehaviour<>))
				baseName = "SingletonStartupBehaviour<"+behaviourName+">";

			return baseName;
		}

		string NotImplementedContent(){
			return "// TODO";
//			return "throw new System.NotImplementedException();";
		}
		#endregion

		bool DerivesFromAbstractBase(){
			return behaviourType == typeof(StartupBehaviour) || behaviourType == typeof(ActivatableStartupBehaviour);
		}

		bool IsActivatable(){
			return behaviourType == typeof(ActivatableStartupBehaviour) || behaviourType == typeof(SimpleAnimatorActivatable);
		}
	}
}
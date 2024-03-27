using UnityEngine;
using UnityEditor;
using NMY;

[CustomEditor(typeof(StartupBehaviour), true)]
public class StartupBehaviourEditor : Editor
{
	private static readonly Color backgroundColorStarted = new Color(0.0f, 1.0f, 0.0f, 0.3f);
	private static readonly Color backgroundColorNStarted = new Color(0.5f, 0.0f, 0.0f, 0.3f);
	private static readonly Color backgroundColorActive = new Color(0.0f, 1.0f, 0.0f, 0.3f);
	private static readonly Color backgroundColorNActive = new Color(1.0f, 0.0f, 0f, 0.3f);
	private static int numberOfFields = 3; // ScriptRef, autoSTartup, startupInAwake
	private static int numberOfFieldsActivatable = 4; // ScriptRef, autoSTartup, startupInAwake, isInitiallyActivated
	private static GUIStyle s;
	private static GUIContent c;
	private static GUIStyleState gss;
	private static Texture2D icon;

	private void Init()
	{
		s = new GUIStyle(GUI.skin.button);
		s.border.left = 0;
		s.border.top = 0;
		s.border.right = 0;
		s.border.bottom = 0;
		gss = new GUIStyleState();
		icon = new Texture2D(16, 16);
		Graphics.ConvertTexture(EditorGUIUtility.IconContent("_Help").image, icon);
		gss.background = icon;
		s.normal = gss;

		c = EditorGUIUtility.IconContent("_Help");
		c.tooltip = "Reveal in Startup View";
	}

	public override void OnInspectorGUI()
	{
		if (EditorPrefs.GetBool("ShowHighlights"))
		{
			var rect = GUILayoutUtility.GetRect(1, 1);

			if(target is ActivatableStartupBehaviour activatable){
				// we have an ActivatableStartupBehaviour
				EditorGUI.DrawRect(new Rect(rect.x-15, rect.y, 
					EditorGUIUtility.currentViewWidth, 
					(EditorGUIUtility.singleLineHeight * numberOfFieldsActivatable) + (EditorGUIUtility.standardVerticalSpacing * (numberOfFieldsActivatable-1)) + 2.5f),
					activatable.hasStartedUp ? 
						(activatable.isActivated ? backgroundColorActive : backgroundColorNActive) :
						backgroundColorNStarted);
			}else{
				// we have a regular StartupBehaviour
				StartupBehaviour startup = (StartupBehaviour)target;
				EditorGUI.DrawRect(new Rect(rect.x-15, rect.y, 
					EditorGUIUtility.currentViewWidth, 
					(EditorGUIUtility.singleLineHeight * numberOfFields) + (EditorGUIUtility.standardVerticalSpacing * (numberOfFields-1)) + 2.5f),
					startup.hasStartedUp ? backgroundColorStarted : backgroundColorNStarted);
			}

			serializedObject.Update();
		}

		if (c == null || s == null || gss == null || gss.background == null)
		{
			Init();
		}

		if (GUI.Button(new Rect(55, 6, 16, 16),string.Empty, s))
		{
			StartupView.ShowWindow(target.GetInstanceID());
		}

		base.OnInspectorGUI();
	}
}
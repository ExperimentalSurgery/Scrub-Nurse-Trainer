using UnityEngine;
using UnityEditor;
using System.Collections;

namespace NMY.Editor {

/// <summary>
/// A custom Transform inspector which enables to copy-and-paste transform
/// properties (translation, rotation, scale) from one game object to
/// another.
/// </summary>
[CustomEditor(typeof(Transform))]
public class ExtTransformInspector : TransformInspector {

	// static private SerializedObject serializedObj;
	static private Vector3 trans;
	static private Quaternion rot;
	static private Vector3 scale;

	public ExtTransformInspector () : base() {}

	public override void OnInspectorGUI ()
	{
		Transform t = target as Transform;

		base.OnInspectorGUI();

		GUILayout.Space(5);
		GUILayout.BeginHorizontal();
		// EditorGUIUtility.LookLikeControls();
		EditorGUIUtility.labelWidth = 25;
		EditorGUIUtility.fieldWidth = 50; 

		if (GUILayout.Button("Copy TRS", EditorStyles.miniButton, GUILayout.Width(90))) {
			trans = t.transform.localPosition;
			rot = t.transform.localRotation;
			scale = t.transform.localScale;
		}
		GUILayout.FlexibleSpace();
		GUILayout.ExpandWidth(false);
		GUILayout.Label("Paste:", EditorStyles.miniLabel, GUILayout.Width(40));

		if (GUILayout.Button("T", EditorStyles.miniButtonLeft, GUILayout.Width(40))) {
			Undo.RecordObject(t, "Translation Change");
			t.localPosition = trans;
		}
		if (GUILayout.Button("R", EditorStyles.miniButtonMid, GUILayout.Width(40))) {
			Undo.RecordObject(t, "Rotation Change");
			t.localEulerAngles = rot.eulerAngles;
		}
		if (GUILayout.Button("S", EditorStyles.miniButtonRight, GUILayout.Width(40))) {
			Undo.RecordObject(t, "Scale Change");
			t.localScale = scale;			
		}
		if (GUILayout.Button("TRS", EditorStyles.miniButton, GUILayout.Width(50))) {
			Undo.RecordObject(t, "Transform Change");
			t.localPosition = trans;
			t.localEulerAngles = rot.eulerAngles;
			t.localScale = scale;
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Copy WorldTR", EditorStyles.miniButton, GUILayout.Width(90))) {
			trans = t.transform.position;
			rot = t.transform.rotation;
			scale = t.transform.lossyScale;
		}
		GUILayout.FlexibleSpace();				
		// GUILayout.ExpandWidth(false);
		GUILayout.Label("Paste:", EditorStyles.miniLabel, GUILayout.Width(40));

		if (GUILayout.Button("WT", EditorStyles.miniButtonLeft, GUILayout.Width(40))) {
			Undo.RecordObject(t, "Translation Change");
			t.position = trans;
		}
		if (GUILayout.Button("WR", EditorStyles.miniButtonMid, GUILayout.Width(40))) {
			Undo.RecordObject(t, "Rotation Change");
			t.eulerAngles = rot.eulerAngles;
		}
		GUILayout.Space(40);
		if (GUILayout.Button("WTR", EditorStyles.miniButton, GUILayout.Width(50))) {
			Undo.RecordObject(t, "Transform Change");
			t.position = trans;
			t.eulerAngles = rot.eulerAngles;
		}

		GUILayout.EndHorizontal();

		GUILayout.Space(5);
	}

}

}
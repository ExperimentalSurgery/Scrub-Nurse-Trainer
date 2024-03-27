// Reverse engineered UnityEditor.TransformInspector

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace NMY.Editor {

//[CustomEditor(typeof(Transform))]
public class TransformInspector : UnityEditor.Editor {
   
    // private bool firstSet;
    //private Vector3 rotation;
    private Quaternion oldQuaternion;
   
   
    public TransformInspector ()
    {
        // this.firstSet = true;
    }
   
   
    public override void OnInspectorGUI ()
    {
        float origLabelWidth = EditorGUIUtility.labelWidth;
        float origFieldWidth = EditorGUIUtility.fieldWidth;
        EditorGUIUtility.labelWidth = 70;
        // EditorGUIUtility.fieldWidth = 25;
        Transform t = (Transform)this.target;
        EditorGUI.indentLevel = 0;
		/*
        //if (this.firstSet)
        {
            if (this.oldQuaternion != t.localRotation)
            {
                this.firstSet = false;
                this.rotation = t.localEulerAngles;
                this.oldQuaternion = t.localRotation;
            }
        //}
        */
        Vector3 position = EditorGUILayout.Vector3Field("Position", t.localPosition);
        Quaternion rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", t.localRotation.eulerAngles));
        Vector3 scale = EditorGUILayout.Vector3Field("Scale", t.localScale);
        if (GUI.changed)
        {
			Undo.RecordObject(t, "Transform Change");
            //this.rotation = this.FixIfNaN(this.rotation);
            t.localPosition = this.FixIfNaN(position);
            t.localEulerAngles = rotation.eulerAngles;
            //this.oldQuaternion = t.localRotation;
            t.localScale = this.FixIfNaN(scale);
        }
        
        EditorGUIUtility.labelWidth = origLabelWidth;
        EditorGUIUtility.fieldWidth = origFieldWidth;
    }
   
    protected Vector3 FixIfNaN (Vector3 v)
    {
        if (float.IsNaN(v.x))
        {
            v.x = 0;
        }
        if (float.IsNaN(v.y))
        {
            v.y = 0;
        }
        if (float.IsNaN(v.z))
        {
            v.z = 0;
        }
        return v;
    }
   
}

}
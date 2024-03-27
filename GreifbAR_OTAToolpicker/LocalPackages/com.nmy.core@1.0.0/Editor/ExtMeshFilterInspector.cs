using UnityEngine;
using UnityEditor;
using System.Collections;

namespace NMY.Editor {

[CustomEditor(typeof(MeshFilter))]
public class ExtMeshFilterInspector : UnityEditor.Editor {
   
	private int totalMeshCount = 0;
	private int totalVertexCount = 0;
	private int totalTriCount = 0;
	private int renderQueue;
	
    public ExtMeshFilterInspector () : base() {}

    public override void OnInspectorGUI ()
    {
		base.DrawDefaultInspector();
 
		totalMeshCount = 0;
		totalVertexCount = 0;
		totalTriCount = 0;
		
		GameObject selectedObj = Selection.activeGameObject;
		MeshFilter[] childMeshFilter = selectedObj.GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter mf in childMeshFilter) {
			if (mf.sharedMesh) {
				totalVertexCount += mf.sharedMesh.vertexCount;
				totalTriCount += mf.sharedMesh.triangles.Length / 3;
				totalMeshCount++;
			}
		}	
		MeshRenderer meshRenderer = selectedObj.GetComponent<MeshRenderer>();
		if (meshRenderer && meshRenderer.sharedMaterial)
			renderQueue = meshRenderer.sharedMaterial.renderQueue;
		
		DrawTinyLayout();
    }
	
	protected void DrawTinyLayout() {
		//string objectName = Selection.activeGameObject==null ? "None" : Selection.activeGameObject.name;
		EditorGUILayout.Separator();
		GUILayout.BeginHorizontal();
		GUILayout.Space(14);
		GUILayout.Label ("Mesh statistics", EditorStyles.boldLabel);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		string vpm = totalMeshCount==0 ? "" :  (totalVertexCount / totalMeshCount).ToString();
		GUILayout.Space(14);
		GUILayout.Label("Meshes: " + totalMeshCount);
		GUILayout.Label(" |  Verts: " + totalVertexCount);
		GUILayout.Label(" |  Tris: " + totalTriCount);
		GUILayout.Label(" |  Verts/Mesh: " + vpm);
		GUILayout.Label(" |  RQ: " + renderQueue);
		GUILayout.Space(8);
		GUILayout.EndHorizontal();		
	}	
}

}
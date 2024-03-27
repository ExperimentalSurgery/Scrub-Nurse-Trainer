using UnityEngine;
using UnityEditor;

namespace NMY.TouchInteraction3D.Editor {

    [InitializeOnLoad]
    public class CheckForTouchableLayers {

        static public string touchableObjectLayer = "touchableObjects"; // this is our primary layer for touchable objects
 	    static public string controlObjectLayer = "controlObjects"; // this layer has priority over the standard layer. objects in here will be hit/checked first, and only if nothing is hit, the primary layer will be checked

        static CheckForTouchableLayers()
        {
            CheckForTouchableAndControlLayers();
        }

        static protected void CheckForTouchableAndControlLayers() {
            int touchableLayerIndex = LayerMask.NameToLayer(touchableObjectLayer);
            int controlLayerIndex = LayerMask.NameToLayer(controlObjectLayer);
            if (touchableLayerIndex==-1) {
                Debug.LogWarning("Layer " + touchableObjectLayer + " required by the TouchManager is missing. Automatically adding it at index 12.");
                AddLayer(touchableObjectLayer, 12);
            } 
            if (controlLayerIndex==-1) {
                Debug.LogWarning("Layer " + controlObjectLayer + " required by the TouchManager is missing. Automatically adding it at index 13.");
                AddLayer(controlObjectLayer, 13);
            }            
        }

        // Code is based on: https://forum.unity.com/threads/adding-layer-by-script.41970/
        static public void AddLayer(string layerName, int index) {
            SerializedObject manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = manager.FindProperty("layers");
            SerializedProperty slot = null;
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(index);
            if (sp != null && string.IsNullOrEmpty(sp.stringValue)) {
                slot = sp;
            }

            if (slot != null) {
                slot.stringValue = layerName;
                Debug.Log("Added layer " + layerName +  " at index " + index);
            }
            else {
                Debug.LogError("Layer Slot at index " + index + " already set. Cannot add layer " + layerName);
            }

            // save
            manager.ApplyModifiedProperties();
        }

    } // class

} // namespace
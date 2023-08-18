using UnityEditor;
using UnityEngine;

namespace NMY.GoogleCloudTextToSpeech
{
    [CustomEditor(typeof(GoogleTextToSpeech))]
    public class GoogleTextToSpeechEditor : Editor
    {
        public override async void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            
            var t = (GoogleTextToSpeech)target;

            if (GUILayout.Button("Synthesize Text To Speech"))
            {
                await t.Start();
            }
            else if (GUILayout.Button("Clear Asset Table Metadata"))
            {
                t.ClearAssetTableMetadata();
            }
        }
    }
}
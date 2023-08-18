using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InformationSpot : MonoBehaviour
{
    [SerializeField] private InstrumentData data;
    [SerializeField] private TextMeshPro titleTextMesh;
    private void OnEnable()
    {
        if (data)
        {
            if(data.title) titleTextMesh.text = data.title.text;
        }
    }
}

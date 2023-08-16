using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using Varjo.XR;

public class MarkerVisualizer : MonoBehaviour
{
    public TextMesh idText;
    public bool isDynamic;    
    public string[] sceneObjectNames;

    private ParentConstraint childTransform;

    public void SetMarkerData(VarjoMarker marker)
    {
        if(isDynamic)
            VarjoMarkers.AddVarjoMarkerFlags(marker.id, VarjoMarkerFlags.DoPrediction);
        transform.localPosition = marker.pose.position;
        transform.localRotation = marker.pose.rotation;
        //transform.localScale = new Vector3(marker.size.x, marker.size.x, marker.size.z);
        idText.text = marker.id.ToString();

        if(sceneObjectNames.Length == 0)
            return;

        if(childTransform != null && childTransform.sourceCount != 0)
            return;

        ParentConstraint[] pcList = FindObjectsOfType<ParentConstraint>(true);
        for (int i = 0; i < pcList.Length; i++)
        {
            if (sceneObjectNames.Contains(pcList[i].gameObject.name))
            {
                childTransform = pcList[i];
                ConstraintSource scr = new ConstraintSource();
                scr.sourceTransform = this.transform;
                scr.weight = 1f;
                if(childTransform.sourceCount > 0)
                    childTransform.SetSource(0, scr);
                else
                    childTransform.AddSource(scr);
                childTransform.constraintActive = true;
                childTransform.SetRotationOffset(0, new Vector3(0,180,0));

            }
        }        
    }
}

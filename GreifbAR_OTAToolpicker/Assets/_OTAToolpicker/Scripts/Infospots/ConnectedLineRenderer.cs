using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectedLineRenderer : MonoBehaviour

{
    [Header("NMY")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform endTransform;


    void Update(){

            lineRenderer.SetPosition(0,startTransform.position);
            lineRenderer.SetPosition(1,endTransform.position);

    }

}

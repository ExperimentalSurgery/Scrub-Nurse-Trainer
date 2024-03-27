using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.Localization;
using NMY.CameraTools;
using UnityEngine.Assertions;

public class InformationSpot : MonoBehaviour
{
    // [SerializeField] private InstrumentDataLegacy data;
    [SerializeField] private LocalizedString title;

    [Header("CloneTransAndLookAt settings")]
    [SerializeField] private Transform sourceTransformCTLA;
    [Header("CloneTransAndLookAt internals")]
    [SerializeField] private CloneTransAndLookAt lineStartCTLA;
    [SerializeField] private CloneTransAndLookAt lineEndCTLA;

    [Header("UI elements")]
    [SerializeField] private TextMeshPro titleTextMesh;
    // [SerializeField] private TextMeshPro descriptionMesh;


    void Awake()
    {
        if (sourceTransformCTLA)
        {
            lineStartCTLA.sourceTrans = sourceTransformCTLA;
            lineEndCTLA.sourceTrans = sourceTransformCTLA;
        }

        // Assert.IsTrue(transform.localPosition.y==0f, "InformationSpot should be at y=0");
        // Assert.IsTrue(transform.localPosition.z==0f, "InformationSpot should be at z=0");

        // check that the first child's transform local position is zero
        // the first child should be the GO with the renderer or the parent of the renderers
        Assert.IsTrue(transform.GetChild(0).localPosition == Vector3.zero, "The first child of InformationSpot should be at local position zero");
    }

    private void OnEnable()
    {
        if(title!=null && !title.IsEmpty) titleTextMesh.text = title.GetLocalizedString();
    }
}

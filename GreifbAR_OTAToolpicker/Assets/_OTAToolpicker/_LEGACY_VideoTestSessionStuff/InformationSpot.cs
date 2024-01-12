using TMPro;
using UnityEngine;

public class InformationSpot : MonoBehaviour
{
    [SerializeField] private InstrumentData data;
    [SerializeField] private TextMeshPro titleTextMesh;
    [SerializeField] private TextMeshPro descriptionMesh;
    
    private void OnEnable(){
        if (data){
            if(!data.title.IsEmpty) titleTextMesh.text = data.title.GetLocalizedString();
            if(!data.description.IsEmpty) descriptionMesh.text = data.description.GetLocalizedString();
        }
    }
}

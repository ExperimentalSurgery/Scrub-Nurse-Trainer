using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    public class IdentificationQuestionsSample : MonoBehaviour
    {
        [SerializeField] private List<IdentificationQuestion> questions;

        [SerializeField] private List<InstrumentData> instruments;

        void Start()
        {
            StartIdentification();   
        }

        public void StartIdentification()
        {
            foreach(var question in questions)
            {
                string instrumentStringList = $"Question: {question.Question.GetLocalizedString()}\n";
                                
                foreach(var instrument in instruments)
                {                    
                    bool isFulfilled = question.IsFulfilledBy(instrument);
                    string colorName = (isFulfilled ? "green" : "red");
                    instrumentStringList += $"Instrument: <color={colorName}>{instrument.Title.GetLocalizedString()}</color>\n";                                        
                }
                Debug.Log(instrumentStringList);
            }
        }
    }
}

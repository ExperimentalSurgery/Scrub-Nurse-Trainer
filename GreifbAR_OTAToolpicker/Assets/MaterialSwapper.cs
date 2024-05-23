using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    public class MaterialSwapper : MonoBehaviour
    {

        public Material zPassMaterial;
        public RectTransform uiTransform;
        public float directionThreshold;
        public bool debug;

        private MeshRenderer[] m_Renderer;

        private Material[] m_MaterialInstances;
        private Material[][] m_MaterialsArrays;

        private bool m_isLookingAtUI;
        private bool m_lastLookAt;


        private float m_lookingValue;
        private Vector3 m_camToInstrumentRay;
        private Vector3 m_camToUiRay;

        // Start is called before the first frame update
        void Start()
        {
            m_Renderer = transform.GetComponentsInChildren<MeshRenderer>(true);
            m_MaterialInstances = new Material[m_Renderer.Length];
            m_MaterialsArrays = new Material[m_Renderer.Length][];
            // create mat instances
            for (int i = 0; i < m_Renderer.Length; i++)
            {
                m_MaterialInstances[i] = new Material(m_Renderer[i].materials[0]);
                m_MaterialsArrays[i] = m_Renderer[i].materials;
                m_MaterialsArrays[i][0] = m_MaterialInstances[i];
                m_Renderer[i].materials = m_MaterialsArrays[i];
            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            m_camToInstrumentRay = (transform.position - Camera.main.transform.position).normalized;
            m_camToUiRay = (uiTransform.position - Camera.main.transform.position).normalized;

            m_lookingValue = Vector3.Dot(m_camToUiRay, m_camToInstrumentRay);
            m_isLookingAtUI = m_lookingValue > directionThreshold;

            if (debug)
            { 
                Debug.Log($"is looking at it value: {m_lookingValue}");
                Debug.DrawRay(Camera.main.transform.position, m_camToInstrumentRay, Color.red);
                Debug.DrawRay(Camera.main.transform.position, m_camToUiRay, Color.cyan);
            }

            if (m_lastLookAt != m_isLookingAtUI)
            {
                if (debug)
                    Debug.Log($"m_isLookingAtUI changed to: {m_isLookingAtUI}");

                if (m_isLookingAtUI)
                {
                    SwitchToZPass();
                }
                else
                {
                    SwitchToNormal();
                }
            }

            m_lastLookAt = m_isLookingAtUI;
        }

        void SwitchToZPass()
        {
            if (debug)
                Debug.Log($"SwitchToZPass");
            for (int i = 0; i < m_Renderer.Length; i++)
            {
                m_MaterialsArrays[i][0] = new Material(zPassMaterial);
                m_Renderer[i].materials = m_MaterialsArrays[i];
            }
        }

        void SwitchToNormal()
        {
            if (debug)
                Debug.Log($"SwitchToNormal");
            for (int i = 0; i < m_Renderer.Length; i++)
            {
                m_MaterialsArrays[i][0] = m_MaterialInstances[i];
                m_Renderer[i].materials = m_MaterialsArrays[i];
            }
        }
    }
}

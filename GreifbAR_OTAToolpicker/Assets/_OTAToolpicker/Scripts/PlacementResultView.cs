using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NMY.OTAToolpicker
{
    public class PlacementResultView : MonoBehaviour
    {
        [SerializeField] private bool isInitiallyActivated = false;
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject successIcon;
        [SerializeField] private GameObject failureIcon;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button failureButton;
        [SerializeField] private Button continueButton;

        void Awake()
        {
            if (isInitiallyActivated)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        void OnEnable()
        {
            failureButton.onClick.AddListener(() => {
                if (errorPanel.activeSelf)
                    errorPanel.SetActive(false);
                else
                    errorPanel.SetActive(true);
            });

            continueButton.onClick.AddListener(() => {
                errorPanel.SetActive(false);
            });
        }

        void OnDisable()
        {
            failureButton.onClick.RemoveAllListeners();
            continueButton.onClick.RemoveAllListeners();
        }

        public void SetResult(PlacementResult result)
        {
            if (!result.isIntersectingTableBorder && result.isPositionValid && result.isDirectionValid)
            {
                successIcon.SetActive(true);
                failureIcon.SetActive(false);
                errorPanel.SetActive(false);
            }
            else
            {
                successIcon.SetActive(false);
                failureIcon.SetActive(true);
                errorPanel.SetActive(false);
            }
            UpdateErrorText(result);
        }

        public void Show()
        {
            Debug.Log("PlacementResultView::Show");
            gameObject.SetActive(true);
            lineRenderer.enabled = true;
            canvas.enabled = true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateErrorText(PlacementResult result)
        {
            string errorString = "";

            if (!result.isPositionValid)
                errorString += "• Falsche Position<br>";
            if (!result.isDirectionValid)
                errorString += "• Falsche Ausrichtung<br>";
            if (result.isIntersectingTableBorder)
                errorString += "• Instrument steht über";

            errorText.text = errorString;
        }

    }
}

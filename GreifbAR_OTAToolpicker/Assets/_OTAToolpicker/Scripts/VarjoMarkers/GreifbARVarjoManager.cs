using System;
using System.Collections.Generic;
using NMY;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Management;
using Varjo.XR;

namespace DFKI.NMY
{
    
    /// <summary>
    /// Component calling some Varjo-Related lines which will cause a Exception when no Varjo System is running.
    /// This error is used as detection mechanism and firing a event as well.
    /// Furthermore it only activates specific Gameobjects (f.e MarkerManager, MixedReality,...) when necassary. 
    /// </summary>
    public class GreifbARVarjoManager : MonoBehaviour
    {
        
        public List<GameObject> enableWhenVarjoSystem = new List<GameObject>();
        public List<GameObject> enableWhenOtherSystem = new List<GameObject>();
     
        public static bool IsVarjoSystem { private set; get; }
        
        [Header("Events")]
        // Event fires true => Varjo Detected, false => No Varjo
        public UnityEvent<bool> VarjoSystemChanged = new UnityEvent<bool>();

        
        public void Initialize()
        {
            IsVarjoSystem = false;
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null) {
                
                try {
                    var loader = XRGeneralSettings.Instance.Manager.activeLoader as Varjo.XR.VarjoLoader;
                    VarjoCameraSubsystem cameraSubsystem = loader.cameraSubsystem as VarjoCameraSubsystem;
                    if (cameraSubsystem != null)
                    {
                        IsVarjoSystem = true;
                        UpdateGameObjectStates();
                        VarjoSystemChanged.Invoke(true);
                    }

                    
                }
                catch (NullReferenceException e) {
                    Debug.LogWarning("No Varjo detected. Disable Varjo related Components "+e.Message);
                    IsVarjoSystem = false;
                    UpdateGameObjectStates();
                    VarjoSystemChanged.Invoke(false);
                }
            }
        }


        public void DisableVarjo()
        {
            Debug.Log("Disable Varjo Components");
            foreach (GameObject v in enableWhenVarjoSystem) {
                v.SetActive(false);
            }
            foreach (GameObject v in enableWhenOtherSystem) {
                v.SetActive(true);
            }

        }

        public void EnableVarjo()
        {
            Debug.Log("Enable Varjo Components");
            foreach (GameObject v in enableWhenVarjoSystem) {
                v.SetActive(true);
            }
            foreach (GameObject v in enableWhenOtherSystem) {
                v.SetActive(false);
            }

        }
        

        public static bool IsVarjoSystemRunning()
        {
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null) {
                try {
                    var loader = XRGeneralSettings.Instance.Manager.activeLoader as Varjo.XR.VarjoLoader;
                    var cameraSubsystem = loader.cameraSubsystem as VarjoCameraSubsystem;
                    return true;
                }
                catch (NullReferenceException e) {
                    Debug.LogWarning("No Varjo detected. Disable Varjo related Components "+e.Message);
                    return false;
                }
            }

            return false;
        }

        public void UpdateGameObjectStates()
        {
            Debug.Log("IsVarjoSystem="+IsVarjoSystem);
            if (IsVarjoSystem)
            {
                EnableVarjo();
            }
            else
            {
                DisableVarjo();
            }

        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectWarningSystem : MonoBehaviour
{
    public GameObject cylinder;
    public GameObject arrow;

    // Visual Warning
    public void VisualWarning()
    {
        cylinder.SetActive(false);
        cylinder.SetActive(true);
        arrow.SetActive(false);
        CameraAndRecognizedPos.turnArrow = true;
        CameraAndRecognizedPos.turnAudio = false;
    }

    // Auditory Warning
    public void AuditoryWarning()
    {
        cylinder.SetActive(false);
        cylinder.SetActive(true);
        arrow.SetActive(false);
        CameraAndRecognizedPos.turnArrow = false;
        CameraAndRecognizedPos.turnAudio = true;
    }

    // Auditory Visual Warning
    public void AuditoryVisualWarning()
    {
        cylinder.SetActive(false);
        cylinder.SetActive(true);
        arrow.SetActive(false);
        CameraAndRecognizedPos.turnArrow = true;
        CameraAndRecognizedPos.turnAudio = true;
    }
}

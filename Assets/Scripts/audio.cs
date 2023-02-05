using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audio : MonoBehaviour
{
    public AudioSource Beep;
    public GameObject arrow;
    private bool update;
    /*[SerializeField]
    public Transform camTransform;
    [SerializeField]*/
    public GameObject cylinder;
    public Vector3 dir;
    public Vector3 point;
    public Vector3 point2;
    public Vector3 angles;
    public void PlayBeep()
    {
        if (CameraAndRecognizedPos.turnAudio) { Beep.Play(); }
        if (CameraAndRecognizedPos.turnArrow)
        {
            arrow.SetActive(true);
        }
        if (cylinder.active)
        {
            update = true;
        }
    }
    private void Update()
    {
        if (update == true )
        {

            dir = cylinder.transform.position - Camera.main.transform.position;//Camera.main.transform.InverseTransformPoint(cylinder.transform.position);
            point = Camera.main.transform.forward * dir.magnitude;
            Vector3 dir2 = point - cylinder.transform.position;

            Quaternion rotation = Quaternion.LookRotation(dir2);
            arrow.transform.rotation = rotation;
        }
        else if (!CameraAndRecognizedPos.turnArrow) {
            arrow.SetActive(false);
        }
        /*dir = cylinder.transform.position - Camera.main.transform.position;//Camera.main.transform.InverseTransformPoint(cylinder.transform.position);
        point = Camera.main.transform.forward * dir.magnitude;
        Vector3 targetPosition = new Vector3(cylinder.transform.position.x, cylinder.transform.position.y, point.z );
        arrow.transform.LookAt(targetPosition);*/
        /*float x = rotation.eulerAngles.x;
        float y = rotation.eulerAngles.y;
        float z = rotation.eulerAngles.z;
        arrow.transform.localEulerAngles = new Vector3(x,y,z);*/
        

        /*Vector3 normdir = dir / dir.magnitude;
        point = Camera.main.transform.forward;
        point2=point - normdir;
            float a = Mathf.Acos(point2.x) * Mathf.Rad2Deg;//Mathf.Atan2(point2.x, point2.z) * Mathf.Rad2Deg;
            float b = Mathf.Acos(point2.y) * Mathf.Rad2Deg;
            float c = Mathf.Acos(point2.z) * Mathf.Rad2Deg;*/
        /*if (cylinder.transform.position.y >= point.y)
            a += 180;
        else
            a = a;*/
        /*a += 180;
        b += 180;
        c += 180;*/
        /*
        b += 90;
        c += 90;

        angles = new Vector3(c,b,a);
        arrow.transform.localEulerAngles = angles;*/

        //}
    }
    public void StopBeep()
    {
        update = false;
        Beep.Stop();
        arrow.SetActive(false);

    }
}

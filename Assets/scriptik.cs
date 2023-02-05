using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scriptik : MonoBehaviour
{
    private int i;
    private Transform cameraTransform;
    // Start is called before the first frame update
    void Start()
    {
        i = 0;
    }

    // Update is called once per frame
    void Update()
    {

        /*           cameraTransform = Camera.main.transform;
                   RaycastHit objHitInfo;

                   Vector3 left = new Vector3(cameraTransform.forward.x, cameraTransform.forward.y, cameraTransform.forward.z);
                   if (Physics.Raycast(cameraTransform.position, left, out objHitInfo, 30.0f, SpatialMapping.PhysicsRaycastMask))
                   {
                       Renderer rend = objHitInfo.transform.GetComponent<Renderer>();
                       GameObject g = objHitInfo.collider.gameObject;
                       if (rend)
                       {
                           // Change the material of all hit colliders
                           // to use a transparent shader.
                           rend.material.shader = Shader.Find("Transparent/Diffuse");
                           Color tempColor = rend.material.color;
                           tempColor.a = 0.3F;
                           rend.material.color = tempColor;

                       }
                   }

               i = i + 1;*/
                RaycastHit[] hits;
                hits = Physics.RaycastAll(transform.position, transform.forward, 100.0F);

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    Renderer rend = hit.transform.GetComponent<Renderer>();

                    if (rend)
                    {
                        // Change the material of all hit colliders
                        // to use a transparent shader.
                        rend.material.shader = Shader.Find("Transparent/Diffuse");
                        Color tempColor = rend.material.color;
                        tempColor.a = 0.3F;
                        rend.material.color = tempColor;
                    }
                }
    }
}

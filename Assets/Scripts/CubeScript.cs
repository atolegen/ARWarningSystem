using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Input;

public class CubeScript : MonoBehaviour
{
    public GameObject cube;
    private IMixedRealitySpatialAwarenessSystem spatialAwarenessSystem = null;
    private int time;
    private static int _meshPhysicsLayer = 0;
    private void Start()
    {
        time = 0;
    }
    // Update is called once per frame
    private void Update()
    {
        time = time + 1;
        if (time == 200)
        {
            cube.SetActive(true);
            cube.transform.position = GetPositionOnSpatialMap();
            //cube.GetComponent<MeshRenderer>().material.color= new Color(1.0f, 1.0f, 1.0f, 0.0f);
            //cube.GetComponent<MeshRenderer>().material.shader = Shader.Find("Transparent/Diffuse");
        }
    }

    private IMixedRealitySpatialAwarenessSystem SpatialAwarenessSystem
    {
        get
        {
            if (spatialAwarenessSystem == null)
            {
                MixedRealityServiceRegistry.TryGetService<IMixedRealitySpatialAwarenessSystem>(out spatialAwarenessSystem);
            }
            return spatialAwarenessSystem;
        }
    }
    private static int GetSpatialMeshMask()
    {
        IMixedRealitySpatialAwarenessSystem spatialAwarenessSystem = null;

        if (spatialAwarenessSystem == null)
        {

            MixedRealityServiceRegistry.TryGetService<IMixedRealitySpatialAwarenessSystem>(out spatialAwarenessSystem);
        }

        if (_meshPhysicsLayer == 0)
        {
            var spatialMappingConfig =
              spatialAwarenessSystem.ConfigurationProfile as 
                MixedRealitySpatialAwarenessSystemProfile;
            if (spatialMappingConfig != null)
            {
                foreach (var config in spatialMappingConfig.ObserverConfigurations)
                {
                    var observerProfile = config.ObserverProfile
                        as MixedRealitySpatialAwarenessMeshObserverProfile;
                    if (observerProfile != null)
                    {
                        _meshPhysicsLayer |= (1 << observerProfile.MeshPhysicsLayer);
                    }
                }
            }
        }

        return _meshPhysicsLayer;
    }

    public static Vector3 GetPositionOnSpatialMap(float maxDistance = 2)
    {
        Vector3 byDefault = new Vector3(0, 0, 0);
        RaycastHit hitInfo;
        Transform transform = Camera.main.transform;
        var headRay = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(headRay, out hitInfo, maxDistance, GetSpatialMeshMask()))
        {
            return hitInfo.point;
        }
        return byDefault;
    }

    public static Transform GetTransformOnSpatialMap(float maxDistance = 2)
    {
        //Vector3 byDefault = new Vector3(0, 0, 0);
        RaycastHit hitInfo;
        Transform transform = Camera.main.transform;
        var headRay = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(headRay, out hitInfo, maxDistance, GetSpatialMeshMask()))
        {
            return hitInfo.transform;
        }
        return null;
        //return byDefault;
    }

}

﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    /// <summary>
    /// This class is an example of the <see cref="SpatialAwareness.IMixedRealitySpatialAwarenessObservationHandler{T}"/> interface. It keeps track
    /// of the IDs of each mesh and tracks the number of updates they have received.
    /// </summary>
    public class DemoSpatialMeshHandler : MonoBehaviour, IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessMeshObject>
    {
        private IMixedRealitySpatialAwarenessSystem spatialAwarenessSystem = null;
        public GameObject cube;
        private static int _meshPhysicsLayer = 0;
        void Start()
        {
            cube.transform.position = GetPositionOnSpatialMap();
        }
        public static Vector3 GetPositionOnSpatialMap(float maxDistance = 2)
        {
            Vector3 byDefault = new Vector3(0, 0, 0);
            RaycastHit hitInfo;
            Transform transform = Camera.main.transform;
            var headRay = new Ray(transform.position, transform.forward);
            if (UnityEngine.Physics.Raycast(headRay, out hitInfo, maxDistance, GetSpatialMeshMask()))
        {
                return hitInfo.point;
            }
            return byDefault;
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
        /// <summary>
        /// Collection that tracks the IDs and count of updates for each active spatial awareness mesh.
        /// </summary>
        private Dictionary<int, uint> meshUpdateData = new Dictionary<int, uint>();

        private async void OnEnable()
        {
            await new WaitUntil(() => SpatialAwarenessSystem != null);
            SpatialAwarenessSystem.Register(gameObject);
        }

        private void OnDisable()
        {
            SpatialAwarenessSystem?.Unregister(gameObject);
        }

        /// <inheritdoc />
        public virtual void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
        {
            // A new mesh has been added.
            if (!meshUpdateData.ContainsKey(eventData.Id))
            {
                Debug.Log($"Tracking mesh {eventData.Id}");
                meshUpdateData.Add(eventData.Id, 0);
            }
        }

        /// <inheritdoc />
        public virtual void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
        {
            uint updateCount = 0;

            // A mesh has been updated. Find it and increment the update count.
            if (meshUpdateData.TryGetValue(eventData.Id, out updateCount))
            {
                // Set the new update count.
                meshUpdateData[eventData.Id] = ++updateCount;

                Debug.Log($"Mesh {eventData.Id} has been updated {updateCount} times.");
            }
        }

        /// <inheritdoc />
        public virtual void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
        {
            // A mesh has been removed. We no longer need to track the count of updates.
            if (meshUpdateData.ContainsKey(eventData.Id))
            {
                Debug.Log($"No longer tracking mesh {eventData.Id}.");
                meshUpdateData.Remove(eventData.Id);
            }
        }
    }
}

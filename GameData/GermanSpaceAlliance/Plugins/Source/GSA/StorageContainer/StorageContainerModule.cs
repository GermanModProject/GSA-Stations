using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GSA.Durability
{
    public class StorageContainerModule : PartModule
    {
        /// <summary>
        /// Partent from Doors
        /// </summary>
        [KSPField(isPersistant = false)]
        public string doorParentGameObjectName = "Doors";

        /// <summary>
        /// Door Name (Door000)
        /// </summary>
        [KSPField(isPersistant = false)]
        public string doorGameObjectName = "Door";

        /// <summary>
        /// Animationsname of Doors
        /// </summary>
        [KSPField(isPersistant = false)]
        public string doorAnimationName = null;

        /// <summary>
        /// Show Doors in VAV
        /// </summary>
        [KSPField(isPersistant = false)]
        public bool showDoorsInEditor = true;

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor)
            {
                setDoors();
            }
        }

        private GameObject _animator;

        private void setDoors()
        {            
            foreach (Transform model in transform)
            {
                foreach (Transform child in model)
                {
                    Debug.Log("StorageContainerModule: GameObject: " + child.gameObject.name);
                    if (child.name == doorParentGameObjectName)
                    {
                        Debug.Log("StorageContainerModule: Parent GameObject: " + child.gameObject.name);
                        if (doorAnimationName != null)
                        {
                            Debug.Log("StorageContainerModule: add Event Animation");
                            part.OnEditorAttach += OnEditorAttach;
                        }

                        Debug.Log("StorageContainerModule: Begin Doors");
                        foreach (Transform doors in child)
                        {
                            string pattern = doorGameObjectName + "([0-9]{3})";
                            string pattern2 = doorGameObjectName + "_([0-9]{3})";
                            string pattern3 = doorGameObjectName + ".([0-9]{3})";
                            if (doors.name == doorGameObjectName || Regex.IsMatch(doors.name, pattern) || Regex.IsMatch(doors.name, pattern2) || Regex.IsMatch(doors.name, pattern3))
                            {
                                Debug.Log("StorageContainerModule: set _Opacity");
                                //if (renderer.material.HasProperty("_Opacity"))
                                //{
                                    //renderer.material.SetFloat("_Opacity", 0.2F);
                                //}
                                Debug.Log("StorageContainerModule: remove Collider");
                                //doors.collider.enabled = false;                        
                                Destroy(doors.GetComponent(typeof(MeshCollider)));
                                if (!showDoorsInEditor)
                                {
                                    Destroy(doors.gameObject);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnEditorAttach()
        {
            Debug.Log("StorageContainerModule: try start Animation");
            try
            {
                //NOT WORK
                _animator.animation.Play(doorAnimationName);
            }
            catch { }
        } 
    }
}
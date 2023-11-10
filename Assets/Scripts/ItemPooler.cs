using Argus.ItemSystem;
using Argus.ItemSystem.Editor;
using Catacombs.Base;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.ElementSystem.Runtime
{
    [DefaultExecutionOrder(-1)]
    public class ItemPooler : UdonSharpBehaviour
    {
        [SerializeField] private ElementTypeManager elementTypeManager;

        [Header("Settings")]
        [SerializeField] private bool hideDebugs;
        [SerializeField] private Vector3 deadPosition;

        [Header("Object Arrays")]
        [SerializeField] private ElementInteractionHandler[] baseElements;
        
        [SerializeField] private ElementInteractionHandler[] elementSpawners;

        [SerializeField] private ElementInteractionHandler[] growableLinks;

        [SerializeField] private ElementPrecipitate[] elementPrecipitates;

        void Start()
        {
            for (int i = 0; i < baseElements.Length; i++)
            {
                baseElements[i].transform.position = deadPosition;
                baseElements[i].childElement.enabled = false;
                baseElements[i].enabled = false;

                baseElements[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < elementPrecipitates.Length; i++)
            {
                elementPrecipitates[i].transform.position = deadPosition;
                elementPrecipitates[i].trailRend.emitting = false;
                elementPrecipitates[i].enabled = false;

                elementPrecipitates[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < elementSpawners.Length; i++)
            {
                elementSpawners[i].transform.position = deadPosition;
                elementSpawners[i].childElement.enabled = false;
                elementSpawners[i].enabled = false;

                elementSpawners[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < growableLinks.Length; i++)
            {
                growableLinks[i].transform.position = deadPosition;
                growableLinks[i].childElement.enabled = false;
                growableLinks[i].enabled = false;

                growableLinks[i].gameObject.SetActive(false);
            }
        }

        public bool ElementSpawnerAvailable()
        {
            for (int i = 0; i < elementSpawners.Length; i++)
            {
                if (!elementSpawners[i].gameObject.activeSelf) return true;
            }

            return false;
        }

        public bool GrowableLinkAvailable()
        {
            for (int i = 0; i < growableLinks.Length; i++)
            {
                if (!growableLinks[i].gameObject.activeSelf) return true;
            }

            return false;
        }

        public BaseElement RequestBaseElement()
        {
            for (int i = 0; i < baseElements.Length; i++)
            {
                if (!baseElements[i].gameObject.activeSelf)
                {
                    baseElements[i].gameObject.SetActive(true);
                    baseElements[i].enabled = true;
                    baseElements[i].childElement.enabled = true;

                    return (BaseElement)baseElements[i].childElement;
                }
            }

            LogWarning("Not enough BaseElements to add another!");
            return null;
        }

        public ElementPrecipitate RequestElementPrecipitate()
        {
            for (int i = 0; i < elementPrecipitates.Length; i++)
            {
                if (!elementPrecipitates[i].gameObject.activeSelf)
                {
                    elementPrecipitates[i].gameObject.SetActive(true);
                    elementPrecipitates[i].enabled = true;

                    elementPrecipitates[i].elementInitialized = true;

                    return elementPrecipitates[i];
                }
            }

            LogWarning("Not enough ElementPrecipitates to add another!");
            return null;
        }

        public GrownObject RequestGrownObject(GrownObjectType grownObjectType)
        {
            switch (grownObjectType)
            {
                default:
                    LogWarning("Attempted to pull grownObject with grownObjectType 0, how did you get here?");
                    return null;

                case GrownObjectType.ElementSpawner:

                    for (int i = 0; i < elementSpawners.Length; i++)
                    {
                        if (!elementSpawners[i].gameObject.activeSelf)
                        {
                            elementSpawners[i].gameObject.SetActive(true);
                            elementSpawners[i].enabled = true;
                            elementSpawners[i].childElement.enabled = true;

                            elementSpawners[i].childElement.elementInitialized = true;

                            return (ElementSpawner)elementSpawners[i].childElement;
                        }
                    }

                    LogWarning("Not enough GrownObjects to add another!");
                    return null;

                case GrownObjectType.GrowableLink:

                    for (int i = 0; i < growableLinks.Length; i++)
                    {
                        if (!growableLinks[i].gameObject.activeSelf)
                        {
                            growableLinks[i].gameObject.SetActive(true);
                            growableLinks[i].enabled = true;
                            growableLinks[i].childElement.enabled = true;

                            growableLinks[i].childElement.elementInitialized = true;

                            return (GrowableLink)growableLinks[i].childElement;
                        }
                    }

                    LogWarning("Not enough GrownObjects to add another!");
                    return null;
            }
        }

        public ElementSpawner RequestElementSpawner()
        {
            for (int i = 0; i < elementSpawners.Length; i++)
            {
                if (!elementSpawners[i].gameObject.activeSelf)
                {
                    elementSpawners[i].gameObject.SetActive(true);
                    elementSpawners[i].enabled = true;
                    elementSpawners[i].childElement.enabled = true;

                    elementSpawners[i].childElement.elementInitialized = true;

                    return (ElementSpawner)elementSpawners[i].childElement;
                }
            }

            LogWarning("Not enough ElementSpawners to add another!");
            return null;
        }

        public GrowableLink RequestGrowableLink()
        {
            for (int i = 0; i < growableLinks.Length; i++)
            {
                if (!growableLinks[i].gameObject.activeSelf)
                {
                    growableLinks[i].gameObject.SetActive(true);
                    growableLinks[i].enabled = true;
                    growableLinks[i].childElement.enabled = true;

                    growableLinks[i].childElement.elementInitialized = true;

                    return (GrowableLink)growableLinks[i].childElement;
                }
            }

            LogWarning("Not enough GrowableLinks to add another!");
            return null;
        }

        public void ReturnBaseElement(GameObject baseElementObj)
        {
            ElementInteractionHandler objInteractionHandler = baseElementObj.GetComponent<ElementInteractionHandler>();
            BaseElement baseElement = (BaseElement)objInteractionHandler.childElement;

            baseElement.elementInitialized = false;

            //Reset transform
            baseElementObj.transform.localScale = baseElement.baseScale;
            baseElementObj.transform.position = deadPosition;
            baseElementObj.transform.parent = transform;
            baseElement.rb.isKinematic = true;

            //Reset script variables
            baseElement.elementTypeId = ElementTypes.None;
            baseElement.elementColor = Color.black;

            //Reset collision sensing
            baseElement.isCollidingSurface = false;
            baseElement.isGrounded = false;

            //clear out collision/seedpod/leaves gameobjects
            for (int i = 1; i < baseElementObj.transform.childCount; i++) Destroy(baseElementObj.transform.GetChild(i).gameObject);

            //Disable object & components
            objInteractionHandler.childElement.enabled = false;
            objInteractionHandler.enabled = false;
            baseElementObj.SetActive(false);

            Log($"BaseElement {baseElementObj.name} returned to Item Pooler");

            //Reset name
            baseElementObj.name = "PooledBaseElement";
        }

        //TODO: For some reason ElementPrecipitates (and perhaps all pooled elements) aren't settings isCollidingSurface = true the first time they collide with an object the VERY first time they are pulled from the Item Pooler. 

        public void ReturnElementPrecipitate(GameObject elementPrecipitateObj)
        {
            ElementPrecipitate elementPrecipitate = elementPrecipitateObj.GetComponent<ElementPrecipitate>();

            elementPrecipitate.elementInitialized = false;

            //Reset transform
            elementPrecipitateObj.transform.position = deadPosition;
            elementPrecipitateObj.transform.parent = transform;
            elementPrecipitate.rb.isKinematic = true;

            //Reset script variables 
            elementPrecipitate.elementTypeId = ElementTypes.None;
            elementPrecipitate.elementColor = Color.black;
            elementPrecipitate.elementRenderer.enabled = true;
            elementPrecipitate.trailRend.emitting = false;
            elementPrecipitate.physicsCollider.enabled = true;
            elementPrecipitate.parentContainer = null;
            elementPrecipitate.isContained = false;
            elementPrecipitate.elementPrecipitateType = 0;

            //Reset collision sensing
            elementPrecipitate.isCollidingSurface = false;
            elementPrecipitate.isGrounded = false;

            //Disable object & components
            elementPrecipitate.enabled = false;
            elementPrecipitateObj.SetActive(false);

            Log($"ElementPrecipitate {elementPrecipitateObj.name} returned to Item Pooler");

            //Reset name
            elementPrecipitateObj.name = "PooledElementPrecipitate";
        }

        public void ReturnGrownObject(GameObject grownObject)
        {
            ElementInteractionHandler objInteractionHandler = grownObject.GetComponent<ElementInteractionHandler>();
            GrownObject grownObjectScript = (GrownObject)objInteractionHandler.childElement;

            grownObjectScript.elementInitialized = false;

            //Reset transform
            grownObject.transform.position = deadPosition;
            grownObject.transform.parent = transform;
            grownObjectScript.rb.isKinematic = true;

            //Reset script variables 
            grownObjectScript.elementTypeId = ElementTypes.None;
            grownObjectScript.elementColor = Color.black;
            grownObjectScript.matured = false;

            //Reset collision sensing
            grownObjectScript.isCollidingSurface = false;
            grownObjectScript.isGrounded = false;

            //Clear out growth transforms/growth stage gameobjects
            for (int i = 0; i < grownObject.transform.childCount; i++) Destroy(grownObject.transform.GetChild(i).gameObject);

            //Disable object & components
            objInteractionHandler.childElement.enabled = false;
            objInteractionHandler.enabled = false;
            grownObject.SetActive(false);

            Log($"GrownElement {grownObject.name} returned to Item Pooler");

            //Reset name
            grownObject.name = "PooledGrownElement";
        }

        private void Log(string message)
        {
            if (!hideDebugs) Debug.Log($"[{name}] {message}", this);
        }

        private void LogWarning(string message)
        {
            if (!hideDebugs) Debug.LogWarning($"[{name}] {message}", this);
        }
    }
}
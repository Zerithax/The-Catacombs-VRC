using Argus.ItemSystem;
using Argus.ItemSystem.Editor;
using Catacombs.Base;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.ElementSystem.Runtime
{
    public class ItemPooler : UdonSharpBehaviour
    {
        [Header("Singletons")]
        [SerializeField] private ElementTypeManager elementTypeManager;

        [Header("Object Arrays")]
        [SerializeField] private ElementInteractionHandler[] baseElements;
        
        [SerializeField] private ElementInteractionHandler[] elementSpawners;

        [SerializeField] private ElementPrecipitate[] elementPrecipitates;

        void Awake()
        {
            for (int i = 0; i < baseElements.Length; i++)
            {
                baseElements[i].childElement.enabled = false;
                baseElements[i].enabled = false;

                baseElements[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < elementSpawners.Length; i++)
            {
                elementSpawners[i].childElement.enabled = false;
                elementSpawners[i].enabled = false;

                elementSpawners[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < elementPrecipitates.Length; i++)
            {
                elementPrecipitates[i].trailRend.emitting = false;
                elementPrecipitates[i].enabled = false;

                elementPrecipitates[i].gameObject.SetActive(false);
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

            Debug.LogWarning("Not enough BaseElements to add another!");
            return null;
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

                    return (ElementSpawner)elementSpawners[i].childElement;
                }
            }

            Debug.LogWarning("Not enough ElementSpawners to add another!");
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

                    return elementPrecipitates[i];
                }
            }

            Debug.LogWarning("Not enough ElementPrecipitates to add another!");
            return null;
        }

        public void ReturnBaseElement(GameObject baseElementObj)
        {
            ElementInteractionHandler objInteractionHandler = baseElementObj.GetComponent<ElementInteractionHandler>();
            BaseElement baseElement = (BaseElement)objInteractionHandler.childElement;
            

            //Reset transform
            baseElementObj.transform.localScale = baseElement.baseScale;
            baseElementObj.transform.position = new Vector3(0, -5, 0);
            baseElementObj.transform.parent = transform;
            objInteractionHandler.childElement.rb.isKinematic = true;

            //Reset script variables
            baseElement.elementTypeId = ElementTypes.None;
            baseElement.elementColor = Color.black;

            //clear out collision/seedpod/leaves gameobjects
            for (int i = 1; i < baseElementObj.transform.childCount; i++) Destroy(baseElementObj.transform.GetChild(i).gameObject);

            //Disable object & components
            objInteractionHandler.childElement.enabled = false;
            objInteractionHandler.enabled = false;
            baseElementObj.SetActive(false);

            Debug.Log($"BaseElement {baseElementObj.name} returned to Item Pooler", this);

            //Reset name
            baseElementObj.name = "PooledBaseElement";
        }

        public void ReturnElementSpawner(GameObject elementSpawnerObj)
        {
            ElementInteractionHandler spawnerInteractionHandler = elementSpawnerObj.GetComponent<ElementInteractionHandler>();
            BaseElement baseElement = (BaseElement)spawnerInteractionHandler.childElement;

            //Reset transform
            elementSpawnerObj.transform.position = new Vector3(0, -5, 0);
            elementSpawnerObj.transform.parent = transform;

            //Reset script variables 
            baseElement.elementTypeId = ElementTypes.None;
            baseElement.elementColor = Color.black;

            //Clear out growth transforms/growth stage gameobjects
            for (int i = 0; i < elementSpawnerObj.transform.childCount; i++) Destroy(elementSpawnerObj.transform.GetChild(i).gameObject);

            //Disable object & components
            spawnerInteractionHandler.childElement.enabled = false;
            spawnerInteractionHandler.enabled = false;
            elementSpawnerObj.SetActive(false);

            Debug.Log($"ElementSpawner {elementSpawnerObj.name} returned to Item Pooler", this);

            //Reset name
            elementSpawnerObj.name = "PooledElementSpawner";
        }

        public void ReturnElementPrecipitate(GameObject elementPrecipitateObj)
        {
            ElementPrecipitate elementPrecipitate = elementPrecipitateObj.GetComponent<ElementPrecipitate>();

            //Reset transform
            elementPrecipitateObj.transform.position = new Vector3(0, -5, 0);
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

            //Disable object & components
            elementPrecipitate.enabled = false;
            elementPrecipitateObj.SetActive(false);

            Debug.Log($"ElementPrecipitate {elementPrecipitateObj.name} returned to Item Pooler", this);

            //Reset name
            elementPrecipitateObj.name = "PooledElementPrecipitate";
        }
    }
}
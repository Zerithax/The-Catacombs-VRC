using Argus.ItemSystem;
using Argus.ItemSystem.Editor;
using Catacombs.Base;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace Catacombs.ElementSystem.Runtime
{
    public class ElementDispenser : UdonSharpBehaviour
    {
        [Header("Element Settings")]
        [SerializeField] private ElementTypeManager elementTypeManager;
        [SerializeField] private ElementTypes elementToDispense;
        [SerializeField] private Transform[] spawnPositions;
        [SerializeField] private bool usePrecipitate;

        [Header("Spawn Settings")]
        [SerializeField] private bool requireInteract;
        [SerializeField] private int spawnCountdown;

        [Header("Storage Settings")]
        [SerializeField] private bool hasStorage;
        [SerializeField] private LocalPlayerTracker playerTracker;
        [SerializeField] private VRCPickup pickupScript;
        [SerializeField] private int maxElements;
        [SerializeField] private int curElements;

        [Header("Debug")]
        [SerializeField] private bool hideDebugs;

        private float timer;

        private void Start() { if (hasStorage && playerTracker == null) playerTracker = GameObject.Find("LocalPlayerTracker").GetComponent<LocalPlayerTracker>(); }

        private void Update()
        {
            if (!requireInteract)
            {
                timer += Time.deltaTime;

                if (timer > spawnCountdown)
                {
                    timer = 0;

                    AttemptSpawnElement();
                }
            }
        }

        private void AttemptSpawnElement()
        {
            if (hasStorage)
            {
                if (curElements <= 0)
                {
                    Debug.Log($"[{name}] No stored elements to dispense!");
                    return;
                }
            }

            RuntimeElement newElement;
            ElementData elementType = elementTypeManager.elementDataObjs[(int)elementToDispense];
            Vector3 spawnPos = spawnPositions[Random.Range(0, spawnPositions.Length)].position;

            if (usePrecipitate) newElement = Instantiate(elementType.ElementPrecipitatePrefab, spawnPos, Quaternion.identity, transform).GetComponent<ElementPrecipitate>();
            else newElement = Instantiate(elementType.BaseElementPrefab, spawnPos, Quaternion.identity, transform).GetComponent<BaseElement>();

            if (hideDebugs) newElement.hideDebugsOverride = true;
            newElement.elementTypeManager = elementTypeManager;
            newElement.elementTypeId = elementToDispense;

        }

        public override void OnPickup()
        {
            base.OnPickup();

            if (requireInteract)
            {
                pickupScript.Drop();

                AttemptSpawnElement();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.gameObject == null) return;

            if (other.gameObject.layer == 24)
            {
                AttemptStoreItem(other);
            }
        }

        private void AttemptStoreItem(Collider other)
        {
            if (curElements < maxElements)
            {
                ElementInteractionHandler interactionHandler = null;
                RuntimeElement collidedElement;

                if (usePrecipitate) collidedElement = other.GetComponent<ElementPrecipitate>();
                else
                {
                    collidedElement = other.GetComponent<BaseElement>();
                    interactionHandler = other.GetComponent<ElementInteractionHandler>();
                }

                //Some Element scripts (not sure if all yet) will have a parent interaction handler script. If we get that instead, get its childElement.
                if (interactionHandler != null) collidedElement = interactionHandler.childElement;

                if (collidedElement != null)
                {
                    if (collidedElement.elementTypeId == elementToDispense)
                    {
                        Destroy(other.gameObject);
                        curElements++;
                    }
                }
            }
        }
    }
}
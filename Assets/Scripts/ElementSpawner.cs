using UnityEngine;
using Catacombs.Base;
using Argus.ItemSystem.Editor;

namespace Catacombs.ElementSystem.Runtime
{
    public class ElementSpawner : GrownObject
    {
        [Header("Element Spawning Details")]
        public int elementSpawnTime;
        private float elementGrowthTime;
        public Transform[] elementSpawnPoints;
        public BaseElement[] elementsSpawned;
        [SerializeField] private bool roomToSpawnElements = true;

        protected override void AdditionalStart() { elementsSpawned = new BaseElement[elementSpawnPoints.Length]; }

        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            ElementData elementData = elementTypeManager.elementDataObjs[(int)elementTypeId];

            elementSpawnTime = elementData.elementSpawnTime;

            parentObject.name = $"{parentObject.name} Spawner";

            Log($"Retrieved ElementSpawner data from {elementData.name}");
            return true;
        }

        protected override void AdditionalUpdate()
        {
            if (matured && isRooted && roomToSpawnElements) SpawnElements();
        }

        private void SpawnElements()
        {
            elementGrowthTime += Time.deltaTime;

            if (elementGrowthTime >= elementSpawnTime)
            {
                elementGrowthTime = 0;

                for (int i = 0; i < elementsSpawned.Length; i++)
                {
                    if (elementsSpawned[i] == null)
                    {
                        physicsCollider.enabled = false;

                        elementsSpawned[i] = (BaseElement)Instantiate(elementTypeManager.elementDataObjs[(int)elementTypeId].BaseElementPrefab, elementSpawnPoints[i].transform.position, Quaternion.identity, transform).GetComponent<ElementInteractionHandler>().childElement;
                        elementsSpawned[i].elementTypeManager = elementTypeManager;
                        elementsSpawned[i].elementTypeId = elementTypeId;
                        elementsSpawned[i].parentElementSpawner = this;

                        for (int j = 0; j < elementsSpawned.Length; j++)
                        {
                            if (elementsSpawned[j] != null) break;
                            roomToSpawnElements = false;
                        }

                        return;
                    }
                }
            }
        }

        public void DetachElement(BaseElement element)
        {
            bool spawnerEmpty = true;
            for (int i = 0; i < elementsSpawned.Length; i++)
            {
                if (elementsSpawned[i] == element) elementsSpawned[i] = null;
                else if (elementsSpawned[i] != null) spawnerEmpty = false;
            }

            if (spawnerEmpty) physicsCollider.enabled = true;

            roomToSpawnElements = true;
        }

        protected override void PlantedEvent()
        {
            //Enable child Elements' pickup colliders again
            for (int i = 0; i < elementsSpawned.Length; i++)
            {
                if (elementsSpawned[i] != null) elementsSpawned[i].physicsCollider.enabled = true;
            }
        }

        protected override void RemoveFromPlot()
        {
            base.RemoveFromPlot();

            //There's a 20% chance for Spawners to just die for uprooting them at all
            if (Random.Range(0, 5) == 0) Destroy(parentObject);

            //Plus an additional 30 second period before dying for staying uprooted
            //TODO: Maybe not hardcode uproot-death period?
            SendCustomEventDelayedSeconds(nameof(_DelayedKill), 30);
        }
    }
}

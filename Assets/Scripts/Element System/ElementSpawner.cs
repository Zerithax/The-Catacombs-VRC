using UnityEngine;
using Catacombs.Base;
using Argus.ItemSystem.Editor;

namespace Catacombs.ElementSystem.Runtime
{
    public class ElementSpawner : GrownObject
    {
        [Header("Element Spawning Details")]
        [SerializeField] private bool roomToSpawnElements = true;
        [SerializeField] private float elementSpawnTime;
        public BaseElement[] elementsSpawned;
        public Transform[] elementSpawnPoints;
        private float elementGrowthTime;

        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            Transform elementSpawnsObject = transform.GetChild(0);
            elementSpawnPoints = new Transform[elementSpawnsObject.childCount];
            elementsSpawned = new BaseElement[elementSpawnPoints.Length];
            for (int i = 0; i < elementSpawnPoints.Length; i++)
            {
                elementSpawnPoints[i] = elementSpawnsObject.GetChild(i);
            }

            elementSpawnTime = elementTypeData.grownObjectElement == 0 ? elementTypeData.baseElementSpawnTime : elementTypeManager.elementDataObjs[(int)elementTypeData.grownObjectElement].baseElementSpawnTime;

            parentObject.name = $"{parentObject.name} Spawner";

            Log($"Retrieved ElementSpawner data from {elementTypeData.name}");
            return true;
        }

        protected override void AdditionalUpdate()
        {
            base.AdditionalUpdate();

            if (matured && isRooted && roomToSpawnElements)
            {
                elementGrowthTime += Time.deltaTime;

                if (elementGrowthTime >= elementSpawnTime)
                {
                    elementGrowthTime = 0;
                    SpawnElement();
                }
            }
            else elementGrowthTime = 0;
        }

        private void SpawnElement()
        {
            for (int i = 0; i < elementsSpawned.Length; i++)
            {
                if (elementsSpawned[i] == null)
                {
                    BaseElement newElement = itemPooler.RequestBaseElement();

                    if (newElement != null)
                    {
                        physicsCollider.enabled = false;

                        elementsSpawned[i] = newElement;

                        if (elementTypeData.elementCanBecomeSeedPod) elementsSpawned[i].isSeedPod = Random.Range(0f, 100f) <= elementTypeData.seedPodSpawnChance;

                        //If the element spawned is a seed pod, override its ElementType if this element's seedPodElementType is specified (and fallback to elementTypeId) 
                        if (elementsSpawned[i].isSeedPod)
                        {
                            elementsSpawned[i].elementTypeId = elementTypeData.seedPodElementType == 0 ? elementTypeId : elementTypeData.seedPodElementType;
                        }
                        //Otherwise, override the new BaseElement's ElementType if this Element's grownObjectElement is specified (and fallback to elementTypeId)
                        else
                        {
                            elementsSpawned[i].elementTypeId = elementTypeData.grownObjectElement == 0 ? elementTypeId : elementTypeData.grownObjectElement;

                            //Not a seed pod, so spawn ElementLeavesPrefab if it exists
                            if (elementTypeData.ElementLeavesPrefab != null) Instantiate(elementTypeData.ElementLeavesPrefab, elementsSpawned[i].parentObject.transform);
                        }

                        elementsSpawned[i].parentElementSpawner = this;
                        elementsSpawned[i].disableContainment = true;

                        elementsSpawned[i]._PullElementType();

                        elementsSpawned[i].parentObject.transform.SetPositionAndRotation(elementSpawnPoints[i].transform.position, elementSpawnPoints[i].transform.rotation);
                        elementsSpawned[i].parentObject.transform.SetParent(transform, true);

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

        protected override void RemoveFromPlot()
        {
            base.RemoveFromPlot();

            //There's a 20% chance for Spawners to just die for uprooting them at all
            if (Random.Range(0, 5) == 0) itemPooler.ReturnGrownObject(parentObject);

            //Plus an additional 30 second period before dying for staying uprooted
            //TODO: Maybe not hardcode uproot-death period?
            SendCustomEventDelayedSeconds(nameof(_AttemptDespawn), 10);
        }
    }
}

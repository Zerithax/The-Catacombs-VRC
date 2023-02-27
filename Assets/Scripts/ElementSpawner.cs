using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Catacombs.Base;
using Argus.ItemSystem.Editor;

namespace Catacombs.ElementSystem.Runtime
{
    public class ElementSpawner : RuntimeElement
    {
        [Header("Element Details")]
        public GameObject ElementPrefabToSpawn;
        public int elementSpawnerGrowthPeriod;
        public int elementSpawnTime;

        [Header("Spawner Details")]
        public bool matured;
        public ElementSpawnerPlot parentSpawnPlot;
        public Animator spawnerAnim;
        public Transform[] elementSpawnPoints;
        [HideInInspector] public BaseElement[] elementsSpawned;


        private bool isRooted = true;
        [SerializeField] private float spawnerGrowthTime;
        private bool roomToSpawnElements = true;
        [SerializeField] private float elementGrowthTime;

        private ElementSpawnerPlot collidingSpawnerPlot;
        private float lastPlotInteractTime;
        private float lastPlotPlantTime;


        protected override void AdditionalStart()
        {
            if (spawnerAnim == null) spawnerAnim = GetComponent<Animator>();
            elementsSpawned = new BaseElement[elementSpawnPoints.Length];

            if (physicsCollider == null) physicsCollider = parentObject.GetComponent<Collider>();

            lastPlotPlantTime = Time.time;
            lastPlotInteractTime = lastPlotPlantTime;
        }

        public override void PullElementType()
        {
            Debug.Log($"[{parentObject.name}] {elementTypeManager.elementTypeData[(int)elementTypeId].name}");

            ElementData elementData = elementTypeManager.elementTypeData[(int)elementTypeId];

            elementSpawnerGrowthPeriod = elementData.elementSpawnerGrowthPeriod;
            ElementPrefabToSpawn = elementData.BaseElementPrefab;
            elementSpawnTime = elementData.elementSpawnTime;

            parentObject.name = Time.time < 1 ? $"{elementTypeId.ToString()} {parentObject.name}" : $"{elementTypeId.ToString()} {parentObject.name.Remove(parentObject.name.Length - 7)}";

            base.PullElementType();

            if (elementTypeId == 0)
            {
                Debug.Log($"[{name}] hasn't received its Element Data yet, retrying in 5 frames");
                SendCustomEventDelayedFrames(nameof(PullElementType), 5);
                return;
            }
        }

        protected override void AdditionalUpdate()
        {
            if (isRooted)
            {
                if (matured)
                {
                    if (roomToSpawnElements) SpawnElements();
                }
                else
                {
                    spawnerGrowthTime += Time.deltaTime;

                    if (spawnerGrowthTime >= elementSpawnerGrowthPeriod / 2 && spawnerGrowthTime < elementSpawnerGrowthPeriod)
                    {
                        //TODO: Make a set of animations that gradually scale three flowers up at the elementSpawnPoints
                        spawnerAnim.Play("GrowthStageTwo");
                    }
                    else if (spawnerGrowthTime >= elementSpawnerGrowthPeriod)
                    {
                        //TODO: Make a set of animations that gradually scale three flowers out and three leaves in at the elementSpawnPoints
                        spawnerAnim.Play("SpawnerCompleted");

                        matured = true;
                    }

                }
            }
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

                        elementsSpawned[i] = (BaseElement)Instantiate(ElementPrefabToSpawn, elementSpawnPoints[i].transform.position, Quaternion.identity, transform).GetComponent<ElementInteractionHandler>().childElement;
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

        public override void Grabbed()
        {
            base.Grabbed();

            RemoveFromPlot();
        }

        private void RemoveFromPlot()
        {
            parentSpawnPlot.RemoveSpawner(this);

            //20% chance to just kill the Spawner for uprooting it
            if (Random.Range(0, 5) == 0) Destroy(parentObject);

            isRooted = false;

            lastPlotInteractTime = Time.time;

            //Disable child elements' colliders until re-planted
            for (int i = 0; i < elementsSpawned.Length; i++)
            {
                if (elementsSpawned[i] != null) elementsSpawned[i].physicsCollider.enabled = false;
            }

            //TODO: Maybe not hardcode the amount of time before ElementSpawners die when uprooted?
            SendCustomEventDelayedSeconds(nameof(_DelayedKill), 30);
        }

        public override void _DelayedKill()
        {
            //Only follow through with the kill if ElementSpawner hasn't been placed back into an ElementSpawnerPlot yet and it's been less at least 30 seconds (DelayedKill time)
            if (!isRooted && lastPlotPlantTime != lastPlotInteractTime && lastPlotInteractTime >= lastPlotInteractTime + 30) Destroy(parentObject);
            else return;

            base._DelayedKill();
        }

        public override void Dropped()
        {
            if (collidingSpawnerPlot != null)
            {
                //The position the seed plot will lie is the Element's X/Z aligned with the SpawnerPlot's Y
                Vector3 seedPlotPos = new Vector3(parentObject.transform.position.x, collidingSpawnerPlot.transform.position.y, parentObject.transform.position.z);

                if (collidingSpawnerPlot.RoomToAddSpawner(seedPlotPos))
                {
                    //For tracking death when not re-planted in time
                    lastPlotPlantTime = Time.time;
                    lastPlotInteractTime = lastPlotPlantTime;

                    //Position ElementSpawner in Spawner Plot
                    parentObject.transform.position = seedPlotPos;
                    parentObject.transform.rotation = Quaternion.Euler(new Vector3(0, parentObject.transform.position.y, 0));

                    //Enable child Elements' pickup colliders again
                    for (int i = 0; i < elementsSpawned.Length; i++)
                    {
                        if (elementsSpawned[i] != null) elementsSpawned[i].physicsCollider.enabled = true;
                    }

                    collidingSpawnerPlot.AddSpawner(this);

                    rb.isKinematic = true;
                    isRooted = true;

                    return;
                }
            }

            rb.isKinematic = false;
        }

        protected override void AdditionalTriggerEnter(Collider other) { if (other.gameObject.layer == 2) collidingSpawnerPlot = other.GetComponent<ElementSpawnerPlot>(); }

        protected override void AdditionalTriggerExit(Collider other) { if (other.gameObject.layer == 2) collidingSpawnerPlot = null; }
    }
}

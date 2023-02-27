using Argus.ItemSystem;
using Argus.ItemSystem.Editor;
using Catacombs.Base;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.ElementSystem.Runtime
{
    public class ElementSpawnerPlot : UdonSharpBehaviour
    {
        [SerializeField] private ElementTypeManager elementTypeManager;
        [SerializeField] private int maxSpawners = 1;
        [SerializeField] private float minimumSpawnerDistance = 0.1f;
        [SerializeField] private Animator plotAnimator;
        public ElementSpawner[] elementSpawners;

        private void Start()
        {
            if (plotAnimator == null) plotAnimator = GetComponentInChildren<Animator>();

            if (elementSpawners.Length == 0) elementSpawners = new ElementSpawner[maxSpawners];
            //If the plot starts with existing spawners, start up their animations
            else
            {
                int i;
                for (i = 0; i < elementSpawners.Length; i++)
                {
                    if (elementSpawners[i] != null) elementSpawners[i].spawnerAnim.Play("GrowthStageOne");
                    else break;
                }

                //Disable Indicator if plot starts full
                if (i == elementSpawners.Length) plotAnimator.Play("SpawnPlotDie");
            }
        }

        public bool RoomToAddSpawner(Vector3 seedPodPos)
        {
            for (int i = 0; i < elementSpawners.Length; i++)
            {
                if (elementSpawners[i] == null)
                {
                    //Fail if Seed Pod has been placed too close to an existing elementSpawner
                    for (int j = 0; j < i; j++)
                    {
                        if (Vector3.Distance(seedPodPos, elementSpawners[j].transform.position) < minimumSpawnerDistance) return false;
                    }

                    return true;
                }
            }

            Debug.Log($"[{name}] No room to add Element Spawner");
            return false;
        }


        public void CreateSpawner(ElementTypes elementType, Vector3 seedPodPos)
        {
            bool hasInitializedSpawner = false;
            int existingSpawners = 0;

            for (int i = 0; i < elementSpawners.Length; i++)
            {
                if (!hasInitializedSpawner && elementSpawners[i] == null)
                {
                    hasInitializedSpawner = true;
                    existingSpawners++;

                    ElementData elementData = elementTypeManager.elementTypeData[(int)elementType];

                    Vector3 elementSpawnerPos = new Vector3(seedPodPos.x, transform.position.y, seedPodPos.z);
                    Quaternion elementSpawnerRot = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));

                    elementSpawners[i] = (ElementSpawner)Instantiate(elementData.ElementSpawnerPrefab, elementSpawnerPos, elementSpawnerRot, transform).GetComponent<ElementInteractionHandler>().childElement;
                    elementSpawners[i].elementTypeManager = elementTypeManager;
                    elementSpawners[i].elementTypeId = elementType;
                    elementSpawners[i].spawnerAnim.Play("GrowthStageOne");
                    elementSpawners[i].parentSpawnPlot = this;

                    Debug.Log($"Successfully created [{elementSpawners[i].name}]");
                }
                else if (elementSpawners[i] != null) existingSpawners++;
            }

            //Disable PlotIndicator if all elementSpawners slots are full
            if (existingSpawners == elementSpawners.Length) plotAnimator.Play("SpawnPlotDie");
        }

        public void RemoveSpawner(ElementSpawner spawner)
        {
            //Remove self from ElementSpawnerPlot's ElementSpawners list
            int existingSpawners = elementSpawners.Length;

            for (int i = 0; i < elementSpawners.Length; i++)
            {
                if (elementSpawners[i] == spawner)
                {
                    elementSpawners[i] = null;
                    existingSpawners--;
                }
                else if (elementSpawners[i] == null) existingSpawners--;
            }

            if (existingSpawners == elementSpawners.Length - 1) plotAnimator.Play("SpawnPlotRevive");
        }

        public void AddSpawner(ElementSpawner spawner)
        {
            bool hasInitializedSpawner = false;
            int existingSpawners = 0;

            for (int i = 0; i < elementSpawners.Length; i++)
            {
                if (!hasInitializedSpawner && elementSpawners[i] == null)
                {
                    hasInitializedSpawner = true;
                    existingSpawners++;

                    elementSpawners[i] = spawner;
                }
                else if (elementSpawners[i] != null) existingSpawners++;

            }

            //Disable PlotIndicator if all elementSpawners slots are full
            if (existingSpawners == elementSpawners.Length) plotAnimator.Play("SpawnPlotDie");
        }
    }
}
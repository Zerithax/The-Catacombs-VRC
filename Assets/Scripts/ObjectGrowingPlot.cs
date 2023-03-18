using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Catacombs.Base;
using Argus.ItemSystem;
using Argus.ItemSystem.Editor;

namespace Catacombs.ElementSystem.Runtime
{
    public class ObjectGrowingPlot : UdonSharpBehaviour
    {
        [SerializeField] private ElementTypeManager elementTypeManager;
        [SerializeField] private int maxObjects = 1;
        [SerializeField] private float minimumObjectDistance = 0.1f;
        [SerializeField] private Animator plotAnimator;
        public GrownObject[] grownObjects;

        private void Start()
        {
            if (plotAnimator == null) plotAnimator = GetComponentInChildren<Animator>();

            if (grownObjects.Length == 0) grownObjects = new GrownObject[maxObjects];
            //If the plot starts with existing objects, start up their animations
            else
            {
                int i;
                for (i = 0; i < grownObjects.Length; i++)
                {
                    if (grownObjects[i] != null) grownObjects[i].grownObjectAnim.Play("GrowthStage1");
                    else break;
                }

                //Disable Indicator if plot starts full
                if (i == grownObjects.Length) plotAnimator.Play("GrowthPlotDie");
            }
        }

        public bool RoomToAddObject(Vector3 grownObjectPos)
        {
            for (int i = 0; i < grownObjects.Length; i++)
            {
                if (grownObjects[i] == null)
                {
                    //Fail if Seed Pod has been placed too close to an existing elementSpawner
                    for (int j = 0; j < i; j++)
                    {
                        if (Vector3.Distance(grownObjectPos, grownObjects[j].transform.position) < minimumObjectDistance)
                        {
                            Log($"New Element Spawner placed too close to existing Spawner");
                            return false;
                        }
                    }

                    return true;
                }
            }

            Log($"No room to add Element Spawner");
            return false;
        }

        public void CreateSpawner(ElementTypes elementType, Vector3 seedPodPos)
        {
            bool hasInitializedSpawner = false;
            int existingGrownObjects = 0;

            for (int i = 0; i < grownObjects.Length; i++)
            {
                if (!hasInitializedSpawner && grownObjects[i] == null)
                {
                    hasInitializedSpawner = true;
                    existingGrownObjects++;

                    ElementData elementData = elementTypeManager.elementDataObjs[(int)elementType];

                    Vector3 elementSpawnerPos = new Vector3(seedPodPos.x, transform.position.y, seedPodPos.z);
                    Quaternion elementSpawnerRot = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));

                    grownObjects[i] = (ElementSpawner)Instantiate(elementData.ElementSpawnerPrefab, elementSpawnerPos, elementSpawnerRot, transform).GetComponent<ElementInteractionHandler>().childElement;
                    grownObjects[i].elementTypeManager = elementTypeManager;
                    grownObjects[i].elementTypeId = elementType;
                    grownObjects[i].grownObjectAnim.Play("GrowthStage1");
                    grownObjects[i].parentSpawnPlot = this;

                    Log($"Successfully created [{grownObjects[i].name}]");
                }
                else if (grownObjects[i] != null) existingGrownObjects++;
            }

            //Disable PlotIndicator if all elementSpawners slots are full
            if (existingGrownObjects == grownObjects.Length) plotAnimator.Play("GrowthPlotDie");
        }

        public void RemoveGrownObject(GrownObject grownObject)
        {
            //Remove self from ElementSpawnerPlot's ElementSpawners list
            int existingGrownObjects = grownObjects.Length;

            for (int i = 0; i < grownObjects.Length; i++)
            {
                if (grownObjects[i] == grownObject)
                {
                    grownObjects[i] = null;
                    existingGrownObjects--;
                }
                else if (grownObjects[i] == null) existingGrownObjects--;
            }

            if (existingGrownObjects == grownObjects.Length - 1) plotAnimator.Play("GrowthPlotRevive");
        }

        public void AddObject(GrownObject spawner)
        {
            bool hasInitializedObject = false;
            int existingObjects = 0;

            for (int i = 0; i < grownObjects.Length; i++)
            {
                if (!hasInitializedObject && grownObjects[i] == null)
                {
                    hasInitializedObject = true;
                    existingObjects++;

                    grownObjects[i] = spawner;
                }
                else if (grownObjects[i] != null) existingObjects++;
            }

            //Disable PlotIndicator if all grownObject slots are full
            if (existingObjects == grownObjects.Length) plotAnimator.Play("GrowthPlotDie");
        }

        private void Log(string message)
        {
            Debug.Log($"[{name}] {message}", this);
        }
    }
}
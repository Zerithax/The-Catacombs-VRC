using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Catacombs.Base;
using Argus.ItemSystem;
using Argus.ItemSystem.Editor;
using Cysharp.Threading.Tasks.Triggers;

namespace Catacombs.ElementSystem.Runtime
{
    public class ObjectGrowingPlot : UdonSharpBehaviour
    {
        [SerializeField] private ItemPooler itemPooler;
        [SerializeField] private ElementTypeManager elementTypeManager;
        [SerializeField] private int maxObjects = 1;
        [SerializeField] private float minimumObjectDistance = 0.1f;
        [SerializeField] private Animator plotAnimator;
        public GrownObject[] grownObjects;

        private void Start()
        {
            if (plotAnimator == null) plotAnimator = GetComponentInChildren<Animator>();

            if (grownObjects.Length == 0)
            {
                grownObjects = new GrownObject[maxObjects];
            }
            //If the plot starts with existing objects, PullElementType on each (consequentially starting up their animations)
            else
            {
                int i;
                for (i = 0; i < grownObjects.Length; i++)
                {
                    if (grownObjects[i] != null)
                    {
                        grownObjects[i]._PullElementType();
                    }
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
                        if (grownObjects[j] == null) return true;

                        if (Vector3.Distance(grownObjectPos, grownObjects[j].transform.position) < minimumObjectDistance)
                        {
                            Log($"New GrownObject placed too close to existing Object");
                            return false;
                        }
                    }

                    return true;
                }
            }

            Log($"No room to add GrownObject");
            return false;
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

        public void AddExistingObject(GrownObject existingObject)
        {
            Log($"Adding existing Object {existingObject.gameObject.name} to grownObjects list");

            bool hasInitializedObject = false;
            int existingGrownObjects = 0;

            for (int i = 0; i < grownObjects.Length; i++)
            {
                if (!hasInitializedObject && grownObjects[i] == null)
                {
                    hasInitializedObject = true;
                    existingGrownObjects++;

                    grownObjects[i] = existingObject;
                }
                else if (grownObjects[i] != null) existingGrownObjects++;
            }

            //Disable PlotIndicator if all grownObject slots are full
            if (existingGrownObjects == grownObjects.Length) plotAnimator.Play("GrowthPlotDie");
        }

        public void AddNewObject(GameObject grownObjectPrefab, Vector3 newObjectPos)
        {
            //If the Prefab's InteractionHandler's childElement isn't a GrownObject, cancel!
            ElementInteractionHandler interactionHandler = grownObjectPrefab.GetComponent<ElementInteractionHandler>();
            if (interactionHandler == null) return;

            bool hasInitializedObject = false;
            int existingGrownObjects = 0;

            for (int i = 0; i < grownObjects.Length; i++)
            {
                if (!hasInitializedObject && grownObjects[i] == null)
                {
                    Quaternion grownObjectRot = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));

                    grownObjects[i] = (GrownObject)Instantiate(grownObjectPrefab, newObjectPos, grownObjectRot).GetComponent<ElementInteractionHandler>().childElement;

                    grownObjects[i].transform.parent.SetParent(transform, true);

                    hasInitializedObject = true;
                    existingGrownObjects++;
                }
                else if (grownObjects[i] != null) existingGrownObjects++;
            }

            //Disable PlotIndicator if all grownObject slots are full
            if (existingGrownObjects == grownObjects.Length) plotAnimator.Play("GrowthPlotDie");
        }

        //v2: Providing an elementType instead of a GrownObject tells us to generate an Element Spawner off of that ID instead!
        public void AddNewObject(ElementTypes elementType, Vector3 newObjectPos)
        {
            bool hasInitializedSpawner = false;
            int existingGrownObjects = 0;

            for (int i = 0; i < grownObjects.Length; i++)
            {
                if (!hasInitializedSpawner && grownObjects[i] == null)
                {
                    ElementData elementData = elementTypeManager.elementDataObjs[(int)elementType];

                    Vector3 elementSpawnerPos = new Vector3(newObjectPos.x, transform.position.y, newObjectPos.z);
                    Quaternion elementSpawnerRot = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));

                    ElementSpawner newElementSpawner = itemPooler.RequestElementSpawner();

                    if (newElementSpawner != null)
                    {
                        hasInitializedSpawner = true;
                        existingGrownObjects++;

                        //Put new Element Spawner in ground
                        newElementSpawner.parentObject.transform.SetPositionAndRotation(elementSpawnerPos, elementSpawnerRot);
                        newElementSpawner.parentObject.transform.parent = transform;

                        //Set pickup/interaction collider sizes
                        CapsuleCollider parentCollider = newElementSpawner.parentObject.GetComponent<CapsuleCollider>();
                        parentCollider.center = new Vector3(0, elementData.colliderYPos, 0);
                        parentCollider.radius = elementData.colliderRadius;
                        parentCollider.height = elementData.colliderHeight;

                        CapsuleCollider spawnerCollider = newElementSpawner.GetComponent<CapsuleCollider>();
                        spawnerCollider.center = new Vector3(0, elementData.colliderYPos, 0);
                        spawnerCollider.radius = elementData.colliderRadius;
                        spawnerCollider.height = elementData.colliderHeight;

                        //Create Element Spawn Transforms
                        Instantiate(elementData.ElementSpawnTransforms, newElementSpawner.transform).SetActive(false);

                        //Set animator controller
                        newElementSpawner.grownObjectAnim.runtimeAnimatorController = elementData.ElementSpawnerAnimator;

                        //Instantiate each animation stage under the Spawner
                        for (int j = 0; j < elementData.ElementSpawnerGrowthPrefabs.Length; j++)
                        {
                            GameObject animationPrefab = Instantiate(elementData.ElementSpawnerGrowthPrefabs[j], newElementSpawner.transform);
                            animationPrefab.name = animationPrefab.name.Remove(animationPrefab.name.Length - 7);
                        }

                        //Rebind Animator to retrieve mesh data from above
                        newElementSpawner.grownObjectAnim.Rebind();

                        //Pull ElementSpawner ElementType data (& consequentially start animations)
                        newElementSpawner.elementTypeId = elementType;
                        newElementSpawner.parentSpawnPlot = this;
                        newElementSpawner._PullElementType();

                        grownObjects[i] = newElementSpawner;

                        Log($"Successfully created [{grownObjects[i].name}]", grownObjects[i]);
                    }
                }
                else if (grownObjects[i] != null) existingGrownObjects++;
            }

            //Disable PlotIndicator if all elementSpawner slots are full
            if (existingGrownObjects == grownObjects.Length) plotAnimator.Play("GrowthPlotDie");
        }

        private void Log(string message)
        {
            Debug.Log($"[{name}] {message}", this);
        }

        private void Log(string message, Object context)
        {
            Debug.Log($"[{name}] {message}", context);
        }
    }
}
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
            //Remove GrownObject from ElementSpawners list
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

        public void AddNewGrownObject(Vector3 newGrownObjectPos, GrownObjectType grownObjectType, ElementTypes grownObjectElement)
        {
            bool hasInitializedGrownObject = false;
            int existingGrownObjects = 0;

            switch (grownObjectType)
            {
                default:

                    LogWarning("Attempted to AddNewGrownObject with grownObjectType 0, how did you get here?");
                    return;

                case GrownObjectType.ElementSpawner:

                    for (int i = 0; i < grownObjects.Length; i++)
                    {
                        if (!hasInitializedGrownObject && grownObjects[i] == null)
                        {
                            ElementData elementData = elementTypeManager.elementDataObjs[(int)grownObjectElement];

                            Vector3 elementSpawnerPos = new Vector3(newGrownObjectPos.x, transform.position.y, newGrownObjectPos.z);
                            Quaternion elementSpawnerRot = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));

                            ElementSpawner newElementSpawner = itemPooler.RequestElementSpawner();

                            if (newElementSpawner != null)
                            {
                                hasInitializedGrownObject = true;
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
                                newElementSpawner.grownObjectAnim.runtimeAnimatorController = elementData.GrownObjectAnimator;

                                //Instantiate each animation stage under the Spawner
                                for (int j = 0; j < elementData.GrownObjectGrowthPrefabs.Length; j++)
                                {
                                    GameObject animationPrefab = Instantiate(elementData.GrownObjectGrowthPrefabs[j], newElementSpawner.transform);
                                    animationPrefab.name = animationPrefab.name.Remove(animationPrefab.name.Length - 7);
                                }

                                //Rebind Animator to retrieve mesh data from above
                                newElementSpawner.grownObjectAnim.Rebind();

                                //Pull ElementSpawner ElementData (& consequentially start animations)
                                newElementSpawner.elementTypeId = grownObjectElement;
                                newElementSpawner.parentSpawnPlot = this;
                                newElementSpawner._PullElementType();

                                grownObjects[i] = newElementSpawner;

                                Log($"Successfully created [{grownObjects[i].name}]", grownObjects[i]);
                            }
                        }
                        else if (grownObjects[i] != null) existingGrownObjects++;
                    }
                    break;

                case GrownObjectType.GrowableLink:

                    for (int i = 0; i < grownObjects.Length; i++)
                    {
                        if (!hasInitializedGrownObject && grownObjects[i] == null)
                        {
                            ElementData elementData = elementTypeManager.elementDataObjs[(int)grownObjectElement];

                            Vector3 linkTorchPos = new Vector3(newGrownObjectPos.x, transform.position.y, newGrownObjectPos.z);
                            Quaternion linkTorchRot = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));

                            GrowableLink newLink = itemPooler.RequestGrowableLink();

                            if (newLink != null)
                            {
                                hasInitializedGrownObject = true;
                                existingGrownObjects++;

                                //Put new GrowableLink in ground
                                newLink.parentObject.transform.SetPositionAndRotation(linkTorchPos, linkTorchRot);
                                newLink.parentObject.transform.parent = transform;

                                //Set pickup/interaction collider sizes
                                CapsuleCollider parentCollider = newLink.parentObject.GetComponent<CapsuleCollider>();
                                parentCollider.center = new Vector3(0, elementData.colliderYPos, 0);
                                parentCollider.radius = elementData.colliderRadius;
                                parentCollider.height = elementData.colliderHeight;

                                CapsuleCollider spawnerCollider = newLink.GetComponent<CapsuleCollider>();
                                spawnerCollider.center = new Vector3(0, elementData.colliderYPos, 0);
                                spawnerCollider.radius = elementData.colliderRadius;
                                spawnerCollider.height = elementData.colliderHeight;

                                //Set animator controller
                                newLink.grownObjectAnim.runtimeAnimatorController = elementData.GrownObjectAnimator;

                                //Instantiate each animation stage under the Link
                                for (int j = 0; j < elementData.GrownObjectGrowthPrefabs.Length; j++)
                                {
                                    GameObject animationPrefab = Instantiate(elementData.GrownObjectGrowthPrefabs[j], newLink.transform);
                                    animationPrefab.name = animationPrefab.name.Remove(animationPrefab.name.Length - 7);
                                }

                                //Rebind Animator to retrieve mesh data from above
                                newLink.grownObjectAnim.Rebind();

                                //Pull GrowableLink ElementData (& consequentially start animations)
                                newLink.elementTypeId = grownObjectElement;
                                newLink.parentSpawnPlot = this;
                                newLink.torchColor = elementTypeManager.elementDataObjs[(int)grownObjectElement].elementColor;
                                newLink._PullElementType();

                                grownObjects[i] = newLink;

                                Log($"Successfully created [{grownObjects[i].name}]", grownObjects[i]);
                            }
                        }
                        else if (grownObjects[i] != null) existingGrownObjects++;
                    }
                    break;
            }

            //Disable PlotIndicator if all elementSpawner slots are full
            if (existingGrownObjects == grownObjects.Length) plotAnimator.Play("GrowthPlotDie");
        }

        /*
        public void AddNewGrowableLink(Vector3 newObjectPos)
        {
            bool hasInitializedObject = false;
            int existingGrownObjects = 0;

            for (int i = 0; i < grownObjects.Length; i++)
            {
                if (!hasInitializedObject && grownObjects[i] == null)
                {
                    Quaternion grownObjectRot = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));

                    grownObjects[i] = itemPooler.RequestGrowableLink();
                    grownObjects[i].transform.SetPositionAndRotation(newObjectPos, grownObjectRot);
                    grownObjects[i].transform.parent = transform;

                    hasInitializedObject = true;
                    existingGrownObjects++;
                }
                else if (grownObjects[i] != null) existingGrownObjects++;
            }

            //Disable PlotIndicator if all grownObject slots are full
            if (existingGrownObjects == grownObjects.Length) plotAnimator.Play("GrowthPlotDie");
        }

        public void AddNewElementSpawner(ElementTypes elementType, Vector3 newObjectPos)
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

                    ElementSpawner newElementSpawner = (ElementSpawner)itemPooler.RequestGrownObject(elementData.grownObjectType);

                    //ElementSpawner newElementSpawner = itemPooler.RequestElementSpawner();

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
                        newElementSpawner.grownObjectAnim.runtimeAnimatorController = elementData.GrownObjectAnimator;

                        //Instantiate each animation stage under the Spawner
                        for (int j = 0; j < elementData.GrownObjectGrowthPrefabs.Length; j++)
                        {
                            GameObject animationPrefab = Instantiate(elementData.GrownObjectGrowthPrefabs[j], newElementSpawner.transform);
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
        */

        private void Log(string message)
        {
            Debug.Log($"[{name}] {message}", this);
        }

        private void Log(string message, Object context)
        {
            Debug.Log($"[{name}] {message}", context);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[{name}] {message}", this);
        }

        private void LogError(string message)
        {
            Debug.Log($"[{name}] {message}", this);
        }
    }
}
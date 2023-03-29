using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Argus.ItemSystem.Editor;

namespace Catacombs.ElementSystem.Runtime
{
    public class BaseElement : RuntimeElement
    {
        [Header("Element Settings")]
        public int elementPrecipitateAmount = 5;
        public ElementPrecipitates precipitateType;
        public float shrinkSpeed;
        public ElementSpawner parentElementSpawner;
        [SerializeField] private GameObject baseElementCollisionPrefab;


        [Header("Can Spawn Seed Pod/Create Element Spawner")]
        public bool canSpawnSeedPod;
        public GameObject elementSpawnerPrefab;
        public bool canPlantManually;

        private Animator seedPodAnim;
        private ObjectGrowingPlot collidingObjectGrowingPlot;

        private bool isElligible;
        private bool isPlanting;
        private float curPlotInteractTime;
        private float lastPlotPlantTime;

        public Vector3 baseScale;
        private int baseSize;
        private int curSize;
        private Vector3[] sizeIntervals = new Vector3[0];

        private Vector3 collidingPestlePos;

        public override void KillElement()
        {
            itemPooler.ReturnBaseElement(parentObject);
        }

        #region INIT
        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            ElementData elementData = elementTypeManager.elementDataObjs[(int)elementTypeId];

            if (elementData.canSpawnSeedPod)
            {
                isElligible = UnityEngine.Random.Range(0f, 100f) <= elementData.seedPodSpawnChance;

                if (isElligible)
                {
                    GameObject seedPod = Instantiate(elementData.SeedPodPrefab, parentObject.transform);
                    seedPod.transform.localPosition = elementData.seedPodPosOffset;

                    seedPodAnim = seedPod.GetComponent<Animator>();
                    if (seedPodAnim != null) seedPodAnim.keepAnimatorControllerStateOnDisable = false;

                    //Override Element to Seed Pod variant's ElementType (not always different) 
                    elementTypeId = elementTypeManager.elementDataObjs[(int)elementTypeId].seedPodElementType;
                }
                else if (elementData.ElementLeavesPrefab != null) Instantiate(elementData.ElementLeavesPrefab, parentObject.transform);
            }

            //ELEMENT DATA
            //Re-fetch elementData if it was changed from turning into a Seed Pod
            if (elementTypeId != elementData.elementTypeId)
            {
                elementData = elementTypeManager.elementDataObjs[(int)elementTypeId];

                //These are usually set in RuntimeElement, but need to be re-fetched if the elementID did in fact change
                elementColor = elementData.elementColor;
                despawnTime = elementData.despawnTime;
                killVelocity = elementData.killVelocity;
            }

            elementPrecipitateAmount = elementData.elementPrecipitateAmount;
            precipitateType = elementData.elementPrecipitateType;
            shrinkSpeed = elementData.shrinkSpeed;
            canPlantManually = elementData.canPlantManually;

            //Calculate size intervals to spawn each precipitate at (+ 1 so we don't need 0 scale before being destroyed)
            baseSize = elementPrecipitateAmount + 1;
            curSize = baseSize;
            sizeIntervals = new Vector3[baseSize];

            baseScale = transform.parent.localScale;

            for (int i = 0; i < baseSize; i++) sizeIntervals[i] = baseScale / baseSize * i;

            //Manually assign filter, collider, & rigidbody data from elementData
            parentObject.GetComponent<MeshFilter>().mesh = elementData.baseElementMesh;
            parentObject.GetComponent<SphereCollider>().radius = elementData.pickupColliderRadius;

            Rigidbody parentRb = parentObject.GetComponent<Rigidbody>();
            parentRb.mass = elementData.rbMass;
            parentRb.drag = elementData.rbDrag;
            parentRb.angularDrag = elementData.rbAngularDrag;

            Instantiate(elementData.BaseElementCollisionPrefab, parentObject.transform);

            //COLOR
            elementRenderer.material.color = elementColor;
            elementRenderer.material.SetFloat("_Glossiness", 0);

            //NAME
            parentObject.name = $"{parentObject.name} Element";

            Log($"Retrieved Element Data for {elementData.name}");
            return true;
        }

        public override void _AttemptDespawn()
        {
            if (lastLandTime == lastInteractTime)
            {
                Log("Timed out, returning to Item Pooler...");

                itemPooler.ReturnBaseElement(parentObject);
            }
        }
        #endregion

        #region SPAWNERS & PLANTING
        public override void Grabbed()
        {
            base.Grabbed();

            if (parentElementSpawner != null)
            {
                parentElementSpawner.DetachElement(this);

                disableContainment = false;
                parentElementSpawner = null;
            }

            if (isPlanting) CancelPlanting();
        }

        private void CancelPlanting()
        {
            curPlotInteractTime = Time.time;

            isPlanting = false;
            seedPodAnim.Play("None");

            //Undo parenting swap
            parentObject = transform.parent.gameObject;
            parentObject.transform.SetParent(null);

            seedPodAnim.transform.SetParent(parentObject.transform, true);
            seedPodAnim.transform.localPosition = Vector3.up * 0.0142f;
        }

        public override void Dropped()
        {
            if (isElligible && canPlantManually && collidingObjectGrowingPlot != null) RequestObjectGrowingPlot();
            else rb.isKinematic = false;
        }

        private void RequestObjectGrowingPlot()
        {
            Log($"Placed on an Object Growing Plot!");

            //If the item pooler doesn't have any ElementSpawners available, immediately reject the request
            if (!itemPooler.ElementSpawnerAvailable())
            {
                rb.isKinematic = false;
                return;
            }

            //The position the seed plot will lie is the Element's X/Z aligned with the GrowingPlot's Y
            Vector3 seedPlotPos = new Vector3(parentObject.transform.position.x, collidingObjectGrowingPlot.transform.position.y, parentObject.transform.position.z);

            if (collidingObjectGrowingPlot.RoomToAddObject(seedPlotPos))
            {
                isPlanting = true;

                //For tracking cancelling & re-planting
                curPlotInteractTime = Time.time;
                lastPlotPlantTime = curPlotInteractTime;

                //Position Element in Ground
                parentObject.transform.SetPositionAndRotation(seedPlotPos, Quaternion.Euler(new Vector3(0, parentObject.transform.position.y, 0)));

                //Swap parenting around for animations
                seedPodAnim.transform.SetParent(collidingObjectGrowingPlot.transform.parent, true);
                parentObject.transform.SetParent(seedPodAnim.transform.GetChild(0).transform, true);
                parentObject = seedPodAnim.gameObject;

                //3 second long animation that removes the collider
                seedPodAnim.Play("SeedPodOpen");
                SendCustomEventDelayedSeconds(nameof(_AttemptCreateSpawner), 3);
            }
            else rb.isKinematic = false;
        }

        public void _AttemptCreateSpawner()
        {
            if (lastPlotPlantTime == curPlotInteractTime)
            {
                lastPlotPlantTime = 0;

                collidingObjectGrowingPlot.AddNewObject(elementTypeId, seedPodAnim.transform.position);

                //Kill self a few seconds after ElementSpawner has appeared
                SendCustomEventDelayedSeconds(nameof(_FixTransformsThenKillElement), 7);
            }
        }

        public void _FixTransformsThenKillElement()
        {
            //Undo animations parenting
            transform.parent.parent = seedPodAnim.transform.parent;
            seedPodAnim.transform.parent = transform.parent;

            parentObject = transform.parent.gameObject;

            KillElement();
        }

        protected override void AdditionalTriggerEnter(Collider other) { if (other.gameObject.layer == 2) collidingObjectGrowingPlot = other.GetComponent<ObjectGrowingPlot>(); }

        protected override void AdditionalTriggerExit(Collider other) { if (other.gameObject.layer == 2) collidingObjectGrowingPlot = null; }
        #endregion

        #region SPAWNING PRECIPITATE

        //Track the position and shrink if colliding with a pestle that is actively moving
        private void OnTriggerStay(Collider other)
        {
            if (other == null || other.gameObject == null) return;

            if (other.gameObject.layer == 23) TrackPestle(other.transform);
        }

        private void TrackPestle(Transform other)
        {
            //TOOD: This might be framerate dependent, maybe multiply shrinkSpeed by by normalized magnitude of pestle distance?

            Vector3 curPestlePos = other.transform.position;

            //If is in Container and collided Pestle is actively moving
            if (isContained && curPestlePos != collidingPestlePos)
            {
                //float distanceMultiplier = (curPestlePos - collidingPestlePos).magnitude;

                //Subtract a steady stream of scale
                parentObject.transform.localScale -= new Vector3(shrinkSpeed, shrinkSpeed, shrinkSpeed);

                //At every equal interval of baseScale, spawn Dust and add to parent container's containedDusts[]
                for (int i = sizeIntervals.Length - 1; i > 0; i--)
                {
                    if ((float)Math.Round(parentObject.transform.localScale.x, 3) == (float)Math.Round(sizeIntervals[i].x, 3))
                    {
                        ElementPrecipitate newPrecipitate = itemPooler.RequestElementPrecipitate();

                        if (newPrecipitate != null)
                        {
                            newPrecipitate.transform.position = parentObject.transform.position + Vector3.up * 0.1f;
                            newPrecipitate.transform.parent = parentObject.transform.parent;

                            newPrecipitate.elementTypeId = elementTypeId;

                            newPrecipitate.rb.isKinematic = false;

                            newPrecipitate._PullElementType();

                            if (newPrecipitate.elementPrecipitateType == ElementPrecipitates.None) newPrecipitate._VerifyDripType();

                            switch (newPrecipitate.elementPrecipitateType)
                            {
                                case ElementPrecipitates.Dust:
                                    parentContainer.AttemptConsumeDust(newPrecipitate);
                                    break;

                                case ElementPrecipitates.Drip:
                                    parentContainer.AttemptContainLiquid(newPrecipitate);
                                    break;
                            }

                            ShrinkElement();
                        }

                        break;
                    }
                }
            }

            collidingPestlePos = curPestlePos;
        }

        private void ShrinkElement()
        {
            curSize--;
            Log($"Shrinking to {curSize}");

            if (curSize <= baseSize - elementPrecipitateAmount)
            {
                Log($"Out of berry dust, returning to Item Pool...");
                itemPooler.ReturnBaseElement(parentObject);
            }
        }
        #endregion
    }
}
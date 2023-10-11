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
        public float shrinkSpeed;
        public ElementSpawner parentElementSpawner;
        [SerializeField] private GameObject baseElementCollisionPrefab;

        public Vector3 baseScale;
        private int baseSize;
        private int curSize;
        private Vector3[] sizeIntervals = new Vector3[0];

        private Vector3 collidingPestlePos;

        public bool isSeedPod;
        private Animator seedPodAnim;
        private ObjectGrowingPlot collidingObjectGrowingPlot;

        private bool isPlanting;
        private float curPlotInteractTime;
        private float lastPlotPlantTime;

        #region INIT
        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            if (isSeedPod)
            {
                GameObject seedPod = Instantiate(elementTypeData.SeedPodPrefab, parentObject.transform);
                seedPod.transform.localPosition = elementTypeData.seedPodPosOffset;

                seedPodAnim = seedPod.GetComponent<Animator>();
                if (seedPodAnim != null) seedPodAnim.keepAnimatorControllerStateOnDisable = false;
            }

            shrinkSpeed = elementTypeData.shrinkSpeed;

            //Calculate size intervals to spawn each precipitate at (+ 1 so we don't need 0 scale before being destroyed)
            baseSize = elementTypeData.elementPrecipitateAmount + 1;
            curSize = baseSize;
            sizeIntervals = new Vector3[baseSize];

            baseScale = transform.parent.localScale;

            for (int i = 0; i < baseSize; i++) sizeIntervals[i] = baseScale / baseSize * i;

            //Manually assign filter, collider, & rigidbody data from elementData
            parentObject.GetComponent<MeshFilter>().mesh = elementTypeData.baseElementMesh;
            parentObject.GetComponent<SphereCollider>().radius = elementTypeData.pickupColliderRadius;

            Rigidbody parentRb = parentObject.GetComponent<Rigidbody>();
            parentRb.mass = elementTypeData.rbMass;
            parentRb.drag = elementTypeData.rbDrag;
            parentRb.angularDrag = elementTypeData.rbAngularDrag;

            Instantiate(elementTypeData.BaseElementCollisionPrefab, parentObject.transform);

            //COLOR
            elementRenderer.material.color = elementColor;
            elementRenderer.material.SetFloat("_Glossiness", 0);

            //NAME
            parentObject.name = $"{parentObject.name} Element";

            Log($"Retrieved Element Data for {elementTypeData.name}");
            return true;
        }
        #endregion

        public override void KillElement() { itemPooler.ReturnBaseElement(parentObject); }

        public override void _AttemptDespawn()
        {
            if (lastLandTime == lastInteractTime)
            {
                Log("Timed out, returning to Item Pooler...");

                itemPooler.ReturnBaseElement(parentObject);
            }
        }

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
            if (isSeedPod && elementTypeData.canPlantManually && collidingObjectGrowingPlot != null) AttemptPlantSeedPod();
            else rb.isKinematic = false;
        }

        private void AttemptPlantSeedPod()
        {
            Log($"Placed on an Object Growing Plot!");

            //If the Item Pooler doesn't have the necessary GrownObject available, immediately reject the request
            switch (elementTypeData.grownObjectType)
            {
                case GrownObjectType.None:

                    break;
                case GrownObjectType.ElementSpawner:

                    if (!itemPooler.ElementSpawnerAvailable())
                    {
                        rb.isKinematic = false;
                        return;
                    }
                    break;
                case GrownObjectType.GrowableLink:

                    if (!itemPooler.GrowableLinkAvailable())
                    {
                        rb.isKinematic = false;
                        return;
                    }
                    break;
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

                ElementTypes elementTypeToUse = elementTypeData.grownObjectElement == 0 ? elementTypeId : elementTypeData.grownObjectElement;

                collidingObjectGrowingPlot.AddNewGrownObject(seedPodAnim.transform.position, elementTypeData.grownObjectType, elementTypeToUse);

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
            //TODO: This might be framerate dependent, maybe multiply shrinkSpeed by by normalized magnitude of pestle distance?

            Vector3 curPestlePos = other.transform.position;

            //If is in Container and collided Pestle is actively moving
            if (isContained && curPestlePos != collidingPestlePos)
            {
                //Subtract a steady stream of scale
                parentObject.transform.localScale -= new Vector3(elementTypeData.shrinkSpeed, elementTypeData.shrinkSpeed, elementTypeData.shrinkSpeed);

                //At every equal interval of baseScale, spawn Dust and add to parent container's containedDusts[]
                for (int i = sizeIntervals.Length - 1; i > 0; i--)
                {
                    if ((float)Math.Round(parentObject.transform.localScale.x, 3) == (float)Math.Round(sizeIntervals[i].x, 3))
                    {
                        ElementPrecipitate newPrecipitate = itemPooler.RequestElementPrecipitate();

                        if (newPrecipitate != null)
                        {
                            if (UnityEngine.Random.Range(0, 1) > elementTypeData.elementPrecipitateSpawnChance)
                            {
                                Log("Failed precipitateSpawnChance roll");
                            }
                            else
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

            //TODO tf is this calc
            if (curSize <= baseSize - elementTypeData.elementPrecipitateAmount)
            {
                Log($"Out of berry dust, returning to Item Pool...");
                itemPooler.ReturnBaseElement(parentObject);
            }
        }
        #endregion
    }
}
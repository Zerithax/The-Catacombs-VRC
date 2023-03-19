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
        public GameObject elementPrecipitatePrefab;
        public ElementPrecipitates precipitateType;
        public float shrinkSpeed;
        public ElementSpawner parentElementSpawner;
        public float seedGrowChance;

        [Header("Creates Element Spawner?")]
        public GameObject elementSpawnerPrefab;
        public bool canCreateSpawners;
        [SerializeField] private GameObject elementLeaves;
        [SerializeField] private GameObject seedPodPrefab;


        private Animator seedPodAnim;
        private bool isElligible;
        private ObjectGrowingPlot collidingObjectGrowingPlot;
        private bool isPlanting;
        private float curPlotInteractTime;
        private float lastPlotPlantTime;

        private Vector3 baseScale;
        private int baseSize;
        private int curSize;
        private Vector3[] sizeIntervals = new Vector3[0];

        private Vector3 collidingPestlePos;

        private bool hasSpawner;

        #region INIT
        protected override void AdditionalStart()
        {
            baseScale = transform.parent.localScale;

            hasSpawner = parentElementSpawner != null;

            //Calculate size intervals to spawn each precipitate at (+ 1 so we don't need 0 scale before being destroyed)
            baseSize = elementPrecipitateAmount + 1;
            curSize = baseSize;
            sizeIntervals = new Vector3[baseSize];

            for (int i = 0; i < baseSize; i++) sizeIntervals[i] = baseScale / baseSize * i;
        }

        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            //COLOR
            elementRenderer.material.color = elementColor;

            //ELEMEN DATA
            ElementData elementData = elementTypeManager.elementDataObjs[(int)elementTypeId];


            elementPrecipitateAmount = elementData.elementPrecipitateAmount;
            elementPrecipitatePrefab = elementData.ElementPrecipitatePrefab;
            precipitateType = elementData.elementPrecipitateType;
            shrinkSpeed = elementData.shrinkSpeed;
            elementSpawnerPrefab = elementData.ElementSpawnerPrefab;
            canCreateSpawners = elementData.canCreateSpawners;
            seedGrowChance = elementData.seedGrowChance;

            if (canCreateSpawners)
            {
                isElligible = UnityEngine.Random.Range(1, 101) <= seedGrowChance;

                if (isElligible)
                {
                    Destroy(elementLeaves);
                    seedPodAnim = Instantiate(seedPodPrefab, parentObject.transform).GetComponent<Animator>();
                    seedPodAnim.transform.localPosition = Vector3.up * 0.0142f;
                    seedPodAnim.keepAnimatorControllerStateOnDisable = false;
                }
            }

            //NAME
            parentObject.name = $"{parentObject.name} Element";

            Log($"Retrieved Element Data for {elementData.name}");
            return true;
        }
        #endregion

        #region SPAWNERS & PLANTING
        public override void Grabbed()
        {
            base.Grabbed();

            if (hasSpawner)
            {
                parentElementSpawner.DetachElement(this);

                parentElementSpawner = null;
                hasSpawner = false;
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
            if (isElligible && collidingObjectGrowingPlot != null) RequestObjectGrowingPlot();
            else rb.isKinematic = false;
        }

        private void RequestObjectGrowingPlot()
        {
            Log($"Placed on an Object Growing Plot!");

            //The position the seed plot will lie is the Element's X/Z aligned with the GrowingPlot's Y
            Vector3 seedPlotPos = new Vector3(parentObject.transform.position.x, collidingObjectGrowingPlot.transform.position.y, parentObject.transform.position.z);

            if (collidingObjectGrowingPlot.RoomToAddObject(seedPlotPos))
            {
                isPlanting = true;

                //For tracking cancelling & re-planting
                curPlotInteractTime = Time.time;
                lastPlotPlantTime = curPlotInteractTime;

                //Position Element in Ground
                parentObject.transform.position = seedPlotPos;
                parentObject.transform.rotation = Quaternion.Euler(new Vector3(0, parentObject.transform.position.y, 0));

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

                collidingObjectGrowingPlot.CreateSpawner(elementTypeId, seedPodAnim.transform.position);

                //Kill self a few seconds after ElementSpawner has appeared
                SendCustomEventDelayedSeconds(nameof(_DelayedKill), 7);
            }
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
            Vector3 curPestlePos = other.transform.position;

            //If is in Container and collided Pestle is actively moving
            if (isContained && curPestlePos != collidingPestlePos)
            {
                //Subtract a steady stream of scale
                parentObject.transform.localScale -= new Vector3(shrinkSpeed, shrinkSpeed, shrinkSpeed);

                //At every equal interval of baseScale, spawn Dust and add to parent container's containedDusts[]
                for (int i = sizeIntervals.Length - 1; i > 0; i--)
                {
                    if ((float)Math.Round(parentObject.transform.localScale.x, 3) == (float)Math.Round(sizeIntervals[i].x, 3))
                    {
                        ElementPrecipitate newPrecipitate = Instantiate(elementPrecipitatePrefab, parentObject.transform.position + Vector3.up * 0.1f, Quaternion.identity, parentObject.transform.parent).GetComponent<ElementPrecipitate>();

                        newPrecipitate.elementTypeManager = elementTypeManager;
                        newPrecipitate.elementTypeId = elementTypeId;

                        if (newPrecipitate.elementPrecipitateType == ElementPrecipitates.Drip) parentContainer.AttemptConsumeDust(newPrecipitate);

                        ShrinkBerry();
                        break;
                    }
                }
            }

            collidingPestlePos = curPestlePos;
        }

        private void ShrinkBerry()
        {
            curSize--;
            Log($"Shrinking to {curSize}");

            if (curSize <= baseSize - elementPrecipitateAmount)
            {
                Log($"Out of berry dust, destroying");
                Destroy(parentObject);
            }
        }
        #endregion
    }
}
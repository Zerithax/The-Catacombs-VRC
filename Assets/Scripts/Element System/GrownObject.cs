using UnityEngine;
using Argus.ItemSystem.Editor;
using System;

namespace Catacombs.ElementSystem.Runtime
{
    public class GrownObject : RuntimeElement
    {
        [Header("Initial Growth Details")]
        public ObjectGrowingPlot parentSpawnPlot;
        public Animator grownObjectAnim;
        public Collider physicsCollider;
        [SerializeField] private float curGrowthTime;
        [SerializeField] protected bool isRooted;
        public bool matured;

        [Header("Replanting")]
        [SerializeField] protected bool isCollidingGrowthPlot;
        [SerializeField] protected ObjectGrowingPlot collidingGrowthPlot;
        protected float lastPlotInteractTime;
        protected float lastPlotPlantTime;

        protected override void AdditionalStart()
        {
            if (grownObjectAnim == null) grownObjectAnim = GetComponent<Animator>();
            if (physicsCollider == null) physicsCollider = parentObject.GetComponent<Collider>();

            disableContainment = true;

            lastPlotPlantTime = (float)Math.Round(Time.time, 3);
            lastPlotInteractTime = lastPlotPlantTime;
        }

        protected override void AdditionalUpdate()
        {
            if (isRooted && !matured)
            {
                curGrowthTime += Time.deltaTime;

                float totalGrowthStages = elementTypeData.GrownObjectGrowthPrefabs.Length - 1;
                float growthPeriod = elementTypeData.grownObjectGrowTime;
                float growthIntervals = growthPeriod / totalGrowthStages;

                //Loop through growthStages and play an animation only if it's within the specific time interval of each growthStage
                for (int i = 1; i < totalGrowthStages; i++)
                {
                    //Each time growthTime reaches a new growthInterval, play the next growthStage[]
                    if (curGrowthTime >= growthIntervals * i && curGrowthTime < growthIntervals * (i + 1))
                    {
                        grownObjectAnim.Play($"GrowthStage{i}");
                    }

                    if (curGrowthTime >= growthPeriod)
                    {
                        grownObjectAnim.Play("GrowthCompleted");

                        matured = true;
                    }
                }
            }
        }

        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            if (grownObjectAnim.runtimeAnimatorController != null) grownObjectAnim.Play("GrowthStage0");
            isRooted = true;

            parentObject.name = $"Grown {parentObject.name}";

            Log($"Retrieved GrownObject Data from {elementTypeData.name}");
            return true;
        }

        public override void KillElement() { itemPooler.ReturnGrownObject(parentObject); }

        public override void _AttemptDespawn()
        {
            if (isRooted) return;

            if (lastPlotPlantTime == lastPlotInteractTime) KillElement();
        }

        public override void Grabbed()
        {
            base.Grabbed();

            if (parentSpawnPlot != null) RemoveFromPlot();
        }

        protected virtual void RemoveFromPlot()
        {
            parentSpawnPlot.RemoveGrownObject(this);

            isRooted = false;
            disableContainment = false;

            lastPlotInteractTime = lastInteractTime;
        }

        public override void Dropped()
        {
            if (isCollidingGrowthPlot)
            {
                //The position the GrownObject will lie is the Element's X/Z aligned with the SpawnerPlot's Y
                Vector3 seedPlotPos = new Vector3(parentObject.transform.position.x, collidingGrowthPlot.transform.position.y, parentObject.transform.position.z);

                if (collidingGrowthPlot.RoomToAddObject(seedPlotPos))
                {
                    Log($"Room to add object successful");

                    //For tracking death when not re-planted in time
                    lastPlotPlantTime = (float)Math.Round(Time.time, 3);
                    lastPlotInteractTime = lastPlotPlantTime;

                    //Position GrownObject in Spawner Plot
                    parentObject.transform.SetParent(collidingGrowthPlot.transform, true);
                    parentObject.transform.SetPositionAndRotation(seedPlotPos, Quaternion.Euler(new Vector3(0, parentObject.transform.position.y, 0)));

                    PlantedEvent();

                    collidingGrowthPlot.AddExistingObject(this);

                    rb.isKinematic = true;
                    isRooted = true;
                    disableContainment = true;

                    return;
                }
            }

            rb.isKinematic = false;
        }

        protected virtual void PlantedEvent() { }

        protected override void AdditionalTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == 2)
            {
                collidingGrowthPlot = other.GetComponent<ObjectGrowingPlot>();
                isCollidingGrowthPlot = true;
            }
        }

        protected override void AdditionalTriggerExit(Collider other)
        {
            if (other.gameObject.layer == 2)
            {
                collidingGrowthPlot = null;
                isCollidingGrowthPlot = false;
            }
        }
    }
}
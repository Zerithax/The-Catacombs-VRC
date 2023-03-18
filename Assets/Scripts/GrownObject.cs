using UnityEngine;
using Argus.ItemSystem.Editor;

namespace Catacombs.ElementSystem.Runtime
{
    public class GrownObject : RuntimeElement
    {
        [Header("Initial Growth Details")]
        public Animator grownObjectAnim;
        public ObjectGrowingPlot parentSpawnPlot;
        [SerializeField] protected bool isRooted = true;
        public int growthPeriod;
        [SerializeField] private float curGrowthTime;
        [SerializeField] private string[] growthStageStateNames;
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

            lastPlotPlantTime = Time.time;
            lastPlotInteractTime = lastPlotPlantTime;
        }

        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            ElementData elementData = elementTypeManager.elementDataObjs[(int)elementTypeId];

            growthPeriod = elementData.elementSpawnerGrowthPeriod;

            parentObject.name = $"Grown {parentObject.name}";

            Log($"Retrieved GrownObject Data from {elementData.name}");
            return true;
        }

        protected override void AdditionalUpdate()
        {
            if (isRooted && !matured)
            {
                //TODO: This might be broken rn (growth isn't happening... is growthTime even incrementing?)

                curGrowthTime += Time.deltaTime;

                float growthIntervals = growthPeriod / growthStageStateNames.Length;

                //Loop through growthStages and play an animation only if it's within the specific time interval of each growthStage
                for (int i = 0; i < growthStageStateNames.Length; i++)
                {
                    //Each time growthTime reaches a new growthInterval, play the next growthStage[]
                    if (curGrowthTime >= growthIntervals * i && curGrowthTime < growthIntervals * (i + 1))
                    {
                        Log($"Playing {growthStageStateNames[i]}");
                        grownObjectAnim.Play(growthStageStateNames[i]);
                    }

                    if (curGrowthTime >= growthPeriod)
                    {
                        Log("Playing GrowthCompleted");
                        grownObjectAnim.Play("GrowthCompleted");

                        matured = true;
                    }
                }

                /*
                if (growthTime >= growthPeriod / 2 && growthTime < growthPeriod)
                {
                    grownObjectAnim.Play("GrowthStageTwo");
                }
                else if (growthTime >= growthPeriod)
                {
                    grownObjectAnim.Play("SpawnerCompleted");

                    matured = true;
                }
                */
            }
        }

        public override void Grabbed()
        {
            base.Grabbed();

            RemoveFromPlot();
        }

        protected virtual void RemoveFromPlot()
        {
            parentSpawnPlot.RemoveGrownObject(this);

            isRooted = false;

            lastPlotInteractTime = Time.time;
        }

        public override void Dropped()
        {
            if (isCollidingGrowthPlot)
            {
                //The position the GrownObject will lie is the Element's X/Z aligned with the SpawnerPlot's Y
                Vector3 seedPlotPos = new Vector3(parentObject.transform.position.x, collidingGrowthPlot.transform.position.y, parentObject.transform.position.z);

                if (collidingGrowthPlot.RoomToAddObject(seedPlotPos))
                {
                    //For tracking death when not re-planted in time
                    lastPlotPlantTime = Time.time;
                    lastPlotInteractTime = lastPlotPlantTime;

                    //Position GrownObject in Spawner Plot
                    parentObject.transform.SetPositionAndRotation(seedPlotPos, Quaternion.Euler(new Vector3(0, parentObject.transform.position.y, 0)));
                    parentObject.transform.SetParent(collidingGrowthPlot.transform, true);

                    PlantedEvent();

                    collidingGrowthPlot.AddObject(this);

                    rb.isKinematic = true;
                    isRooted = true;

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

        public override void _DelayedKill()
        {
            //Only follow through with the kill if GrownElement hasn't been placed back into an ElementSpawnerPlot yet and it's been less at least 30 seconds (DelayedKill time)
            if (!isRooted && lastPlotPlantTime != lastPlotInteractTime && lastPlotInteractTime >= lastPlotInteractTime + 30) Destroy(parentObject);
            else return;

            base._DelayedKill();
        }
    }
}
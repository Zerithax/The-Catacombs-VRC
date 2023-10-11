using Argus.ItemSystem.Editor;
using Catacombs.Base;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.ElementSystem.Runtime
{
    public class ElementPrecipitate : RuntimeElement
    {
        [Header("Precipitate Settings")]
        public ElementPrecipitates elementPrecipitateType;
        public TrailRenderer trailRend;
        public Collider physicsCollider;

        [Space(3)]
        public bool precipitateIsPrimed;

        protected override void AdditionalStart() { physicsCollider = GetComponent<Collider>(); }

        protected override void AdditionalUpdate() { SimulateViscosity(); }

        private void SimulateViscosity()
        {
            //TODO: To remove the environment (isGrounded) restriction, get the angle of the ground's normal vs vector3.up and compare to an element-dependent threshold (ranged ~10-45 degrees)
            if (isCollidingSurface && !isGrounded && !isContained)
            {
                //Log("Simulating viscosity...");

                if (rb.velocity.magnitude < elementTypeData.minimumVelocity) rb.velocity *= 1 + elementTypeData.velocityMultiplier;
                else if (rb.velocity.magnitude > elementTypeData.maximumVelocity) rb.velocity *= 1 - elementTypeData.velocityMultiplier;
            }
        }

        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            //ELEMENT DATA
            elementPrecipitateType = elementTypeData.elementPrecipitateType;

            //PRECIPITATE TYPE
            string precipitateTypeName = "";

            switch (elementTypeData.elementPrecipitateType)
            {
                case ElementPrecipitates.Dust:

                    //COLOR
                    float elementTintAmount = (float)System.Math.Round(Random.Range(0.1f, 0.65f), 1);
                    //Hopefully I'm just stealing 25% from Green and giving it back to the Element (dust was way too green)
                    float greenTintAmount = 0.75f - elementTintAmount;
                    elementTintAmount += 0.25f;

                    elementRenderer.material.color = elementColor * elementTintAmount + Color.green * greenTintAmount;
                    elementRenderer.material.SetFloat("_Glossiness", 0);

                    rb.drag = 0.4f;
                    trailRend.emitting = false;
                    hideWhenContained = false;

                    //Dusts override despawn time to a fifth of Element's default despawnTime
                    canDespawn = true;
                    despawnTime = elementTypeData.despawnTime / 5;

                    //And halve the killVelocity
                    killVelocity = elementTypeData.killVelocity / 2;

                    //NAME
                    precipitateTypeName = "Dust";
                    break;

                case ElementPrecipitates.Drip:

                    //COLOR
                    elementRenderer.material.color = elementColor;
                    elementRenderer.material.SetFloat("_Glossiness", 1);

                    trailRend = GetComponent<TrailRenderer>();
                    trailRend.material.SetColor("_Emission", elementColor);
                    trailRend.startColor = elementColor;
                    trailRend.endColor = elementColor;

                    //Liquid Precipitates override despawn time to 3 seconds
                    canDespawn = true;
                    despawnTime = 3;

                    //And have unlimited killVelocity
                    killVelocity = Mathf.Infinity;

                    rb.drag = 5;
                    trailRend.emitting = true;
                    hideWhenContained = true;

                    //NAME
                    precipitateTypeName = "Drip";
                    break;
            }

            //NAME p2
            parentObject.name = $"{parentObject.name} {precipitateTypeName} t{System.Math.Round(Time.time, 1)}";

            Log($"Retrieved ElementPrecipitate Data from {elementTypeData.name}");
            return true;
        }

        public override void KillElement() { itemPooler.ReturnElementPrecipitate(parentObject); }

        public override void _AttemptDespawn()
        {
            //TODO: Before despawning, search the ground area and attempt to create a temporary container to exist in (pools of water)
            //The search can probably be a phat stationary spherecast, that looks for a "pool" of angled normals? Do spherecasts return *all* collided normals of a single object?

            if (lastLandTime == lastInteractTime)
            {
                Log("Timed out, returning to Item Pooler...");

                itemPooler.ReturnElementPrecipitate(parentObject);
            }
        }

        protected override void AdditionalTriggerEnter(Collider other)
        {
            if (hideWhenContained) trailRend.emitting = false;

            if (elementTypeData.elementHasUsableEffect && precipitateIsPrimed)
            {
                switch (elementTypeData.effectUseTrigger)
                {
                    case ElementUseTrigger.None:
                        Debug.LogError($"[{name}] Attempting to use potion with useTrigger set to None. How did this happened?!?!?!?");
                        break;
                    case ElementUseTrigger.Ingesting:
                        //If this element can be ingested, AttemptIngestElement if colliding with PlayerMouthTracker layer (Default)
                        if (other.gameObject.layer == 0) AttemptIngestElement(other.gameObject.GetComponent<PlayerMouthTracker>());
                        break;
                    case ElementUseTrigger.GroundingGrownObject:
                        //If this element triggers an action when grounded, AttemptGroundingTrigger if colliding with ObjectGrowingPlot layer (Ignore Raycast)
                        if (other.gameObject.layer == 2) AttemptSpawnGrownObject(other.gameObject);
                        break;
                }
            }
        }

        private void AttemptIngestElement(PlayerMouthTracker possibleMouthTracker)
        {
            if (possibleMouthTracker != null)
            {
                Log($"Collided with Player Mouth!");

                possibleMouthTracker.playerTracker.AttemptAddEffect(elementTypeData.ingestedEffect, elementTypeData.ingestedEffectStrength, elementTypeData.ingestedEffectDuration);

                //Kill self after being ingested
                HideThenDelayedKill();
            }
        }

        private void AttemptSpawnGrownObject(GameObject collidingObject)
        {
            ObjectGrowingPlot collidingSpawnerPlot = collidingObject.GetComponent<ObjectGrowingPlot>();

            if (collidingSpawnerPlot != null)
            {
                Log($"Collided with an Element Spawner Plot!");

                //Instantly return if Item Pooler doesn't have any Growable Links available
                switch (elementTypeData.grownObjectType)
                {
                    default:

                        LogWarning("Attempted to Spawn GrownObject with grownObjectType 0! How did this happen??");
                        break;
                    case GrownObjectType.ElementSpawner:

                        if (!itemPooler.ElementSpawnerAvailable()) return;
                        break;
                    case GrownObjectType.GrowableLink:

                        if (!itemPooler.GrowableLinkAvailable()) return;
                        break;
                }

                //The position the new ElementSpawner will lie is the Element's X/Z aligned with the SpawnerPlot's Y
                Vector3 seedPlotPos = new Vector3(parentObject.transform.position.x, collidingSpawnerPlot.transform.position.y, parentObject.transform.position.z);

                if (collidingSpawnerPlot.RoomToAddObject(seedPlotPos))
                {
                    ElementTypes elementTypeToUse = elementTypeData.grownObjectElement == 0 ? elementTypeId : elementTypeData.grownObjectElement;

                    //Add to spawnerPlot
                    collidingSpawnerPlot.AddNewGrownObject(seedPlotPos, elementTypeData.grownObjectType, elementTypeToUse);

                    //Kill Self after attempting to plant object
                    HideThenDelayedKill();
                }
            }
        }

        public void _VerifyDripType()
        {
            rb.isKinematic = true;
            rb.isKinematic = false;
        }

        public void HideThenDelayedKill()
        {
            elementRenderer.enabled = false;
            trailRend.emitting = false;
            rb.isKinematic = true;
            physicsCollider.enabled = false;

            SendCustomEventDelayedSeconds(nameof(KillElement), 2f);
        }
    }
}
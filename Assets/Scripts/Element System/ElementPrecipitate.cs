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
        [Header("Precipitate Type")]
        public ElementPrecipitates elementPrecipitateType;

        [Header("Momentum Scaling Settings")]
        public float minimumVelocity = 0;
        public float maximumVelocity = Mathf.Infinity;
        public float velocityMultiplier = 0.01f;
        public TrailRenderer trailRend;

        [Header("Grounding Effect Settings")]
        public bool hasUsableEffect;
        public bool precipitateIsPrimed;
        public ElementPrimingTrigger primingTrigger;
        public int primingThreshold;
        public ElementUseTrigger useTrigger;

        public PlayerEffect ingestedEffect;
        public int ingestedEffectStrength;
        public float ingestedEffectDuration;

        public GameObject GrownObjectPrefab;

        public Collider physicsCollider;

        public override void KillElement()
        {
            itemPooler.ReturnElementPrecipitate(parentObject);
        }

        protected override void AdditionalUpdate() { SimulateViscosity(); }

        protected override void AdditionalStart() { physicsCollider = GetComponent<Collider>(); }

        private void SimulateViscosity()
        {
            //TODO: isCollidingSurface is false if colliding when recently instantiated... object pooling will probably fix this

            //TODO: To remove the environment (isGrounded) restriction, get the angle of the ground's normal vs vector3.up and compare to an element-dependent threshold (ranged ~10-45 degrees)
            if (isCollidingSurface && !isGrounded && !isContained)
            {
                Log("Simulating viscosity...");
                if (rb.velocity.magnitude < minimumVelocity) rb.velocity *= 1 + velocityMultiplier;
                else if (rb.velocity.magnitude > maximumVelocity) rb.velocity *= 1 - velocityMultiplier;
            }

            /*
            if (!isGrounded && !isContained)
            {
                if (rb.velocity.magnitude < minimumVelocity) rb.velocity *= 1 + velocityMultiplier;
                else if (rb.velocity.magnitude > maximumVelocity) rb.velocity *= 1 - velocityMultiplier;
            }
            */
        }

        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            //ELEMENT DATA
            ElementData elementData = elementTypeManager.elementDataObjs[(int)elementTypeId];

            elementPrecipitateType = elementData.elementPrecipitateType;
            minimumVelocity = elementData.minimumVelocity;
            maximumVelocity = elementData.maximumVelocity;
            velocityMultiplier = elementData.velocityMultiplier;

            //ELEMENT-POTION DATA
            hasUsableEffect = elementData.elementHasUsableEffect;
            primingTrigger = elementData.elementEffectPrimingTrigger;
            primingThreshold = elementData.effectPrimingThreshold;
            useTrigger = elementData.effectUseTrigger;

            ingestedEffect = elementData.ingestedEffect;
            ingestedEffectStrength = elementData.ingestedEffectStrength;
            ingestedEffectDuration = elementData.ingestedEffectDuration;

            GrownObjectPrefab = elementData.GrownObjectPrefab;

            //PRECIPITATE TYPE
            string precipitateTypeName = "";

            switch (elementPrecipitateType)
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

                    //Dust Precipitates halve the killVelocity
                    killVelocity = killVelocity / 2;

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

                    //Liquid Precipitates have unlimited killVelocity
                    killVelocity = Mathf.Infinity;

                    //Liquids override despawn time to 3 seconds
                    despawnTime = 3;

                    rb.drag = 5;
                    trailRend.emitting = true;
                    hideWhenContained = true;

                    //NAME
                    precipitateTypeName = "Drip";
                    break;
            }

            //NAME p2
            parentObject.name = $"{parentObject.name} {precipitateTypeName}";

            Log($"Retrieved ElementPrecipitate Data from {elementData.name}");
            return true;
        }

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

            if (hasUsableEffect && precipitateIsPrimed)
            {
                switch (useTrigger)
                {
                    case ElementUseTrigger.None:
                        Debug.LogError($"[{name}] Attempting to use potion with useTrigger set to None. How did this happened?!?!?!?");
                        break;
                    case ElementUseTrigger.Ingesting:
                        //If this element can be ingested, AttemptIngestElement if colliding with PlayerMouthTracker layer (Default)
                        if (other.gameObject.layer == 0) AttemptIngestElement(other.gameObject.GetComponent<PlayerMouthTracker>());
                        break;
                    case ElementUseTrigger.Grounding:
                        //If this element triggers an action when grounded, AttemptGroundingTrigger if colliding with ObjectGrowingPlot layer (Ignore Raycast)
                        if (other.gameObject.layer == 2) AttemptGroundedTrigger(other.gameObject);
                        break;
                }
            }
        }

        private void AttemptIngestElement(PlayerMouthTracker possibleMouthTracker)
        {
            if (possibleMouthTracker != null)
            {
                Log($"Collided with Player Mouth!");

                possibleMouthTracker.playerTracker.AttemptAddEffect(ingestedEffect, ingestedEffectStrength, ingestedEffectDuration);

                //Kill self after being ingested
                HideThenDelayedKill();
            }
        }

        private void AttemptGroundedTrigger(GameObject collidingObject)
        {
            ObjectGrowingPlot collidingSpawnerPlot = collidingObject.GetComponent<ObjectGrowingPlot>();

            if (collidingSpawnerPlot != null)
            {
                Log($"Collided with an Element Spawner Plot!");

                //The position the new ElementSpawner will lie is the Element's X/Z aligned with the SpawnerPlot's Y
                Vector3 seedPlotPos = new Vector3(parentObject.transform.position.x, collidingSpawnerPlot.transform.position.y, parentObject.transform.position.z);

                if (collidingSpawnerPlot.RoomToAddObject(seedPlotPos))
                {
                    //TODO: When the GrownObjectPrefab is an ElementSpawner, we need AddNewObject to check the ElementType for an ElementSpawner, and spawn that instead of the GrownObjectPrefab if it exists

                    //Add to spawnerPlot
                    collidingSpawnerPlot.AddNewObject(GrownObjectPrefab, seedPlotPos);

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
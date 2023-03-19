using Argus.ItemSystem.Editor;
using Catacombs.Base;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.ElementSystem.Runtime
{
    //TODO: Add code so drips try to grow elements if canSpawnElements;

    public enum ElementPrecipitates
    {
        None,
        Dust = 1,
        Drip = 2
    }

    public class ElementPrecipitate : RuntimeElement
    {
        [Header("Precipitate Type")]
        public ElementPrecipitates elementPrecipitateType;

        [Header("Momentum Scaling Settings")]
        public float minimumVelocity = 0;
        public float maximumVelocity = Mathf.Infinity;
        public float velocityMultiplier = 0.01f;
        [SerializeField] private TrailRenderer trailRend;

        [Header("Potion Settings")]
        public bool isPotion;
        public bool precipitateIsPrimed;
        public ElementPrimingTrigger primingTrigger;
        public int primingThreshold;
        public ElementUseTrigger useTrigger;

        public PlayerEffect ingestedEffect;
        public int ingestedEffectStrength;
        public float ingestedEffectDuration;

        public GameObject GrownObjectPrefab;
        public float growTime;

        protected override void AdditionalStart()
        {
            if (elementPrecipitateType == ElementPrecipitates.Drip)
            {
                trailRend = GetComponent<TrailRenderer>();

                //Liquids override despawn time to 3 seconds
                despawnTime = 3;
            }
        }

        protected override void AdditionalUpdate() { SimulateViscosity(); }

        private void SimulateViscosity()
        {
            //TODO: isCollidingSurface is false if colliding when recently instantiated... object pooling will probably fix this

            /*
            if (isCollidingSurface)
            {
                //TODO: To remove the environment restriction, get the angle of the ground's normal vs vector3.up and compare to an element-dependent threshold (ranged ~10-45 degrees)
                if (isCollidingSurface && !isGrounded && !isContained)
                {
                    if (rb.velocity.magnitude < minimumVelocity) rb.velocity *= 1 + velocityMultiplier;
                    else if (rb.velocity.magnitude > maximumVelocity) rb.velocity *= 1 - velocityMultiplier;
                }
            }
            */
                //TODO: To remove the environment restriction, get the angle of the ground's normal vs vector3.up and compare to an element-dependent threshold (ranged ~10-45 degrees)

            if (!isGrounded && !isContained)
            {
                if (rb.velocity.magnitude < minimumVelocity) rb.velocity *= 1 + velocityMultiplier;
                else if (rb.velocity.magnitude > maximumVelocity) rb.velocity *= 1 - velocityMultiplier;
            }
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
            isPotion = elementData.elementIsPotion;
            primingTrigger = elementData.potionPrimingTrigger;
            primingThreshold = elementData.potionPrimingThreshold;
            useTrigger = elementData.potionUseTrigger;

            ingestedEffect = elementData.ingestedEffect;
            ingestedEffectStrength = elementData.ingestedEffectStrength;
            ingestedEffectDuration = elementData.ingestedEffectDuration;

            GrownObjectPrefab = elementData.GrownObjectPrefab;
            growTime = elementData.growTime;

            //COLOR
            base._PullElementType();

            trailRend.startColor = elementColor;
            trailRend.endColor = elementColor;

            string precipitateTypeName = "";

            switch (elementPrecipitateType)
            {
                case ElementPrecipitates.Dust:

                    float elementTintAmount = (float)System.Math.Round(Random.Range(0.1f, 0.65f), 1);
                    //Hopefully I'm just stealing 25% from Green and giving it back to the Element (dust was way too green)
                    float greenTintAmount = 0.75f - elementTintAmount;
                    elementTintAmount += 0.25f;

                    GetComponent<Renderer>().material.color = elementColor * elementTintAmount + Color.green * greenTintAmount;

                    precipitateTypeName = "Dust";
                    break;

                case ElementPrecipitates.Drip:

                    GetComponent<Renderer>().material.color = elementColor;
                    trailRend.material.SetColor("_Emission", elementColor);

                    precipitateTypeName = "Drip";
                    break;
            }

            //NAME
            parentObject.name = $"{parentObject.name} {precipitateTypeName}";

            Log($"Retrieved ElementPrecipitate Data from {elementData.name}");
            return true;
        }

        protected override void AdditionalTriggerEnter(Collider other)
        {
            if (hideWhenContained) trailRend.emitting = false;

            if (isPotion && precipitateIsPrimed)
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
                    //Setup GrownObjectPrefab instance
                    GrownObject newGrownObject = (GrownObject)Instantiate(GrownObjectPrefab).GetComponent<ElementInteractionHandler>().childElement;
                    newGrownObject.elementTypeId = elementTypeId;
                    newGrownObject.elementTypeManager = elementTypeManager;

                    //Place in ground
                    newGrownObject.transform.parent.SetPositionAndRotation(seedPlotPos, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
                    newGrownObject.transform.parent.SetParent(collidingSpawnerPlot.transform, true);

                    //Add to spawnerPlot
                    collidingSpawnerPlot.AddObject(newGrownObject);

                    //Kill Self after attempting to plant object
                    HideThenDelayedKill();
                }
            }
        }

        protected override void AdditionalTriggerExit(Collider other) { if (hideWhenContained) trailRend.emitting = true; }

        public override void _AttemptDespawn()
        {
            base._AttemptDespawn();

            //TODO: Before despawning, search the ground area and attempt to create a temporary container to exist in (pools of water)
            //The search can probably be a phat stationary spherecast, that looks for a "pool" of angled normals? Do spherecasts return *all* collided normals of a single object?
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

            SendCustomEventDelayedSeconds(nameof(_DelayedKill), 1);
        }
    }
}
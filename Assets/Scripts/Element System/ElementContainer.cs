using System;
using UdonSharp;
using UnityEngine;
using Argus.ItemSystem;
using Argus.ItemSystem.Editor;
using System.ComponentModel;
using VRC.SDK3.Components;

namespace Catacombs.ElementSystem.Runtime
{
    //Element Containers are any objects that will be able to hold BaseElements & ElementPrecipitates in them (if marked Containable)
    //Current Containables: Berries, Dusts, Liquids
    public class ElementContainer : UdonSharpBehaviour
    {
        [SerializeField] private ItemPooler itemPooler;
        [SerializeField] private ElementTypeManager elementTypeManager;

        [Header("Container Properties")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private VRCPickup pickupScript;
        [SerializeField] private Renderer containerRend;
        public GameObject containerSpout;
        private MeshFilter containerSpoutFilter;
        [Tooltip("The max amount of liquid this Container can store; Usable Potion Recipes are limited to lists of this length")]
        [SerializeField] private int maxLiquidParts = 3;
        [SerializeField] private int curLiquidParts = 0;
        private int dustParts = 0;
        private int totalElementColors = 1;
        private Color curLiquidColor;

        [SerializeField] private float timeToPour = 0.5f;
        private bool canPour;
        private float pourTimer;


        [Header("Water Material Clipping Plane")]
        [SerializeField] private Renderer liquidClippingPlane;
        public Vector3 planeNormal;
        public Vector3 planePosition;
        [SerializeField] private float fxMaxHeight;
        [SerializeField] private float fxMinHeight;

        [SerializeField] private int curHeightInterval;

        private float startFxHeight;
        [SerializeField] private int lerpDirection;
        private float lerpStartTime;


        [Header("Potion Forming")]
        [SerializeField] private bool canCreatePotions = true;
        [SerializeField] private ElementTypes[] containedLiquids;
        [SerializeField] private ElementTypes[] containedElements;
        public ElementPrecipitate[] containedDusts = new ElementPrecipitate[10];


        [Header("Potion Priming/Use")]
        [SerializeField] private ParticleSystem elementPrimedParticles;
        [SerializeField] private Vector2 primedParticlesStartScale;
        [SerializeField] private Vector2 primedParticlesTopScale;
        [SerializeField] private ElementPrimingTrigger elementUsePrimingTrigger;

        [Tooltip("How Primed the Potion is; Dependent on PrimingTrigger type")]
        [SerializeField, Range(0, 100)] private float elementPrimedAmount;

        [Tooltip("How much primedAmount is required before the potion actually Primes")]
        [SerializeField, Range(0, 100)] private int elementPrimedThreshold = 100;
        [SerializeField] private bool liquidElementIsPrimed;
        [SerializeField] private ElementUseTrigger elementUseTrigger;
        [SerializeField] private bool containerIsOpenEnded;
        [SerializeField] private float deprimingSpeed = 0.2f;
        private Vector3 collidingPestlePos;
        private bool collidingPestleIsMixing;

        void Start()
        {
            if (containerRend == null) containerRend = GetComponent<Renderer>();
            if (containerSpout != null) containerSpoutFilter = containerSpout.GetComponent<MeshFilter>();

            containedLiquids = new ElementTypes[maxLiquidParts];
            containedElements = new ElementTypes[maxLiquidParts];

            Transform planeTransform = liquidClippingPlane.transform.parent;
            planeTransform.localPosition = new Vector3(0, -1, 0);

            liquidClippingPlane.material.SetInt("_StencilMask", containerRend.material.GetInt("_StencilMask"));

            if (rb == null) rb = transform.parent.GetComponent<Rigidbody>();

            UpdateShaderProperties();
        }

        private void Update()
        {
            //Update shader properties if object is moving (does this mean I don't need to check pickup anymore?)
            if (rb.velocity.magnitude >= 0.1f || pickupScript.IsHeld) UpdateShaderProperties();

            if (curLiquidParts > 0)
            {
                //TODO: R&D a threshold multiplier for "straight up" dependent on its curLiquidAmount so it only checks if it knows it has enough to pour compared to its angle

                //If container isn't pointing straight up, check to see if it can pour & animate liquid plane
                if (Vector3.Angle(Vector3.up, transform.up) != 0)
                {
                    CheckPouring();
                    UpdateShaderProperties();
                }
            }

            switch (lerpDirection)
            {
                case -1: LerpFxDown(); break;

                case 0: break;

                case 1: LerpFxUp(); break;
            }

            MonitorPriming();
        }

        private void LerpFxUp()
        {
            float targetPos = fxMinHeight + (fxMaxHeight - fxMinHeight) / maxLiquidParts * (curHeightInterval + lerpDirection);
            liquidClippingPlane.transform.parent.localPosition = new Vector3(0, Mathf.SmoothStep(startFxHeight, targetPos, (Time.time - lerpStartTime) * 0.7f), 0);

            UpdateShaderProperties();

            //return and continue lerping if clippingPlane has yet to reach targetPos
            if (liquidClippingPlane.transform.parent.localPosition.y != targetPos) return;
            
            curHeightInterval += lerpDirection;

            //If we have any containedDusts and the Container is full enough...
            if (containedDusts[0] != null && curHeightInterval >= (float)maxLiquidParts / 3)
            {
                //For every dust in containedDusts, Attempt to add to Recipes list
                for (int i = 0; i < containedDusts.Length; i++)
                {
                    if (containedDusts[i] != null)
                    {
                        Log($"Consuming containedDust [{containedDusts[i].name}]");
                        AttemptConsumeDust(containedDusts[i]);
                        containedDusts[i] = null;
                    }
                }
            }

            //return and continue lerping if Container has another interval left to fill
            if (curHeightInterval < curLiquidParts) return;

            lerpDirection = 0;

            //Resize primedParticles' emission scale to match the heightInterval
            var shape = elementPrimedParticles.shape;
            shape.scale = primedParticlesStartScale + (primedParticlesTopScale - primedParticlesStartScale) / maxLiquidParts * (curHeightInterval + lerpDirection);
        }

        private void LerpFxDown()
        {
            //float startHeight = liquidClippingPlane.transform.parent.localPosition.y;
            float targetPos;

            //If we're lerping to 0 override targetPos to -1.5 so liquid visually completely drains from a tilted Container
            if (curHeightInterval + lerpDirection == 0) targetPos = -1.5f;
            //Else lerp to the next heightInterval down
            else targetPos = fxMinHeight + (fxMaxHeight - fxMinHeight) / maxLiquidParts * (curHeightInterval + lerpDirection);

            liquidClippingPlane.transform.parent.localPosition = new Vector3(0, Mathf.SmoothStep(startFxHeight, targetPos, (Time.time - lerpStartTime) * 0.7f), 0);

            UpdateShaderProperties();

            //return and continue lerping if clippingPlane has yet to reach targetPos
            if (liquidClippingPlane.transform.parent.localPosition.y != targetPos) return;
            
            curHeightInterval += lerpDirection;

            //return and continue lerping if Container has another interval left to drain
            if (curHeightInterval > curLiquidParts) return;

            lerpDirection = 0;

            UpdateShaderProperties();

            //Resize primedParticles' emission scale to match the heightInterval
            var shape = elementPrimedParticles.shape;
            shape.scale = primedParticlesStartScale + (primedParticlesTopScale - primedParticlesStartScale) / maxLiquidParts * (curHeightInterval + lerpDirection);
        }

        private void MonitorPriming()
        {
            //Elements CANNOT be primed if any Dusts have been added
            if (containedElements[0] != ElementTypes.None)
            {
                elementPrimedAmount = 0;
                return;
            }

            switch (elementUsePrimingTrigger)
            {
                case ElementPrimingTrigger.None:
                    elementPrimedAmount = -1;
                    break;

                case ElementPrimingTrigger.Always:
                    elementPrimedAmount = 100;
                    break;

                case ElementPrimingTrigger.Mixing:

                    if (containerIsOpenEnded)
                    {
                        if (collidingPestleIsMixing)
                        {
                            if (elementPrimedAmount < 100) elementPrimedAmount += 0.5f;
                        }
                        else
                        {
                            if (elementPrimedAmount > 0) elementPrimedAmount -= deprimingSpeed / 4;
                        }
                    }
                    else
                    {
                        if (rb.velocity.magnitude > 0.1f)
                        {
                            if (elementPrimedAmount < 100) elementPrimedAmount += 0.5f;
                        }
                        else
                        {
                            if (elementPrimedAmount > 0) elementPrimedAmount -= deprimingSpeed / 8;
                        }
                    }
                    break;
            }

            if (liquidElementIsPrimed)
            {
                if (elementPrimedAmount < elementPrimedThreshold)
                {
                    liquidElementIsPrimed = false;
                    elementPrimedParticles.Stop();
                }
            }
            else
            {
                if (elementPrimedAmount >= elementPrimedThreshold)
                {
                    liquidElementIsPrimed = true;
                    elementPrimedParticles.Play();
                    SendCustomEventDelayedSeconds(nameof(_TimeoutPriming), 15);
                }
            }
        }

        //TODO: ClippingPlane position visuals gradually worsen and get really ugly once you've tilted > ~90 degrees
        #region LIQUID POURING
        private void UpdateShaderProperties()
        {
            //If we are not lerping (lerpDirection == 0) and have no liquid (curHeightInterval == 0), always keep clippingPlane in an unrenderable position
            if (lerpDirection == 0 && curHeightInterval == 0) liquidClippingPlane.transform.parent.position = new Vector3(0, -5, 0);

            liquidClippingPlane.transform.rotation = Quaternion.Euler(new Vector3(90, liquidClippingPlane.transform.rotation.y, 0));

            containerRend.material.SetVector("_PlaneNormal", liquidClippingPlane.transform.TransformVector(new Vector3(0, 0, -1)));
            containerRend.material.SetVector("_PlanePosition", liquidClippingPlane.transform.position);
        }

        private void CheckPouring()
        {
            //Starting at maxLiquidParts, count down for every curLiquidParts
            for (int i = maxLiquidParts; i > maxLiquidParts - curLiquidParts; i--)
            {
                //If the angle of the container vs Up is greater than curLiquidParts of maxLiquidParts * 80 (a percentage of 80° based on fullness), container can pour!
                if (Math.Round(Vector3.Angle(Vector3.up, transform.up), 3) >= Math.Round(1f / maxLiquidParts * i * 80, 3) && curLiquidParts > 0)
                {
                    canPour = true;
                    break;
                }
                else canPour = false;
            }

            if (canPour)
            {
                pourTimer += Time.deltaTime;
                if (pourTimer > timeToPour)
                {
                    Log($"Container has tipped enough to pour!");
                    pourTimer = 0;

                    PourLiquid();
                }
            }
            else pourTimer = 0;
        }

        private void PourLiquid()
        {
            ElementPrecipitate newPrecipitate = itemPooler.RequestElementPrecipitate();

            if (newPrecipitate != null)
            {
                curLiquidParts--;

                //lowestVert, the literal lowest vertex of the spout mesh, indicates which way the container is tilted
                Vector3 lowestVert = new Vector3(0, Mathf.Infinity, 0);

                foreach (Vector3 vert in containerSpoutFilter.mesh.vertices)
                {
                    Vector3 vertGlobal = transform.TransformPoint(vert);
                    if (vertGlobal.y < lowestVert.y) lowestVert = vertGlobal;
                }

                //After getting lowest vert in Mesh Filter the actual obj instance's transform must be manually added
                lowestVert += containerSpoutFilter.transform.localPosition;

                newPrecipitate.transform.position = lowestVert;
                newPrecipitate.transform.parent = null;
                newPrecipitate.trailRend.emitting = true;

                newPrecipitate.elementTypeId = containedLiquids[0];

                newPrecipitate.rb.isKinematic = false;

                newPrecipitate._PullElementType();

                if (liquidElementIsPrimed) newPrecipitate.precipitateIsPrimed = true;

                //Init lerping
                startFxHeight = liquidClippingPlane.transform.parent.localPosition.y;

                if (lerpDirection == 0)
                {
                    lerpDirection = -1;
                    lerpStartTime = Time.time;
                }

                //If we don't have any liquid left, reset all Element Priming Data
                if (curLiquidParts == 0) ResetElementPrimingData();

                //Remove the top liquid element of containedLiquids
                for (int i = containedLiquids.Length - 1; i >= 0; i--)
                {
                    if (containedLiquids[i] != ElementTypes.None)
                    {
                        containedLiquids[i] = ElementTypes.None;
                        break;
                    }
                }
            }
        }
        #endregion LIQUID POURING

        #region CONTAINING PRECIPITATE
        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.gameObject == null) return;

            //If on Element layer
            if (other.gameObject.layer == 24)
            {
                ElementPrecipitate elementPrecipitate = other.GetComponent<ElementPrecipitate>();
                if (elementPrecipitate != null)
                {
                    switch (elementPrecipitate.elementPrecipitateType)
                    {
                        case ElementPrecipitates.None:
                            Debug.LogWarning($"[{name}] Attempted to contain Precipitate [{other.gameObject.name}] with Drip type None; Verifying Drip Type...");
                            elementPrecipitate._VerifyDripType(); //SendCustomEventDelayedFrames(nameof(elementPrecipitate._VerifyDripType), 1);
                            break;

                        case ElementPrecipitates.Drip:
                            AttemptContainLiquid(elementPrecipitate);
                            break;

                        case ElementPrecipitates.Dust:
                            AttemptConsumeDust(elementPrecipitate);
                            break;
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null || other.gameObject == null) return;

            //If on Pestle layer
            if (other.gameObject.layer == 23) collidingPestleIsMixing = false;

            //If on Element layer
            if (other.gameObject.layer == 24)
            {
                ElementPrecipitate elementPrecipitate = other.GetComponent<ElementPrecipitate>();

                //If precipitate exists and is dust, remove from containedDusts
                if (elementPrecipitate != null && elementPrecipitate.elementPrecipitateType == ElementPrecipitates.Dust)
                {
                    for (int i = 0; i < containedDusts.Length; i++)
                    {
                        if (containedDusts[i] == elementPrecipitate)
                        {
                            containedDusts[i] = null;
                            break;
                        }
                    }
                }
            }
        }

        public void AttemptContainLiquid(ElementPrecipitate elementDrip)
        {
            if (curLiquidParts < maxLiquidParts)
            {
                //If we already contain at least one liquid Element, don't add the new liquid if it isn't the same ElementType
                if (containedLiquids[0] != ElementTypes.None && elementDrip.elementTypeId != containedLiquids[0]) return;

                liquidClippingPlane.gameObject.SetActive(true);

                curLiquidParts++;

                //Add liquid's ElementType to containedLiquids
                for (int i = 0; i < containedLiquids.Length; i++)
                {
                    if (containedLiquids[i] == ElementTypes.None)
                    {
                        containedLiquids[i] = elementDrip.elementTypeId;

                        //Is this Container's first containedLiquid?
                        if (i == 0)
                        {
                            SetupLiquidElementPotionInfo(elementTypeManager.elementDataObjs[(int)elementDrip.elementTypeId]);

                            //Reset position of the liquidClippingPlane
                            liquidClippingPlane.transform.parent.localPosition = new Vector3(0, fxMinHeight, 0);
                        }
                        break;
                    }
                }

                //Init lerping
                startFxHeight = liquidClippingPlane.transform.parent.localPosition.y;

                if (lerpDirection == 0)
                {
                    lerpDirection = 1;
                    lerpStartTime = Time.time;
                }

                if (canCreatePotions) CheckRecipes();
            }

            //TODO: Keep liquid drip object, disabled and hidden, and dump it back out in DrainLiquid()?
            //ALWAYS destroy liquids that enter containers, even if already full
            elementDrip.HideThenDelayedKill();
        }

        private void SetupLiquidElementPotionInfo(ElementData elementData)
        {
            //Set the liquid renderers' colors to element color
            curLiquidColor = elementData.elementColor;
            containerRend.material.SetColor("_CrossColor", curLiquidColor);
            containerRend.material.SetColor("_Color", curLiquidColor);
            liquidClippingPlane.material.color = curLiquidColor;

            //Verify if element can be Used
            if (elementData.elementHasUsableEffect)
            {
                elementUsePrimingTrigger = elementData.elementEffectPrimingTrigger;
                elementPrimedThreshold = elementData.effectPrimingThreshold;
                elementUseTrigger = elementData.effectUseTrigger;
            }
        }

        public void AttemptConsumeDust(ElementPrecipitate elementDust)
        {
            //If the container is not full of enough liquid, add dust to a list for later and Return
            if (curHeightInterval < (float)maxLiquidParts / 3)
            {
                for (int i = 0; i < containedDusts.Length; i++)
                {
                    if (containedDusts[i] == elementDust) break;
                    else if (containedDusts[i] == null)
                    {
                        containedDusts[i] = elementDust;
                        break;
                    }
                }

                return;
            }

            //If there aren't already more Dust ElementIDs than the max, add to the recipe list
            if (dustParts < maxLiquidParts)
            {
                dustParts++; totalElementColors++;

                for (int i = 0; i < containedLiquids.Length; i++)
                {
                    if (containedElements[i] == ElementTypes.None)
                    {
                        containedElements[i] = elementDust.elementTypeId;

                        //TODO: lerp color!

                        //Each time an Element is added to the recipe list, set color to all Element Colors divided by number of Element Dust added
                        curLiquidColor += elementDust.elementColor;
                        Color averagedLiquidColor = curLiquidColor / totalElementColors;

                        containerRend.material.SetColor("_CrossColor", averagedLiquidColor);
                        containerRend.material.SetColor("_Color", averagedLiquidColor);
                        liquidClippingPlane.material.color = averagedLiquidColor;

                        if (canCreatePotions) CheckRecipes();

                        break;
                    }
                }
            }

            //A full enough container will ALWAYS kill ElementPrecipitates, even if it wasn't accepted as an ingredient!
            itemPooler.ReturnElementPrecipitate(elementDust.parentObject);
        }
        #endregion

        //Loop through elementTypeManager, comparing all elements in containedElements & containedLiquids against potionRecipeDataObjs[] to see if the current combination is a Potion Recipe!
        //If so, loop through the element lists, deleting all saved Dust elements, and converting the Liquid to the new type!
        //Then play an animation to indicate a Potion has formed, and check for Priming if necessary before checking for the Effect Trigger.
        #region POTION DATA
        private void CheckRecipes()
        {
            //Start at 1 to align with ElementTypes Enum (0 == ElementTypes.None)
            for (int i = 1; i < elementTypeManager.potionRecipeObjs.Length; i++)
            {
                //For each PotionRecipeData object, compare the Container's current liquid type, then loop through the other ingredients to verify if they match!
                PotionRecipeData potionRecipeData = elementTypeManager.potionRecipeObjs[i];
                ElementTypes[] existingElements = (ElementTypes[]) containedElements.Clone();
                ElementTypes[] existingLiquids = (ElementTypes[]) containedLiquids.Clone();
                ElementTypes[] potionRecipe = (ElementTypes[]) potionRecipeData.requiredElementTypes.Clone();

                int foundElements = 0;

                //If the recipe doesn't specify a liquid, search only for dusts:
                if (potionRecipeData.requiredLiquidType == ElementTypes.None)
                {
                    //For every element in existingElements[], check if exists in potionRecipe[], then remove elements & increment foundElements to verify ingredient is present
                    for (int j = 0; j < existingElements.Length; j++)
                    {
                        for (int k = 0; k < potionRecipe.Length; k++)
                        {
                            //Compare each object in existingElements vs potionRecipe, then remove equal elements from both as they are found!
                            if (existingElements[j] != 0 && potionRecipe[k] != 0 && existingElements[j] == potionRecipe[k])
                            {
                                existingElements[j] = 0;
                                potionRecipe[k] = 0;
                                foundElements++;
                            }
                        }
                    }
                }
                //Otherwise compare with both containedLiquids' ElementType and all containedElement ElementTypes
                else if (potionRecipeData.requiredLiquidType == containedLiquids[0])
                {
                    //For every element in existingElements[], check if exists in potionRecipe[], then remove elements & increment foundElements to verify ingredient is present
                    for (int j = 0; j < existingElements.Length; j++)
                    {
                        for (int k = 0; k < potionRecipe.Length; k++)
                        {
                            if (potionRecipe[k] != 0)
                            {
                                if (existingElements[j] != 0)
                                {
                                    //Compare each object in existingElements vs potionRecipe, then remove equal elements from both as they are found!
                                    if (existingElements[j] == potionRecipe[k])
                                    {
                                        existingElements[j] = 0;
                                        potionRecipe[k] = 0;
                                        foundElements++;
                                    }
                                }
                                else if (existingLiquids[j] != 0)
                                {
                                    //Search in existingLiquids too since there's a requiredLiquidType
                                    if (existingLiquids[j] == potionRecipe[k])
                                    {
                                        existingLiquids[j] = 0;
                                        potionRecipe[k] = 0;
                                        foundElements++;
                                    }
                                }
                            }
                        }
                    }
                }

                //If we successfully found the total number of Elements the Potion Recipe contains, create the new Potion!
                if (foundElements == potionRecipe.Length) CreatePotion(potionRecipeData);
                //else Log($"Failed to satisfy Potion Recipe, found {foundElements} successful elements, needed {potionRecipe.Length}");
            }
        }

        private void CreatePotion(PotionRecipeData potionRecipeData)
        {
            /*
            //If no liquidType was specified, overwrite potionColor with current liquid's color (so flexible recipes' craft methods affect additional colors of some items!)
            if (potionRecipeData.requiredLiquidType == ElementTypes.None) potionRecipeData.potionColor = elementTypeManager.elementDataObjs[(int)potionRecipeData.potionElementType].elementColor;
            */

            ElementData elementData = elementTypeManager.elementDataObjs[(int)potionRecipeData.potionElementType];

            //Erase all dusts & convert liquid to Potion's Element Type (and color!)
            for (int i = 0; i < containedLiquids.Length; i++)
            {
                containedLiquids[i] = potionRecipeData.potionElementType;
                containedElements[i] = ElementTypes.None;
            }

            SetupLiquidElementPotionInfo(elementData);

            containerRend.material.SetColor("_CrossColor", potionRecipeData.potionColor);
            containerRend.material.SetColor("_Color", potionRecipeData.potionColor);

            Log($"Successfully created {potionRecipeData.name}!");
        }

        private void ResetElementPrimingData()
        {
            Log("Resetting Container's Element Priming Data");

            elementUsePrimingTrigger = ElementPrimingTrigger.None;
            elementPrimedThreshold = 100;
            elementPrimedAmount = 0;
            elementUseTrigger = ElementUseTrigger.None;
            elementPrimedAmount = 0;
            liquidElementIsPrimed = false;

            elementPrimedParticles.Stop();
        }

        private void OnTriggerStay(Collider other)
        {
            if (other == null || other.gameObject == null) return;

            //If on Pestle layer
            if (other.gameObject.layer == 23)
            {
                if (elementUsePrimingTrigger == ElementPrimingTrigger.Mixing && containerIsOpenEnded)
                {
                    Vector3 curPestlePos = other.transform.position;

                    collidingPestleIsMixing = curPestlePos != collidingPestlePos;

                    /*
                    //If collided Pestle is actively moving
                    if (curPestlePos != collidingPestlePos)
                    {
                        if (elementPrimedAmount < 100) elementPrimedAmount += 0.5f;
                        collidingPestleIsMixing = true;
                    }
                    else if (elementPrimedAmount > 0) elementPrimedAmount -= deprimingSpeed / 4;
                    */

                    collidingPestlePos = curPestlePos;
                }
            }
        }

        public void _TimeoutPriming()
        {
            liquidElementIsPrimed = false;
            elementPrimedAmount = 0;
            elementPrimedParticles.Stop();
        }

        #endregion

        private void Log(string message)
        {
            Debug.Log($"[{name}] {message}", this);
        }
    }
}
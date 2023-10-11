using UnityEngine;
using Catacombs.Base;
using Catacombs.ElementSystem.Runtime;
using UnityEditor.Animations;
using UnityEditor;
using Boo.Lang;
using UnityEngine.EventSystems;
using UdonSharp;
using System;
using System.Linq;

namespace Argus.ItemSystem.Editor
{
    public enum ElementTypes
    {
        None = 0,
        Linkberry = 1,
        Arieberry = 2,
        Blueberry = 3,
        Water = 4,
        Oil = 5,
        BlueberryJuice = 6,
        Linktorch = 7,
        Clover = 8,
        CloverSeed = 9,
        CloverSprout = 10,
        TestElement = 11
    }

    public enum ElementPrecipitates
    {
        None,
        Dust = 1,
        Drip = 2
    }

    public enum GrownObjectType
    {
        None,
        ElementSpawner = 1,
        GrowableLink = 2
    }

    public enum ElementUseTrigger
    {
        None = 0,
        Ingesting = 1,
        GroundingGrownObject = 2
    }

    public enum ElementPrimingTrigger
    {
        None = 0,
        Always = 1,
        Mixing = 2,
        Heating = 3,
        Lighting = 4
    }

    [CreateAssetMenu(fileName = "New Element Type", menuName = "Catacombs/ElementType")]
    public class ElementType : ScriptableObject
    {
        [Header("Element Info")]
        public ElementTypes elementTypeId;
        public Color elementColor;
        [Tooltip("Automatic: True if a PotionRecipe points to this ElementType")]
        public bool elementIsProducedByPotion;
        [Tooltip("Automatic: Whether or not this Element ever spawns from an Element Spawner")]
        public bool elementIsSpawnedByElementSpawner;
        [Tooltip("Automatic: Whether or not this Element ever spawns as a 'Seed Pod'")]
        public bool elementIsSeedPod;
        [Tooltip("Automatic: Whether or not this Element is capable of spawning a GrownObject in some way")]
        public bool elementCanSpawnGrownObject;


        [Header("Despawn Settings")]
        [Tooltip("Whether or not instances of this Element can despawn over time or on collision")]
        public bool canDespawn;
        [Tooltip("The amount of time to wait before killing instances of this Element due to inactivity")]
        [Min(2)] public int despawnTime = 30;
        [Tooltip("The maximum velocity this instances of this Element may impact any collider before dying")]
        public float killVelocity = Mathf.Infinity;


        [Header("BaseElement Settings")]
        [Tooltip("The time (in seconds) it takes each of this Element Spawner's BaseElement Objects to grow")]
        public int baseElementSpawnTime;
        [Tooltip("The visual mesh to use for the BaseElement")]
        public Mesh baseElementMesh;
        [Tooltip("A prefab containing the colliders for the BaseElement")]
        public GameObject BaseElementCollisionPrefab;
        [Tooltip("The Radius of the BaseElement elementInteractionHandler's Pickup Collider")]
        public float pickupColliderRadius;
        [Tooltip("The Mass of the BaseElement elementInteractionHandler's Rigidbody")]
        public float rbMass = 1f;
        [Tooltip("The Drag of the BaseElement elementInteractionHandler's Rigidbody")]
        public float rbDrag;
        [Tooltip("The AngularDrag of the BaseElement elementInteractionHandler's Rigidbody")]
        public float rbAngularDrag = 0.05f;


        [Header("Seed Pod Settings")]
        [Tooltip("Whether or not this Element can turn into a Seed Pod BaseElement when grown from its Element Spawner")]
        public bool elementCanBecomeSeedPod;
        [Tooltip("The chances of a grown BaseElement turning into a Seed Pod")]
        [Range(0, 100)] public float seedPodSpawnChance;
        [Tooltip("Which SeedPod Prefab to spawn when a BaseElement turns into a Seed Pod")]
        public GameObject SeedPodPrefab;
        [Tooltip("The local position the SeedPod Prefab should move to")]
        public Vector3 seedPodPosOffset;
        [Tooltip("The ElementType for the Seed Pod variant (Optional: Leave None if ElementType isn't different)")]
        public ElementTypes seedPodElementType;
        [Tooltip("What prefab to spawn if BaseElement isn't a SeedPod (Optional: leave null if none exists)")]
        public GameObject ElementLeavesPrefab;
        [Tooltip("Whether or not the Seed Pod can be planted in ObjectGrowingPlots by hand")]
        public bool canPlantManually;


        [Header("Grown Object Settings")]
        [Tooltip("The AnimatorController for the GrownObject this Element spawns/spawns from")]
        public AnimatorController GrownObjectAnimator;
        [Tooltip("Which script to apply to the GrownObject when spawned")]
        public GrownObjectType grownObjectType;
        [Tooltip("The ElementType to apply to the the GrownObject when spawned")]
        public ElementTypes grownObjectElement;
        [Tooltip("The Y Position of the GrownObject elementInteractionHandler's Pickup Collider")]
        public float colliderYPos;
        [Tooltip("The radius of the GrownObject elementInteractionHandler's Pickup Collider")]
        public float colliderRadius = 0.05f;
        [Tooltip("The height of the GrownObject elementInteractionHandler's Pickup Collider")]
        public float colliderHeight = 0.5f;
        [Tooltip("A list of prefabs for each stage of growth (including complete) the GrownObject has")]
        public GameObject[] GrownObjectGrowthPrefabs;
        [Tooltip("The time (in seconds) it takes the GrownObject to mature")]
        public int grownObjectGrowTime;
        [Tooltip("A Prefab containing all BaseElement spawn positions (Optional: leave null if GrownObject is not an ElementSpawner)")]
        public GameObject ElementSpawnTransforms;


        //Precipitate (verb): To cause drops of moisture or particles of dust to be deposited from the atmosphere or from a vapor or suspension (in this case the BaseElement)
        //Precipitate (noun): A substance precipitated from a solution (again, kinda BaseElement)
        [Header("Precipitate Settings")]
        [Tooltip("Whether or not this Element can spawn as a Precipitate in some way")]
        public bool elementHasPrecipitateForm;
        [Tooltip("Is the Precipitate in solid (Dust) or liquid (Drip) form?")]
        public ElementPrecipitates elementPrecipitateType;
        [Tooltip("The minimum velocity the Element Precipitate obj may have before being sped up")]
        public float minimumVelocity = 0;
        [Tooltip("The maximum velocity the Element Precipitate obj may have before being slowed down")]
        public float maximumVelocity = Mathf.Infinity;
        [Tooltip("The %rate at which the Element Precipitate obj's velocity is modified")]
        [Range(0, 0.5f)] public float velocityMultiplier = 0.01f;


        [Header("BaseElement Precipitation Settings")]
        [Tooltip("The rate at which scale is subtracted when colliding with a moving Pestle")]
        public float shrinkSpeed = 0.001f;
        [Tooltip("The total amount of Precipitate objects to attempt to produce before being destroyed")]
        public int elementPrecipitateAmount = 3;
        [Tooltip("The chances of producing a Precipitate object each time")]
        [Range(0, 1)] public float elementPrecipitateSpawnChance = 1;


        [Header("Effect Settings")]
        [Tooltip("Whether or not this Element can be used to perform effects")]
        public bool elementHasUsableEffect;
        [Tooltip("The Trigger required to Prime this Element's Effect")]
        public ElementPrimingTrigger elementEffectPrimingTrigger;
        [Tooltip("The required intensity to activate the PrimingTrigger (dependent on Trigger type)")]
        [Range(0, 100)] public int effectPrimingThreshold = 100;
        [Tooltip("The Trigger required to cause this Element's effect")]
        public ElementUseTrigger effectUseTrigger;
        [Tooltip("The effect to apply when ingested")]
        public PlayerEffect ingestedEffect;
        [Tooltip("The strength to multiply the ingested effect by")]
        public int ingestedEffectStrength;
        [Tooltip("The time (in seconds) before the ingested effect wears off")]
        public float ingestedEffectDuration;
    }

    [CustomEditor(typeof(ElementType))]
    public class ElementTypeEditor : UnityEditor.Editor
    {
        private ElementTypeManager elementTypeManager;

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Regenerate Element Types", EditorStyles.toolbarButton)) ItemEditorUtility.RefreshElementTypes();

            if (elementTypeManager == null) elementTypeManager = FindObjectOfType<ElementTypeManager>();

            var elementTypes = ItemEditorUtility.GetAllElementTypes().OrderBy(o => o.elementTypeId).ToList();

            serializedObject.Update();

            List<string> excludedProperties = new List<string>();

            ElementType myBehaviour = target as ElementType;

            //Header: 'Element Info'
            bool isProducedByPotion = false;

             //Loop through PotionRecipeData objects and check if any of their resulting ElementTypes are this Element's
            for (int i = 1; i < elementTypeManager.potionRecipeObjs.Length; i++)
            {
                if (myBehaviour.elementTypeId == elementTypeManager.potionRecipeObjs[i].potionElementType)
                {
                    isProducedByPotion = true;
                    break;
                }
            }
            myBehaviour.elementIsProducedByPotion = isProducedByPotion;
            if (myBehaviour.elementIsProducedByPotion) myBehaviour.elementHasPrecipitateForm = true;

             //Loop through ElementData objects to find any SeedPod & GrownObject references to this Element
            bool isSeedPod = false;
            bool isSpawnedByElementSpawner = false;
            for (int i = 1; i < elementTypes.Count; i++)
            {
                //If elementTypeID exists as an elementData's seedPodElementType 
                if (myBehaviour.elementTypeId == elementTypes[i].seedPodElementType)
                {
                    isSeedPod = true;
                    isSpawnedByElementSpawner = true;
                    break;
                }

                //If elementData hasUsableEffect & canSpawnGrownObject
                if (elementTypes[i].elementHasUsableEffect && elementTypes[i].elementCanSpawnGrownObject)
                {
                    //If elementData's grownObjectType is ElementSpawner & grownObjectElement is elementTypeID 
                    if (elementTypes[i].grownObjectType == GrownObjectType.ElementSpawner)
                    {
                        if (myBehaviour.elementTypeId == elementTypes[i].grownObjectElement)
                        {
                            isSpawnedByElementSpawner = true;
                            Debug.Log("Test!");
                            break;
                        }
                    }
                }
            }
            myBehaviour.elementIsSeedPod = isSeedPod;
            myBehaviour.elementIsSpawnedByElementSpawner = isSpawnedByElementSpawner;

            myBehaviour.elementCanSpawnGrownObject = myBehaviour.canPlantManually || (myBehaviour.elementHasUsableEffect && myBehaviour.effectUseTrigger == ElementUseTrigger.GroundingGrownObject);

            //Header: 'Despawn Settings'
            if (!myBehaviour.canDespawn)
            {
                excludedProperties.Add("despawnTime");
                excludedProperties.Add("killVelocity");
            }
            else
            {
                excludedProperties.Remove("despawnTime");
                excludedProperties.Remove("killVelocity");
            }

            //Header: 'BaseElement Settings'
            if (!myBehaviour.elementIsSpawnedByElementSpawner)
            {
                excludedProperties.Add("baseElementSpawnTime");
                excludedProperties.Add("baseElementMesh");
                excludedProperties.Add("BaseElementCollisionPrefab");
                excludedProperties.Add("pickupColliderRadius");
                excludedProperties.Add("rbMass");
                excludedProperties.Add("rbDrag");
                excludedProperties.Add("rbAngularDrag");
                excludedProperties.Add("elementSpawnerCanSpawnSeedPod");

                //Header: 'GrownObject Settings'
                excludedProperties.Add("GrownObjectAnimator");
                excludedProperties.Add("colliderYPos");
                excludedProperties.Add("colliderRadius");
                excludedProperties.Add("colliderHeight");
                excludedProperties.Add("GrownObjectGrowthPrefabs");
                excludedProperties.Add("grownObjectGrowTime");
                excludedProperties.Add("ElementSpawnTransforms");
            }
            else
            {
                excludedProperties.Remove("baseElementSpawnTime");
                excludedProperties.Remove("baseElementMesh");
                excludedProperties.Remove("BaseElementCollisionPrefab");
                excludedProperties.Remove("pickupColliderRadius");
                excludedProperties.Remove("rbMass");
                excludedProperties.Remove("rbDrag");
                excludedProperties.Remove("rbAngularDrag");
                excludedProperties.Remove("elementSpawnerCanSpawnSeedPod");
            }

            //Header: 'Seed Pod Settings' p1
            if (!myBehaviour.elementCanBecomeSeedPod)
            {
                excludedProperties.Add("seedPodSpawnChance");
                excludedProperties.Add("ElementLeavesPrefab");
                excludedProperties.Add("seedPodElementType");
            }
            else
            {
                excludedProperties.Remove("seedPodSpawnChance");
                excludedProperties.Remove("ElementLeavesPrefab");
                excludedProperties.Remove("seedPodElementType");
            }

            //Header: 'Seed Pod Settings' p2
            if (!myBehaviour.elementIsSeedPod)
            {
                excludedProperties.Add("SeedPodPrefab");
                excludedProperties.Add("seedPodPosOffset");
                excludedProperties.Add("canPlantManually");

                //Header: 'Grown Object Settings'
                if (!myBehaviour.elementIsSpawnedByElementSpawner && myBehaviour.grownObjectType != GrownObjectType.GrowableLink)
                {
                    excludedProperties.Add("GrownObjectAnimator");
                    excludedProperties.Add("colliderYPos");
                    excludedProperties.Add("colliderRadius");
                    excludedProperties.Add("colliderHeight");
                    excludedProperties.Add("GrownObjectGrowthPrefabs");
                    excludedProperties.Add("grownObjectGrowTime");
                    excludedProperties.Add("ElementSpawnTransforms");
                }
                else
                {
                    excludedProperties.Remove("GrownObjectAnimator");
                    excludedProperties.Remove("colliderYPos");
                    excludedProperties.Remove("colliderRadius");
                    excludedProperties.Remove("colliderHeight");
                    excludedProperties.Remove("GrownObjectGrowthPrefabs");
                    excludedProperties.Remove("grownObjectGrowTime");
                    excludedProperties.Remove("ElementSpawnTransforms");
                }
            }
            else
            {
                excludedProperties.Remove("SeedPodPrefab");
                excludedProperties.Remove("seedPodPosOffset");
                excludedProperties.Remove("canPlantManually");

                //Header: 'Grown Object Settings'
                if (!myBehaviour.elementCanSpawnGrownObject && !myBehaviour.canPlantManually)
                {
                    excludedProperties.Add("GrownObjectAnimator");
                    excludedProperties.Add("colliderYPos");
                    excludedProperties.Add("colliderRadius");
                    excludedProperties.Add("colliderHeight");
                    excludedProperties.Add("GrownObjectGrowthPrefabs");
                    excludedProperties.Add("grownObjectGrowTime");
                    excludedProperties.Add("ElementSpawnTransforms");
                }
                else
                {
                    excludedProperties.Remove("GrownObjectAnimator");
                    excludedProperties.Remove("colliderYPos");
                    excludedProperties.Remove("colliderRadius");
                    excludedProperties.Remove("colliderHeight");
                    excludedProperties.Remove("GrownObjectGrowthPrefabs");
                    excludedProperties.Remove("grownObjectGrowTime");
                    excludedProperties.Remove("ElementSpawnTransforms");
                }
            }

            //Header: 'Grown Object Settings'
            if (!myBehaviour.elementCanSpawnGrownObject)
            {
                excludedProperties.Add("grownObjectType");
                excludedProperties.Add("grownObjectElement");
            }
            else
            {
                excludedProperties.Remove("grownObjectType");
                excludedProperties.Remove("grownObjectElement");
            }

            //Header: 'Precipitate Settings'
            if (!myBehaviour.elementHasPrecipitateForm)
            {
                excludedProperties.Add("elementPrecipitateType");
                excludedProperties.Add("minimumVelocity");
                excludedProperties.Add("maximumVelocity");
                excludedProperties.Add("velocityMultiplier");

                //Header: 'BaseElement Precipitation Settings'
                excludedProperties.Add("shrinkSpeed");
                excludedProperties.Add("elementPrecipitateAmount");
                excludedProperties.Add("elementPrecipitateSpawnChance");
            }
            else
            {
                excludedProperties.Remove("elementPrecipitateType");
                excludedProperties.Remove("minimumVelocity");
                excludedProperties.Remove("maximumVelocity");
                excludedProperties.Remove("velocityMultiplier");
                excludedProperties.Remove("shrinkSpeed");
                excludedProperties.Remove("elementPrecipitateAmount");

                //Header: 'BaseElement Precipitation Settings'
                if (!myBehaviour.elementIsSpawnedByElementSpawner)
                {
                    excludedProperties.Add("shrinkSpeed");
                    excludedProperties.Add("elementPrecipitateAmount");
                    excludedProperties.Add("elementPrecipitateSpawnChance");
                }
                else
                {
                    excludedProperties.Remove("shrinkSpeed");
                    excludedProperties.Remove("elementPrecipitateAmount");
                    excludedProperties.Remove("elementPrecipitateSpawnChance");
                }
            }
             //hide velocityMultiplier if minVel & maxVel unmodified 
            if (myBehaviour.minimumVelocity == 0 && myBehaviour.maximumVelocity == Mathf.Infinity) excludedProperties.Add("velocityMultiplier");
            else excludedProperties.Remove("velocityMultiplier");

            //Header: 'Effect Settings'
            if (!myBehaviour.elementHasUsableEffect)
            {
                excludedProperties.Add("elementEffectPrimingTrigger");
                excludedProperties.Add("effectPrimingThreshold");
                excludedProperties.Add("effectUseTrigger");
                excludedProperties.Add("ingestedEffect");
                excludedProperties.Add("ingestedEffectStrength");
                excludedProperties.Add("ingestedEffectDuration");
            }
            else
            {
                excludedProperties.Remove("elementEffectPrimingTrigger");
                excludedProperties.Remove("effectPrimingThreshold");
                excludedProperties.Remove("effectUseTrigger");
                excludedProperties.Remove("ingestedEffect");
                excludedProperties.Remove("ingestedEffectStrength");
                excludedProperties.Remove("ingestedEffectDuration");
            }
             //elementEffectPrimingTrigger ENUM Toggles
            switch (myBehaviour.elementEffectPrimingTrigger)
            {
                default:
                    excludedProperties.Add("effectPrimingThreshold");
                    break;

                case ElementPrimingTrigger.Always:
                    excludedProperties.Add("effectPrimingThreshold");
                    break;

                case ElementPrimingTrigger.Mixing:
                    excludedProperties.Remove("effectPrimingThreshold");
                    break;

                case ElementPrimingTrigger.Heating:
                    excludedProperties.Remove("effectPrimingThreshold");
                    break;

                case ElementPrimingTrigger.Lighting:
                    excludedProperties.Remove("effectPrimingThreshold");
                    break;
            }
             //effectUseTrigger ENUM Toggles
            switch (myBehaviour.effectUseTrigger)
            {
                default:

                    excludedProperties.Add("ingestedEffect");
                    excludedProperties.Add("ingestedEffectStrength");
                    excludedProperties.Add("ingestedEffectDuration");

                    if (!myBehaviour.elementCanSpawnGrownObject)
                    {
                        myBehaviour.grownObjectType = GrownObjectType.None;
                        myBehaviour.grownObjectElement = ElementTypes.None;
                    }
                    break;

                case ElementUseTrigger.Ingesting:

                    excludedProperties.Remove("ingestedEffect");
                    excludedProperties.Remove("ingestedEffectStrength");
                    excludedProperties.Remove("ingestedEffectDuration");
                    break;

                case ElementUseTrigger.GroundingGrownObject:

                    excludedProperties.Add("ingestedEffect");
                    excludedProperties.Add("ingestedEffectStrength");
                    excludedProperties.Add("ingestedEffectDuration");

                    myBehaviour.elementHasPrecipitateForm = true;
                    break;
            }

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }
    }
}
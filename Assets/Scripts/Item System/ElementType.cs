using UnityEngine;
using Catacombs.Base;
using UnityEditor.Animations;
using UnityEditor;
using Boo.Lang;

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
        CloverSprout = 10
    }
    public enum ElementPrecipitates
    {
        None,
        Dust = 1,
        Drip = 2
    }

    public enum ElementUseTrigger
    {
        None = 0,
        Ingesting = 1,
        Grounding = 2
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


        [Header("Despawn Settings")]
        [Tooltip("Can instances of this Element despawn from collision or over time?")]
        public bool canDespawn;
        [Tooltip("The amount of time to wait before killing this Element due to inactivity")]
        [Min(2)] public int despawnTime = 60;
        [Tooltip("The maximum velocity this Element may impact any collider before dying")]
        public float killVelocity = Mathf.Infinity;

        [Header("Element Spawner Settings")]
        [Tooltip("Is this Element intended to spawn from an Element Spawner of its ElementType?")]
        public bool spawnsFromElementSpawner;
        [Space(3)]
        [Tooltip("The AnimatorController for the Element Spawner")]
        public AnimatorController ElementSpawnerAnimator;

        public float colliderYPos;
        public float colliderRadius = 0.05f;
        public float colliderHeight = 0.5f;

        [Tooltip("A list of prefabs for each stage of growth (including complete) the Spawner has")]
        public GameObject[] ElementSpawnerGrowthPrefabs;
        [Tooltip("A GameObject containing all child spawnpoints")]
        public GameObject ElementSpawnTransforms;
        [Tooltip("The time (in seconds) it takes this Element's Spawner to fully grow")]
        public int elementSpawnerGrowTime;
        [Tooltip("The time (in seconds) it takes this Element to grow on an Element Spawner")]
        public int elementSpawnTime;

        [Header("Base Element Settings")]
        public Mesh baseElementMesh;
        public GameObject BaseElementCollisionPrefab;
        public float pickupColliderRadius;

        public float rbMass = 1f;
        public float rbDrag;
        public float rbAngularDrag = 0.05f;


        //Precipitate (verb): To cause drops of moisture or particles of dust to be deposited from the atmosphere or from a vapor or suspension (in this case the BaseElement)
        //Precipitate (noun): A substance precipitated from a solution (again, kinda BaseElement)
        [Header("Precipitate Settings")]
        [Tooltip("If this Element can be ground up into Precipitate by tools like the Pestle")]
        public bool canCreatePrecipitate;
        [Tooltip("Does this Element precipitate solids (Dust) or liquids (Drip)?")]
        public ElementPrecipitates elementPrecipitateType;
        [Tooltip("The rate at which scale is subtracted when colliding with a moving Pestle")]
        public float shrinkSpeed = 0.001f;
        [Tooltip("The total amount of Precipitate objects to produce before being destroyed")]
        public int elementPrecipitateAmount = 3;
        [Tooltip("The minimum velocity the Element Precipitate obj may have before being sped up")]
        public float minimumVelocity = 0;
        [Tooltip("The maximum velocity the Element Precipitate obj may have before being slowed down")]
        public float maximumVelocity = Mathf.Infinity;
        [Tooltip("The %rate at which the Element Precipitate obj's velocity is modified")]
        [Range(0, 0.5f)] public float velocityMultiplier = 0.01f;


        [Header("Seed Pod Settings")]
        [Tooltip("Whether or not this Element can turn into a Seed Pod variant when grown")]
        public bool canSpawnSeedPod;
        [Tooltip("The chances of this grown Element turning into a Seed Pod")]
        [Range(0, 100)] public float seedPodSpawnChance;
        [Tooltip("Which SeedPod Prefab to spawn when this element turns into a Seed Pod variant")]
        public GameObject SeedPodPrefab;
        [Tooltip("The local position the SeedPod Prefab should start in")]
        public Vector3 seedPodPosOffset;
        [Tooltip("What prefab to spawn if object isn't a SeedPod (leave null if none exists)")]
        public GameObject ElementLeavesPrefab;
        [Tooltip("The ElementType for the Seed Pod variant")]
        public ElementTypes seedPodElementType;
        [Tooltip("Whether or not the Seed Pod can be planted in ObjectGrowingPlots by hand")]
        public bool canPlantManually;


        [Header("Effect Settings")]
        [Tooltip("Whether or not this element can be used to perform effects")]
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
        [Tooltip("The time (in seconds) it before the ingested effect wears off")]
        public float ingestedEffectDuration;

        [Tooltip("The Prefab for the GrownObject to instantiate when Grounded")]
        public GameObject GrownObjectPrefab;
    }

    [CustomEditor(typeof(ElementType))]
    public class ElementTypeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            List<string> excludedProperties = new List<string>();

            ElementType myBehaviour = target as ElementType;

            //canDespawn BOOL Toggles
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

            //canSpawnFromElementSpawner BOOL Toggles
            if (!myBehaviour.spawnsFromElementSpawner)
            {
                excludedProperties.Add("BaseElementCollisionPrefab");
                excludedProperties.Add("baseElementMesh");
                excludedProperties.Add("pickupColliderRadius");
                excludedProperties.Add("rbMass");
                excludedProperties.Add("rbDrag");
                excludedProperties.Add("rbAngularDrag");

                excludedProperties.Add("ElementSpawnerAnimator");
                excludedProperties.Add("colliderYPos");
                excludedProperties.Add("colliderRadius");
                excludedProperties.Add("colliderHeight");
                excludedProperties.Add("ElementSpawnerGrowthPrefabs");
                excludedProperties.Add("ElementSpawnTransforms");
                excludedProperties.Add("elementSpawnerGrowTime");
                excludedProperties.Add("elementSpawnTime");
                excludedProperties.Add("canPlantManually");
            }
            else
            {
                excludedProperties.Remove("BaseElementCollisionPrefab");
                excludedProperties.Remove("baseElementMesh");
                excludedProperties.Remove("pickupColliderRadius");
                excludedProperties.Remove("rbMass");
                excludedProperties.Remove("rbDrag");
                excludedProperties.Remove("rbAngularDrag");

                excludedProperties.Remove("ElementSpawnerAnimator");
                excludedProperties.Remove("colliderYPos");
                excludedProperties.Remove("colliderRadius");
                excludedProperties.Remove("colliderHeight");
                excludedProperties.Remove("ElementSpawnerGrowthPrefabs");
                excludedProperties.Remove("ElementSpawnTransforms");
                excludedProperties.Remove("elementSpawnerGrowTime");
                excludedProperties.Remove("elementSpawnTime");
                excludedProperties.Remove("canPlantManually");
            }

            //canSpawnPrecipitate BOOL Toggles
            if (!myBehaviour.canCreatePrecipitate)
            {
                excludedProperties.Add("elementPrecipitateType");
                excludedProperties.Add("shrinkSpeed");
                excludedProperties.Add("elementPrecipitateAmount");
                excludedProperties.Add("minimumVelocity");
                excludedProperties.Add("maximumVelocity");
                excludedProperties.Add("velocityMultiplier");
            }
            else
            {
                excludedProperties.Remove("elementPrecipitateType");
                excludedProperties.Remove("shrinkSpeed");
                excludedProperties.Remove("elementPrecipitateAmount");
                excludedProperties.Remove("minimumVelocity");
                excludedProperties.Remove("maximumVelocity");
                excludedProperties.Remove("velocityMultiplier");
            }

            //hide velocityMultiplier if minVel & maxVel unmodified 
            if (myBehaviour.minimumVelocity == 0 && myBehaviour.maximumVelocity == Mathf.Infinity) excludedProperties.Add("velocityMultiplier");
            else excludedProperties.Remove("velocityMultiplier");


            //canSpawnSeedPod BOOL Toggles
            if (!myBehaviour.canSpawnSeedPod)
            {
                excludedProperties.Add("seedPodSpawnChance");
                excludedProperties.Add("SeedPodPrefab");
                excludedProperties.Add("seedPodPosOffset");
                excludedProperties.Add("ElementLeavesPrefab");
                excludedProperties.Add("seedPodElementType");
            }
            else
            {
                excludedProperties.Remove("seedPodSpawnChance");
                excludedProperties.Remove("SeedPodPrefab");
                excludedProperties.Remove("seedPodPosOffset");
                excludedProperties.Remove("ElementLeavesPrefab");
                excludedProperties.Remove("seedPodElementType");
            }

            //elementHasUsableEffect BOOL Toggles
            if (!myBehaviour.elementHasUsableEffect)
            {
                excludedProperties.Add("elementEffectPrimingTrigger");
                excludedProperties.Add("effectPrimingThreshold");
                excludedProperties.Add("effectUseTrigger");
                excludedProperties.Add("ingestedEffect");
                excludedProperties.Add("ingestedEffectStrength");
                excludedProperties.Add("ingestedEffectDuration");
                excludedProperties.Add("GrownObjectPrefab");
            }
            else
            {
                excludedProperties.Remove("elementEffectPrimingTrigger");
                excludedProperties.Remove("effectPrimingThreshold");
                excludedProperties.Remove("effectUseTrigger");
                excludedProperties.Remove("ingestedEffect");
                excludedProperties.Remove("ingestedEffectStrength");
                excludedProperties.Remove("ingestedEffectDuration");
                excludedProperties.Remove("GrownObjectPrefab");
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
                    excludedProperties.Add("GrownObjectPrefab");
                    break;

                case ElementUseTrigger.Ingesting:

                    excludedProperties.Remove("ingestedEffect");
                    excludedProperties.Remove("ingestedEffectStrength");
                    excludedProperties.Remove("ingestedEffectDuration");
                    excludedProperties.Add("GrownObjectPrefab");
                    break;

                case ElementUseTrigger.Grounding:

                    excludedProperties.Add("ingestedEffect");
                    excludedProperties.Add("ingestedEffectStrength");
                    excludedProperties.Add("ingestedEffectDuration");
                    excludedProperties.Remove("GrownObjectPrefab");
                    break;
            }

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }
    }
}
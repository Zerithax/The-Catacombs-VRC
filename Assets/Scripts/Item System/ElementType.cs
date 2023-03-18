using UnityEngine;
using Catacombs.ElementSystem.Runtime;
using Catacombs.Base;

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
        Linktorch = 7
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
        public bool canDespawn;
        [Min(2)] public int despawnTime = 60;

        [Tooltip("The maximum velocity this Element may impact any collider before dying")]
        public float killVelocity = Mathf.Infinity;

        
        [Header("Precipitate Settings")]
        [Tooltip("If this Element can be ground up into Precipitate by tools like the Pestle")]
        public bool canCreatePrecipitate;
        public ElementPrecipitates elementPrecipitateType;
        public GameObject ElementPrecipitatePrefab;
        public float shrinkSpeed = 0.001f;
        public int elementPrecipitateAmount = 5;


        //Precipitate (verb): To cause drops of moisture or particles of dust to be deposited from the atmosphere or from a vapor or suspension (in this case the BaseElement)
        //Precipitate (noun): A substance precipitated from a solution (again, kinda BaseElement)
        [Header("Precipitate Settings")]
        [Tooltip("The minimum velocity the Element Precipitate may have before being sped up")]
        public float minimumVelocity = 0;

        [Tooltip("The maximum velocity the Element Precipitate may have before being slowed down")]
        public float maximumVelocity = Mathf.Infinity;

        [Tooltip("The %rate at which the Element Precipitate's velocity is modified")]
        [Range(0, 0.5f)]public float velocityMultiplier = 0.01f;


        [Header("Spawner Settings")]
        [Tooltip("Whether or not this Element can spawn Seed Pod variants that produce Element Spawners")]
        public bool canCreateSpawner;

        [Tooltip("The chances of a grown Element having a Seed Pod")]
        [Range(0, 100)] public float seedGrowChance;

        [Tooltip("The time (in seconds) it takes this Element's Spawner to fully grow")]
        public int elementSpawnerGrowTime;

        [Tooltip("The time (in seconds) it takes this Element to grow on an Element Spawner")]
        public int elementSpawnTime;
        public GameObject ElementSpawnerPrefab;
        public GameObject BaseElementPrefab;


        [Header("Is Potion?")]
        [Tooltip("Whether or not this element performs potion effects")]
        public bool elementIsPotion;

        [Tooltip("The Trigger required to Prime this Element's Potion Effect")]
        public ElementPrimingTrigger elementEffectPrimingTrigger;

        [Tooltip("The required intensity to activate the PrimingTrigger (dependent on Trigger type)")]
        [Range(0, 100)] public int effectPrimingThreshold = 100;

        [Tooltip("The Trigger required to cause this Element's effect")]
        public ElementUseTrigger effectUseTrigger;

        //NOTE: There must be a set of variables for each ElementUseTrigger, which should only be visible dependent on the selected effectUseTrigger

        //These are the Ingestion variables: PlayerEffect, Strength, Duration
        [Tooltip("The effect to apply when ingested")]
        public PlayerEffect ingestedEffect;

        [Tooltip("The strength to multiply the ingested effect by")]
        public int ingestedEffectStrength;

        [Tooltip("The time (in seconds) it before the ingested effect wears off")]
        public float ingestedEffectDuration;

        //These are the Grounding variables: GrownObject prefab, growTime
        [Tooltip("The Prefab for the GrownObject to instantiate when Grounded")]
        public GameObject GrownObjectPrefab;

        [Tooltip("The time it takes the GrownObject to grow")]
        public float growTime;
    }
}
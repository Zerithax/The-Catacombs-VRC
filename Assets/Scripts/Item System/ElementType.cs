using Catacombs.Base;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Catacombs.ElementSystem.Runtime;

namespace Argus.ItemSystem.Editor
{
    [CreateAssetMenu(fileName = "New Element Type", menuName = "Catacombs/ElementType")]
    public class ElementType : ScriptableObject
    {
        [Header("Element")]
        public ElementTypes elementTypeId;
        public Color elementColor;


        [Header("Death Settings")]
        public bool canDespawn;
        public float killVelocity = Mathf.Infinity;
        [Min(2)] public int despawnTime = 60;


        //Precipitate (verb): To cause drops of moisture or particles of dust to be deposited from the atmosphere or from a vapor or suspension (in this case the BaseElement)
        //Precipitate (noun): A substance precipitated from a solution (again, kinda BaseElement)
        [Header("Base Element Settings")]
        public bool canCreatePrecipitate;
        public ElementPrecipitates elementPrecipitateType;
        public GameObject ElementPrecipitatePrefab;
        public float shrinkSpeed = 0.001f;
        public int elementPrecipitateAmount = 5;


        [Header("Precipitate Settings")]
        public bool scaleVelocity;
        public float targetVelocity;
        public float momentumScale = 1.01f;


        [Header("Spawner Settings")]
        public bool canCreateSpawner;
        public GameObject ElementSpawnerPrefab;
        public int elementSpawnerGrowTime;
        public GameObject BaseElementPrefab;
        public int elementSpawnTime;
        [Range(0, 100)] public float seedGrowChance;
    }
}
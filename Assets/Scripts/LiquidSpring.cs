using Argus.ItemSystem;
using UdonSharp;
using UnityEngine;

namespace Catacombs.ElementSystem.Runtime
{
    public class LiquidSpring : UdonSharpBehaviour
    {
        [SerializeField] private ElementTypeManager elementTypeManager;
        [SerializeField] private ElementTypes elementToSpawn;
        [SerializeField] private int spawnCountdown;

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer > spawnCountdown)
            {
                timer = 0;
                ElementPrecipitate newDroplet = Instantiate(elementTypeManager.elementTypeData[(int)elementToSpawn].ElementPrecipitatePrefab, transform.position, Quaternion.identity, transform).GetComponent<ElementPrecipitate>();

                newDroplet.elementTypeManager = elementTypeManager;
                newDroplet.elementTypeId = elementToSpawn;

            }
        }
    }
}
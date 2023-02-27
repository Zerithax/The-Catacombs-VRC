using Argus.ItemSystem;
using UdonSharp;
using UnityEngine;

namespace Catacombs.ElementSystem.Runtime
{
    //TODO: Rewrite this as ElementDispenser, the script that manages dispensing Elements (optionally from an inventory, for future Element Storage Containers)
    //ElementDispenser should have a toggle to decide whether or not it dispenses BaseElements or an Elements' Precipitate (like a LiquidSpring)

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
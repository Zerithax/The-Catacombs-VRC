using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.Base
{
    public enum BerryTypes
    {
        None = 0,
        Linkberry = 1,
        Arieberry = 2,
        Blueberry = 3
    }

    public class Berry : ContainableItem
    {
        [Header("Berry Fields")]
        [SerializeField] private int berryDustCount = 5;
        private Vector3 baseScale;
        private int baseBerrySize;
        private int curBerrySize;
        private Vector3[] berrySizeIntervals = new Vector3[0];
        [SerializeField] private float shrinkSpeed = 0.001f;

        [Header("Berry Dust")]
        [SerializeField] private bool spawnBerryDust = false;
        [SerializeField] private BerryTypes berryType;
        [SerializeField] private GameObject berryDustPrefab;
        [SerializeField] private Color dustColor;

        [Header("Liquid Drops")]
        [SerializeField] private bool spawnLiquidDrop = false;
        [SerializeField] private LiquidTypes liquidType;
        [SerializeField] private GameObject liquidDropPrefab;
        [SerializeField] private Color liquidColor;

        private Vector3 enterPos;

        protected override void AdditionalStart()
        {
            //Add one extra point, since berries die at the last stage, not 0
            baseBerrySize = berryDustCount + 1;

            baseScale = transform.parent.localScale;
            curBerrySize = baseBerrySize;

            berrySizeIntervals = new Vector3[baseBerrySize];

            for (int i = 0; i < baseBerrySize; i++) berrySizeIntervals[i] = baseScale / baseBerrySize * i;

            dustColor = itemRend.material.color;

        }

        private void OnTriggerStay(Collider other)
        {
            //If colliding with a pestle
            if (other.gameObject.layer == 23)
            {
                Vector3 curPos = other.transform.position;

                //And pestle is actively moving
                if (isContained && curPos != enterPos)
                {
                    //Subtract a steady stream of scale
                    parentObject.transform.localScale -= new Vector3(shrinkSpeed, shrinkSpeed, shrinkSpeed);

                    //Auto destroy if too small
                    if (parentObject.transform.localScale.x <= berrySizeIntervals[1].x) Destroy(parentObject);

                    //At every equal interval of baseScale, SpawnDust()
                    for (int i = berrySizeIntervals.Length - 1; i > 0; i--)
                    {
                        Debug.Log(i);
                        if ((float)Math.Round(parentObject.transform.localScale.x, 3) == (float)Math.Round(berrySizeIntervals[i].x, 3))
                        {
                            //Change this to spawn liquid OR dust depending on which is selected
                            if (spawnBerryDust) SpawnDust();
                            if (spawnLiquidDrop) SpawnDrop();
                            SpawnDust();
                            break;
                        }
                    }
                }

                enterPos = curPos;
            }
        }

        private void SpawnDust()
        {
            //Spawn Dust at Berry location (TODO: pick a random spot around the berry?)
            GameObject newDust = Instantiate(berryDustPrefab);
            newDust.transform.position = transform.position;
            newDust.transform.parent = parentObject.transform.parent;

            newDust.name = $"{parentObject.name} Dust";
            Debug.Log($"Spawned {newDust.name}");

            //Tint dust to avg of Berry Dust color & Green
            //TODO: make each berry a random range between mostly-berrydust and mostly-green instead of static color
            newDust.GetComponent<Renderer>().material.color = new Color((dustColor.r * 2 + Color.green.r) / 3, (dustColor.g * 2 + Color.green.g) / 3, (dustColor.b * 2 + Color.green.b) / 3);

            BerryDust newDustComp = newDust.GetComponent<BerryDust>();
            Debug.Log($"Setting new dust's berry type to {berryType}");
            newDustComp.berryDustType = berryType;
            Debug.Log($"Set to: {newDustComp.berryDustType}");
            newDustComp.berryColor = dustColor;

            ShrinkBerry();
        }

        private void SpawnDrop()
        {
            //Spawn Drop at berry location
            GameObject newDrop = Instantiate(liquidDropPrefab);
            newDrop.transform.position = transform.position;
            newDrop.transform.parent = parentObject.transform.parent;

            newDrop.name = $"{parentObject.name} Drop";
            Debug.Log($"Spawned {newDrop.name}");

            newDrop.GetComponent<Renderer>().material.color = liquidColor;
            newDrop.GetComponent<TrailRenderer>().material.color = liquidColor;

            LiquidDrop newDropComp = newDrop.GetComponent<LiquidDrop>();
            newDropComp.liquidType = liquidType;

            ShrinkBerry();
        }

        private void ShrinkBerry()
        {
            curBerrySize--;
            if (curBerrySize <= 1 || parentObject.transform.localScale.x < 0)
            {
                Debug.Log($"{name} is out of berry dust, destroying");
                Destroy(parentObject);
            }
        }
    }
}
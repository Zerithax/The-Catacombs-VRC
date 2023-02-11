using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.Base
{
    //Containers are any objects that will be able to hold other items in them
    //This currently only consists of Dusts and Liquids, but may be expanded upon as needed
    public class Container : UdonSharpBehaviour
    {
        //The amount of liquid particles required to reach fullness
        [SerializeField] private int maxLiquidParts = 4;
        [SerializeField] private int curLiquidParts = 0;
        private int dustParts = 0;
        private int totalDustColors = 0;

        [SerializeField] private Renderer containerRend;

        [Header("Clipping Plane")]
        [SerializeField] private Renderer liquidClippingPlane;
        public Vector3 planeNormal;
        public Vector3 planePosition;
        [SerializeField] private float clippingPlaneMaxHeight;
        [SerializeField] private float clippingPlaneMinHeight;


        [Header("Recipe Containers")]
        [SerializeField] private LiquidTypes[] containedLiquids = new LiquidTypes[0];
        [SerializeField] private BerryTypes[] containedBerries = new BerryTypes[0];

        void Start()
        {
            if (containerRend == null) containerRend = GetComponent<Renderer>();

            containedLiquids = new LiquidTypes[maxLiquidParts];
            containedBerries = new BerryTypes[maxLiquidParts];

            Transform planeTransform = liquidClippingPlane.transform;
            if (planeTransform.localPosition.y != 0) clippingPlaneMaxHeight = planeTransform.localPosition.y;
            planeTransform.localPosition = new Vector3(0, -1, 0);


            planeNormal = liquidClippingPlane.transform.TransformVector(new Vector3(0, 0, -1));
            planePosition = liquidClippingPlane.transform.position;
            UpdateShaderProperties();
        }

        private void Update()
        {
            UpdateShaderProperties();
            liquidClippingPlane.transform.rotation = Quaternion.Euler(new Vector3(90, liquidClippingPlane.transform.rotation.y, 0));

            //TODO: Check if local y tilts too far from vector3.y and has liquid (scaling with fullness), create LiquidDrop prefab at container edge every 0.5 seconds
        }

        private void UpdateShaderProperties()
        {
            planeNormal = liquidClippingPlane.transform.TransformVector(new Vector3(0, 0, -1));
            planePosition = liquidClippingPlane.transform.position;
            for (int i = 0; i < containerRend.materials.Length; i++)
            {
                if (containerRend.materials[i].shader.name == "CrossSection/OnePlaneBSP")
                {
                    containerRend.materials[i].SetVector("_PlaneNormal", planeNormal);
                    containerRend.materials[i].SetVector("_PlanePosition", planePosition);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.gameObject == null) return;

            //If a Water object enters the collider...
            if (other.gameObject.layer == 4)
            {
                if (curLiquidParts < maxLiquidParts)
                {
                    //Start the liquid color off as the first liquid
                    if (curLiquidParts == 0)
                    {
                        Color liquidColor = other.GetComponent<Renderer>().material.color;
                        containerRend.material.color = liquidColor;
                        liquidClippingPlane.material.color = liquidColor;
                    }

                    curLiquidParts++;

                    //TODO: Make this lerp instead of snapping to the new fullness position
                    liquidClippingPlane.transform.localPosition = new Vector3(0, (clippingPlaneMaxHeight / maxLiquidParts) * curLiquidParts + clippingPlaneMinHeight, 0);

                    LiquidDrop liquidComp = other.GetComponent<LiquidDrop>();
                    for (int i = 0; i < containedLiquids.Length; i++)
                    {
                        if (containedLiquids[i] == 0)
                        {
                            containedLiquids[i] = liquidComp.liquidType;
                            break;
                        }
                    }
                }

                //ALWAYS destroy liquids that enter containers, even if already full
                Destroy(other.gameObject);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            //As long as a containable object is in the collider...
            if (other.gameObject.layer == 24)
            {
                //We only currently care if a Berry Dust item is in the container
                BerryDust dustComp = other.GetComponent<BerryDust>();
                if (dustComp == null) return;

                Debug.Log($"Comparing {curLiquidParts} >= {(float)maxLiquidParts / 3}");
                //If the container is at least a third full of liquid, it can absorb Dust!
                if (curLiquidParts >= (float)maxLiquidParts / 3)
                {
                    totalDustColors++;

                    //If there isn't already more dust than the total amount of liquid, add to the recipe list
                    if (dustParts < maxLiquidParts)
                    {
                        dustParts++;
                        for (int i = 0; i < containedLiquids.Length; i++)
                        {
                            if (containedBerries[i] == 0)
                            {
                                containedBerries[i] = dustComp.berryDustType;
                                break;
                            }
                        }
                    }

                    //A Dust-Ready container will ALWAYS kill dust, even if it didn't absorb it!
                    Destroy(other.gameObject);

                    containerRend.material.color += dustComp.berryColor / totalDustColors;
                }
            }
        }
    }
}
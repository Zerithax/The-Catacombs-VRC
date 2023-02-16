using UdonSharp;
using UnityEngine;
using System;
using VRC.SDKBase;
using VRC.Udon;
using UnityEditorInternal;
using UnityEngine.Rendering.PostProcessing;

namespace Catacombs.Base
{
    //Containers are any objects that will be able to hold other items in them
    //Contanable items currently only consists of Dusts and Liquids, but may be expanded upon as needed
    public class Container : UdonSharpBehaviour
    {
        [SerializeField] private GameObject liquidDropPrefab;
        [SerializeField] private Renderer containerRend;
        [SerializeField] private GameObject containerSpout;
        private MeshFilter containerSpoutFilter;

        //The max amount of liquid particles container may store before full
        [SerializeField] private int maxLiquidParts = 4;
        [SerializeField] private int curLiquidParts = 0;
        private int dustParts = 0;
        private int totalColors = 1;
        private Color curLiquidColor;

        [SerializeField] private float timeToPour = 0.5f;
        private bool canPour;
        private float pourTimer;



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
            containerSpoutFilter = containerSpout.GetComponent<MeshFilter>();

            containedLiquids = new LiquidTypes[maxLiquidParts];
            containedBerries = new BerryTypes[maxLiquidParts];

            Transform planeTransform = liquidClippingPlane.transform;
            if (planeTransform.localPosition.y != 0) clippingPlaneMaxHeight = planeTransform.localPosition.y;
            planeTransform.localPosition = new Vector3(0, -1, 0);

            UpdateShaderProperties();
        }

        private void Update()
        {
            liquidClippingPlane.transform.rotation = Quaternion.Euler(new Vector3(90, liquidClippingPlane.transform.rotation.y, 0));

            if (curLiquidParts > 0)
            {
                UpdateShaderProperties();

                //If container isn't pointing straight up, check to see if it can pour
                if (Vector3.Angle(Vector3.up, transform.up) != 0) CheckPouring();
            }

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
                    Debug.Log($"[{name}] has tipped enough to pour!");
                    pourTimer = 0;


                    GameObject newDrop = Instantiate(liquidDropPrefab);
                    Color liquidColor = containerRend.material.GetColor("_CrossColor");

                    //Loop through all vertices of spout mesh, then position berry at the lowest one!
                    Vector3[] spoutVertices = containerSpoutFilter.mesh.vertices;
                    Vector3 lowestVert = new Vector3(0, Mathf.Infinity, 0);

                    foreach (Vector3 vert in spoutVertices)
                    {
                        Vector3 vertGlobal = transform.TransformPoint(vert);
                        if (vertGlobal.y < lowestVert.y) lowestVert = vertGlobal;
                    }

                    lowestVert += containerSpoutFilter.transform.localPosition;
                    newDrop.transform.position = lowestVert;

                    newDrop.name = $"{transform.parent.name} Drop";
                    Debug.Log($"Spawned {newDrop.name}");

                    LiquidDrop newDropComp = newDrop.GetComponent<LiquidDrop>();
                    newDropComp.containerSpawnedFrom = gameObject;
                    newDropComp.liquidType = containedLiquids[0];

                    newDrop.GetComponent<Renderer>().material.color = liquidColor;
                    newDrop.GetComponent<TrailRenderer>().material.SetColor("_EmissionColor", liquidColor);                 

                    curLiquidParts--;

                    //If we still have any liquid, lower the clipping plane by 1, otherwise move it away and stop updating until needed
                    if (curLiquidParts > 0) liquidClippingPlane.transform.localPosition = new Vector3(0, (clippingPlaneMaxHeight - clippingPlaneMinHeight) / maxLiquidParts * curLiquidParts + clippingPlaneMinHeight, 0);
                    else
                    {
                        liquidClippingPlane.transform.position = new Vector3(liquidClippingPlane.transform.position.x, transform.position.y - 1, liquidClippingPlane.transform.position.z);

                        //Manually call to make sure the system recognizes it's moved away
                        UpdateShaderProperties();
                    }

                    for (int i = containedLiquids.Length - 1; i > 0; i--)
                    {
                        if (containedLiquids[i] != LiquidTypes.None)
                        {
                            containedLiquids[i] = LiquidTypes.None;
                            break;
                        }
                    }
                }
            }
            else pourTimer = 0;
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
                LiquidDrop liquidComp = other.GetComponent<LiquidDrop>();

                //Reject liquid drops that came from us!
                if (liquidComp.containerSpawnedFrom == gameObject) return;

                if (curLiquidParts < maxLiquidParts)
                {
                    liquidClippingPlane.gameObject.SetActive(true);

                    //TODO: Make this lerp instead of snapping to the new fullness position
                    liquidClippingPlane.transform.localPosition = new Vector3(0, (clippingPlaneMaxHeight - clippingPlaneMinHeight) / maxLiquidParts * (curLiquidParts + 1) + clippingPlaneMinHeight, 0);

                    //Start the liquid color off as the first liquid
                    if (curLiquidParts == 0)
                    {
                        curLiquidColor = other.GetComponent<Renderer>().material.color;
                        containerRend.material.SetColor("_CrossColor", curLiquidColor);
                        containerRend.material.SetColor("_Color", curLiquidColor);
                        liquidClippingPlane.material.color = curLiquidColor;
                    }
                    else if (liquidComp.liquidType != containedLiquids[0]) return;

                    curLiquidParts++;

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
                liquidComp.itemRend.enabled = false;
                liquidComp.rb.isKinematic = true;
                liquidComp.SendCustomEventDelayedSeconds("DelayedKill", 1);
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

                //If the container is at least a third full of liquid, it can absorb Dust!
                if (curLiquidParts >= (float)maxLiquidParts / 3)
                {
                    totalColors++;

                    //If there isn't already more dust than the total amount of liquid, add to the recipe list
                    if (dustParts < maxLiquidParts)
                    {
                        dustParts++;
                        for (int i = 0; i < containedLiquids.Length; i++)
                        {
                            if (containedBerries[i] == 0)
                            {
                                containedBerries[i] = dustComp.berryDustType;

                                //Each time a berry is added to the recipe list, divide total color by the average of all colors added
                                curLiquidColor += dustComp.berryColor;
                                containerRend.material.SetColor("_CrossColor", curLiquidColor / totalColors);
                                containerRend.material.SetColor("_Color", curLiquidColor / totalColors);

                                break;
                            }
                        }
                    }

                    //A Dust-Ready container will ALWAYS kill dust, even if it didn't absorb it!
                    Destroy(other.gameObject);
                }
            }
        }
    }
}
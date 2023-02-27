using UdonSharp;
using UnityEngine;
using System;
using VRC.SDKBase;
using VRC.Udon;
using Argus.ItemSystem;

namespace Catacombs.ElementSystem.Runtime
{
    //Element Containers are any objects that will be able to hold RuntimeElements in them (if marked Containable)
    //Current Elements/Drops: Berries, Dusts, Liquids
    public class ElementContainer : UdonSharpBehaviour
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
        [SerializeField] private bool canMixElements = true;
        [SerializeField] private ElementTypes[] containedLiquids = new ElementTypes[0];
        [SerializeField] private ElementTypes[] containedElements = new ElementTypes[0];

        void Start()
        {
            if (containerRend == null) containerRend = GetComponent<Renderer>();
            containerSpoutFilter = containerSpout.GetComponent<MeshFilter>();

            containedLiquids = new ElementTypes[maxLiquidParts];
            containedElements = new ElementTypes[maxLiquidParts];

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

                    //lowestVert, the literal lowest vertex of the spout mesh, indicates which way the container is tilted
                    Vector3 lowestVert = new Vector3(0, Mathf.Infinity, 0);

                    foreach (Vector3 vert in containerSpoutFilter.mesh.vertices)
                    {
                        Vector3 vertGlobal = transform.TransformPoint(vert);
                        if (vertGlobal.y < lowestVert.y) lowestVert = vertGlobal;
                    }

                    //After getting verts from Mesh Filter the actual obj instance's transform must be manually added
                    lowestVert += containerSpoutFilter.transform.localPosition;

                    Instantiate(liquidDropPrefab, lowestVert, Quaternion.identity, transform.parent);

                    DrainLiquid();
                }
            }
            else pourTimer = 0;
        }

        private void DrainLiquid()
        {
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
                if (containedLiquids[i] != ElementTypes.None)
                {
                    containedLiquids[i] = ElementTypes.None;
                    break;
                }
            }
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
                ElementPrecipitate elementPrecipitate = other.GetComponent<ElementPrecipitate>();

                //Reject liquid drops that came from us!
                //if (elementPrecipitate.containerSpawnedFrom == gameObject) return;

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
                    else if (elementPrecipitate.elementTypeId != containedLiquids[0]) return;

                    curLiquidParts++;

                    for (int i = 0; i < containedLiquids.Length; i++)
                    {
                        if (containedLiquids[i] == 0)
                        {
                            containedLiquids[i] = elementPrecipitate.elementTypeId;
                            break;
                        }
                    }
                }

                //ALWAYS destroy liquids that enter containers, even if already full
                elementPrecipitate.elementRenderer.enabled = false;
                elementPrecipitate.rb.isKinematic = true;
                elementPrecipitate.GetComponent<Collider>().enabled = false;
                elementPrecipitate.SendCustomEventDelayedSeconds(nameof(ElementPrecipitate._DelayedKill), 1);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            //As long as a containable object is in the collider...
            if (other.gameObject.layer == 24)
            {
                //We only currently care if a Berry Dust item is in the container
                ElementPrecipitate elementPrecipitate = other.GetComponent<ElementPrecipitate>();
                if (elementPrecipitate == null) return;

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
                            if (containedElements[i] == 0)
                            {
                                containedElements[i] = elementPrecipitate.elementTypeId;

                                //Each time a berry is added to the recipe list, divide total color by the average of all colors added
                                curLiquidColor += elementPrecipitate.elementColor;
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
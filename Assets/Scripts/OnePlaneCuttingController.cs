using UdonSharp;
using UnityEngine;

//[ExecuteInEditMode]
public class OnePlaneCuttingController : UdonSharpBehaviour
{
    public GameObject liquidClippingPlane;
    public Vector3 normal;
    public Vector3 position;
    public Renderer containerRend;
    // Use this for initialization
    void Start()
    {
        containerRend = GetComponent<Renderer>();
        normal = liquidClippingPlane.transform.TransformVector(new Vector3(0, 0, -1));
        position = liquidClippingPlane.transform.position;
        UpdateShaderProperties();
    }
    void Update()
    {
        UpdateShaderProperties();
    }

    private void UpdateShaderProperties()
    {

        normal = liquidClippingPlane.transform.TransformVector(new Vector3(0, 0, -1));
        position = liquidClippingPlane.transform.position;
        for (int i = 0; i < containerRend.materials.Length; i++)
        {
            if (containerRend.materials[i].shader.name == "CrossSection/OnePlaneBSP")
            {
                containerRend.materials[i].SetVector("_PlaneNormal", normal);
                containerRend.materials[i].SetVector("_PlanePosition", position);
            }
        }

    }
}